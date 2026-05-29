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

    [LoggerMessage(Level = LogLevel.Information, Message = "B2BProfileClaimsProvider: requesting subjectId={SubjectId} url={Url}")]
    internal static partial void B2BClaimsRequested(this ILogger logger, Guid subjectId, string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "B2BProfileClaimsProvider: received subjectId={SubjectId} status={Status} claimCount={ClaimCount}")]
    internal static partial void B2BClaimsReceived(this ILogger logger, Guid subjectId, int status, int claimCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "B2BProfileClaimsProvider: non-success subjectId={SubjectId} status={Status} body={Body}")]
    internal static partial void B2BClaimsNonSuccess(this ILogger logger, Guid subjectId, int status, string body);

    [LoggerMessage(Level = LogLevel.Error, Message = "B2BProfileClaimsProvider: failed subjectId={SubjectId}")]
    internal static partial void B2BClaimsFailed(this ILogger logger, Exception ex, Guid subjectId);

    [LoggerMessage(Level = LogLevel.Information, Message = "CustomerProfileClaimsProvider: requesting subjectId={SubjectId} url={Url}")]
    internal static partial void CustomerClaimsRequested(this ILogger logger, Guid subjectId, string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "CustomerProfileClaimsProvider: received subjectId={SubjectId} status={Status} claimCount={ClaimCount}")]
    internal static partial void CustomerClaimsReceived(this ILogger logger, Guid subjectId, int status, int claimCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "CustomerProfileClaimsProvider: non-success subjectId={SubjectId} status={Status} body={Body}")]
    internal static partial void CustomerClaimsNonSuccess(this ILogger logger, Guid subjectId, int status, string body);

    [LoggerMessage(Level = LogLevel.Error, Message = "CustomerProfileClaimsProvider: failed subjectId={SubjectId}")]
    internal static partial void CustomerClaimsFailed(this ILogger logger, Exception ex, Guid subjectId);
}
