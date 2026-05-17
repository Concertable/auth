using Concertable.User.Contracts;

namespace Concertable.Auth.Services;

public abstract record RoleResolution
{
    public sealed record Resolved(Role Role) : RoleResolution;
    public sealed record UnknownClient : RoleResolution;
    public sealed record InvalidSelection : RoleResolution;
}
