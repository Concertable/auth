using Concertable.Shared.Email.Application;

namespace Concertable.Auth.Hosting;

public static class AuthTopology
{
    public static AsbTopology AddAuthTopology(this AsbTopology topology) =>
        topology
            .Queue<SendEmailCommand>(AuthConstants.ServiceName)
            .Queue<SendVerificationEmailCommand>(AuthConstants.ServiceName);
}
