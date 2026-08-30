using System.Buffers.Binary;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Concertable.Auth.OperationalStoreMigration;

internal sealed class OperationalStoreMigrator
{
    private const string Schema = "idsrv";
    private const int LockTimeoutMilliseconds = 10_000;
    private static readonly string[] TableNames =
    [
        "DeviceCodes",
        "Keys",
        "PersistedGrants",
        "PushedAuthorizationRequests",
        "ServerSideSessions"
    ];

    public async Task<OperationalStoreMigrationReport> InspectAsync(
        string sourceConnectionString,
        string targetConnectionString,
        CancellationToken ct = default)
    {
        await using var source = new SqlConnection(sourceConnectionString);
        await using var target = new SqlConnection(targetConnectionString);
        await OpenAndValidateAsync(source, target, ct);

        var definitions = await ReadAndValidateDefinitionsAsync(source, target, ct);
        var reports = new List<OperationalStoreTableReport>(definitions.Count);
        foreach (var definition in definitions)
        {
            var sourceState = await ReadStateAsync(source, null, definition, ct);
            var targetState = await ReadStateAsync(target, null, definition, ct);
            reports.Add(ToReport(definition.Name, sourceState, targetState));
        }

        return new OperationalStoreMigrationReport(false, reports);
    }

    public async Task<OperationalStoreMigrationReport> CopyAsync(
        string sourceConnectionString,
        string targetConnectionString,
        CancellationToken ct = default)
    {
        await using var source = new SqlConnection(sourceConnectionString);
        await using var target = new SqlConnection(targetConnectionString);
        await OpenAndValidateAsync(source, target, ct);

        var definitions = await ReadAndValidateDefinitionsAsync(source, target, ct);
        await using var targetTransaction = (SqlTransaction)await target.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockTablesAsync(target, targetTransaction, definitions, ct);
        await EnsureTargetIsEmptyAsync(target, targetTransaction, definitions, ct);

        await using var sourceTransaction = (SqlTransaction)await source.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockTablesAsync(source, sourceTransaction, definitions, ct);

        foreach (var definition in definitions)
        {
            await CopyTableAsync(source, sourceTransaction, target, targetTransaction, definition, ct);
            await CopyIdentityStateAsync(source, sourceTransaction, target, targetTransaction, definition, ct);
        }

        var reports = new List<OperationalStoreTableReport>(definitions.Count);
        foreach (var definition in definitions)
        {
            var sourceState = await ReadStateAsync(source, sourceTransaction, definition, ct);
            var targetState = await ReadStateAsync(target, targetTransaction, definition, ct);
            if (sourceState != targetState)
                throw new InvalidOperationException(
                    $"Verification failed for {Qualified(definition.Name)}; the target transaction was not committed.");

            reports.Add(ToReport(definition.Name, sourceState, targetState));
        }

        await targetTransaction.CommitAsync(ct);
        await sourceTransaction.CommitAsync(ct);
        return new OperationalStoreMigrationReport(true, reports);
    }

    private static async Task OpenAndValidateAsync(SqlConnection source, SqlConnection target, CancellationToken ct)
    {
        await source.OpenAsync(ct);
        await target.OpenAsync(ct);

        var sourceIdentity = await ReadDatabaseIdentityAsync(source, ct);
        var targetIdentity = await ReadDatabaseIdentityAsync(target, ct);
        if (string.Equals(sourceIdentity, targetIdentity, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Source and target resolve to the same SQL database.");
    }

    private static async Task<string> ReadDatabaseIdentityAsync(SqlConnection connection, CancellationToken ct)
    {
        const string sql = "SELECT CONCAT(CONVERT(nvarchar(256), SERVERPROPERTY('ServerName')), N'|', DB_NAME())";
        await using var command = new SqlCommand(sql, connection);
        return (string)(await command.ExecuteScalarAsync(ct)
            ?? throw new InvalidOperationException("Could not resolve the SQL database identity."));
    }

    private static async Task<IReadOnlyList<TableDefinition>> ReadAndValidateDefinitionsAsync(
        SqlConnection source,
        SqlConnection target,
        CancellationToken ct)
    {
        var definitions = new List<TableDefinition>(TableNames.Length);
        foreach (var tableName in TableNames)
        {
            var sourceDefinition = await ReadDefinitionAsync(source, tableName, ct);
            var targetDefinition = await ReadDefinitionAsync(target, tableName, ct);
            if (!sourceDefinition.Columns.SequenceEqual(targetDefinition.Columns)
                || !sourceDefinition.PrimaryKey.SequenceEqual(targetDefinition.PrimaryKey, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Source and target schemas differ for {Qualified(tableName)}.");
            }

            definitions.Add(sourceDefinition);
        }

        return definitions;
    }

    private static async Task<TableDefinition> ReadDefinitionAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken ct)
    {
        const string columnSql = """
            SELECT
                column_definition.name,
                TYPE_NAME(column_definition.user_type_id),
                column_definition.max_length,
                column_definition.precision,
                column_definition.scale,
                column_definition.is_nullable,
                column_definition.is_identity,
                column_definition.is_computed,
                column_definition.collation_name
            FROM sys.columns AS column_definition
            INNER JOIN sys.tables AS table_definition
                ON table_definition.object_id = column_definition.object_id
            INNER JOIN sys.schemas AS schema_definition
                ON schema_definition.schema_id = table_definition.schema_id
            WHERE schema_definition.name = @schema AND table_definition.name = @table
            ORDER BY column_definition.column_id;
            """;

        var columns = new List<ColumnDefinition>();
        await using (var command = new SqlCommand(columnSql, connection))
        {
            command.Parameters.AddWithValue("@schema", Schema);
            command.Parameters.AddWithValue("@table", tableName);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                columns.Add(new ColumnDefinition(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetInt16(2),
                    reader.GetByte(3),
                    reader.GetByte(4),
                    reader.GetBoolean(5),
                    reader.GetBoolean(6),
                    reader.GetBoolean(7),
                    reader.IsDBNull(8) ? null : reader.GetString(8)));
            }
        }

        if (columns.Count == 0)
            throw new InvalidOperationException(
                $"Required table {Qualified(tableName)} does not exist in database '{connection.Database}'.");

        const string primaryKeySql = """
            SELECT column_definition.name
            FROM sys.indexes AS index_definition
            INNER JOIN sys.index_columns AS index_column
                ON index_column.object_id = index_definition.object_id
                AND index_column.index_id = index_definition.index_id
            INNER JOIN sys.columns AS column_definition
                ON column_definition.object_id = index_column.object_id
                AND column_definition.column_id = index_column.column_id
            INNER JOIN sys.tables AS table_definition
                ON table_definition.object_id = index_definition.object_id
            INNER JOIN sys.schemas AS schema_definition
                ON schema_definition.schema_id = table_definition.schema_id
            WHERE schema_definition.name = @schema
                AND table_definition.name = @table
                AND index_definition.is_primary_key = 1
            ORDER BY index_column.key_ordinal;
            """;

        var primaryKey = new List<string>();
        await using (var command = new SqlCommand(primaryKeySql, connection))
        {
            command.Parameters.AddWithValue("@schema", Schema);
            command.Parameters.AddWithValue("@table", tableName);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                primaryKey.Add(reader.GetString(0));
        }

        if (primaryKey.Count == 0)
            throw new InvalidOperationException($"Required table {Qualified(tableName)} has no primary key.");

        return new TableDefinition(tableName, columns, primaryKey);
    }

    private static async Task LockTablesAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IEnumerable<TableDefinition> definitions,
        CancellationToken ct)
    {
        await using (var timeout = new SqlCommand($"SET LOCK_TIMEOUT {LockTimeoutMilliseconds};", connection, transaction))
            await timeout.ExecuteNonQueryAsync(ct);

        foreach (var definition in definitions)
        {
            var sql = $"SELECT COUNT_BIG(*) FROM {Qualified(definition.Name)} WITH (TABLOCKX, HOLDLOCK);";
            await using var command = new SqlCommand(sql, connection, transaction);
            await command.ExecuteScalarAsync(ct);
        }
    }

    private static async Task EnsureTargetIsEmptyAsync(
        SqlConnection target,
        SqlTransaction transaction,
        IEnumerable<TableDefinition> definitions,
        CancellationToken ct)
    {
        foreach (var definition in definitions)
        {
            var rows = await ReadRowCountAsync(target, transaction, definition.Name, ct);
            if (rows != 0)
                throw new InvalidOperationException(
                    $"Target table {Qualified(definition.Name)} contains {rows} row(s); refusing to overwrite it.");
        }
    }

    private static async Task CopyTableAsync(
        SqlConnection source,
        SqlTransaction sourceTransaction,
        SqlConnection target,
        SqlTransaction targetTransaction,
        TableDefinition definition,
        CancellationToken ct)
    {
        var columns = string.Join(", ", definition.Columns.Select(column => Quote(column.Name)));
        var sql = $"SELECT {columns} FROM {Qualified(definition.Name)};";
        await using var command = new SqlCommand(sql, source, sourceTransaction);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
        using var bulkCopy = new SqlBulkCopy(
            target,
            SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.TableLock,
            targetTransaction)
        {
            DestinationTableName = Qualified(definition.Name),
            BulkCopyTimeout = 600
        };
        foreach (var column in definition.Columns)
            bulkCopy.ColumnMappings.Add(column.Name, column.Name);

        await bulkCopy.WriteToServerAsync(reader, ct);
    }

    private static async Task CopyIdentityStateAsync(
        SqlConnection source,
        SqlTransaction sourceTransaction,
        SqlConnection target,
        SqlTransaction targetTransaction,
        TableDefinition definition,
        CancellationToken ct)
    {
        if (!definition.Columns.Any(column => column.IsIdentity))
            return;

        await using var readCommand = new SqlCommand(
            $"SELECT IDENT_CURRENT(N'{Schema}.{definition.Name}');",
            source,
            sourceTransaction);
        var currentIdentity = (decimal)(await readCommand.ExecuteScalarAsync(ct)
            ?? throw new InvalidOperationException($"Could not read the identity state for {Qualified(definition.Name)}."));
        var identity = currentIdentity.ToString(CultureInfo.InvariantCulture);
        await using var writeCommand = new SqlCommand(
            $"DBCC CHECKIDENT (N'{Qualified(definition.Name)}', RESEED, {identity});",
            target,
            targetTransaction);
        await writeCommand.ExecuteNonQueryAsync(ct);
    }

    private static async Task<TableState> ReadStateAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        TableDefinition definition,
        CancellationToken ct)
    {
        var rows = await ReadRowCountAsync(connection, transaction, definition.Name, ct);
        var columns = string.Join(", ", definition.Columns.Select(column => Quote(column.Name)));
        var orderBy = string.Join(", ", definition.PrimaryKey.Select(Quote));
        var sql = $"SELECT {columns} FROM {Qualified(definition.Name)} ORDER BY {orderBy};";
        await using var command = new SqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        while (await reader.ReadAsync(ct))
        {
            hash.AppendData([0x7F]);
            for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                AppendValue(hash, reader.GetValue(ordinal));
        }

        return new TableState(rows, Convert.ToHexString(hash.GetHashAndReset()));
    }

    private static async Task<long> ReadRowCountAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string tableName,
        CancellationToken ct)
    {
        await using var command = new SqlCommand($"SELECT COUNT_BIG(*) FROM {Qualified(tableName)};", connection, transaction);
        return (long)(await command.ExecuteScalarAsync(ct)
            ?? throw new InvalidOperationException($"Could not count {Qualified(tableName)}."));
    }

    private static void AppendValue(IncrementalHash hash, object value)
    {
        if (value is DBNull)
        {
            hash.AppendData([0]);
            return;
        }

        hash.AppendData([1]);
        var bytes = value switch
        {
            byte[] binary => binary,
            string text => Encoding.UTF8.GetBytes(text),
            DateTime dateTime => Encoding.UTF8.GetBytes(dateTime.ToString("O", CultureInfo.InvariantCulture)),
            DateTimeOffset dateTimeOffset => Encoding.UTF8.GetBytes(dateTimeOffset.ToString("O", CultureInfo.InvariantCulture)),
            Guid guid => guid.ToByteArray(),
            bool boolean => [boolean ? (byte)1 : (byte)0],
            short number => BitConverter.GetBytes(number),
            int number => BitConverter.GetBytes(number),
            long number => BitConverter.GetBytes(number),
            float number => BitConverter.GetBytes(number),
            double number => BitConverter.GetBytes(number),
            decimal number => DecimalBytes(number),
            _ => Encoding.UTF8.GetBytes(Convert.ToString(value, CultureInfo.InvariantCulture)
                ?? throw new InvalidOperationException($"Could not serialize SQL value of type {value.GetType().FullName}."))
        };

        Span<byte> length = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
    }

    private static byte[] DecimalBytes(decimal value)
    {
        var bits = decimal.GetBits(value);
        var bytes = new byte[bits.Length * sizeof(int)];
        for (var index = 0; index < bits.Length; index++)
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(index * sizeof(int), sizeof(int)), bits[index]);
        return bytes;
    }

    private static OperationalStoreTableReport ToReport(string name, TableState source, TableState target) =>
        new(name, source.Rows, target.Rows, source.Sha256, target.Sha256);

    private static string Qualified(string tableName) => $"{Quote(Schema)}.{Quote(tableName)}";

    private static string Quote(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

    private sealed record ColumnDefinition(
        string Name,
        string StoreType,
        short MaxLength,
        byte Precision,
        byte Scale,
        bool IsNullable,
        bool IsIdentity,
        bool IsComputed,
        string? Collation);

    private sealed record TableDefinition(
        string Name,
        IReadOnlyList<ColumnDefinition> Columns,
        IReadOnlyList<string> PrimaryKey);

    private sealed record TableState(long Rows, string Sha256);
}

internal sealed record OperationalStoreMigrationReport(
    bool Executed,
    IReadOnlyList<OperationalStoreTableReport> Tables)
{
    public bool TargetIsEmpty => Tables.All(table => table.TargetRows == 0);
}

internal sealed record OperationalStoreTableReport(
    string Name,
    long SourceRows,
    long TargetRows,
    string SourceSha256,
    string TargetSha256);
