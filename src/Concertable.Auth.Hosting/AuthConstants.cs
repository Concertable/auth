namespace Concertable.Auth.Hosting;

public static class AuthConstants
{
    public const string Database = "AuthDb";
    public const string Resource = "auth";
    public const string ServiceName = "concertable-auth";

    public const int ContainerPort = 8080;

    public static readonly string ContainerHttpsUrl = $"https://+:{ContainerPort}";
}
