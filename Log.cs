using Microsoft.Extensions.Logging;

namespace Concertable.Auth;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "AuthDevSeeder: existing credential count {ExistingCount}; about to seed {NewCount} new")]
    internal static partial void SeedingCredentials(this ILogger logger, int existingCount, int newCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "AuthDevSeeder: SaveChanges completed for {Count} new credentials")]
    internal static partial void SeededCredentials(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "AuthDevSeeder: skipped (credentials already exist)")]
    internal static partial void SeedSkipped(this ILogger logger);
}
