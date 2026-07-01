using Microsoft.Extensions.Logging;

namespace Concertable.Auth;

internal static partial class Log
{
    #region AuthDevSeeder

    [LoggerMessage(Level = LogLevel.Information, Message = "AuthDevSeeder: existing credential count {ExistingCount}; about to seed {NewCount} new")]
    internal static partial void SeedingCredentials(this ILogger logger, int existingCount, int newCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "AuthDevSeeder: SaveChanges completed for {Count} new credentials")]
    internal static partial void SeededCredentials(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "AuthDevSeeder: skipped (credentials already exist)")]
    internal static partial void SeedSkipped(this ILogger logger);

    #endregion

    #region RemoteProfileClaimsProvider

    [LoggerMessage(Level = LogLevel.Information, Message = "{Source} claims: requesting subjectId={SubjectId}")]
    internal static partial void RemoteClaimsRequested(this ILogger logger, string source, Guid subjectId);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Source} claims: received subjectId={SubjectId} claimCount={ClaimCount}")]
    internal static partial void RemoteClaimsReceived(this ILogger logger, string source, Guid subjectId, int claimCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Source} claims: non-success subjectId={SubjectId} status={Status} body={Body}")]
    internal static partial void RemoteClaimsNonSuccess(this ILogger logger, string source, Guid subjectId, int status, string body);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Source} claims: failed subjectId={SubjectId}")]
    internal static partial void RemoteClaimsFailed(this ILogger logger, Exception ex, string source, Guid subjectId);

    #endregion
}
