using Concertable.Auth.Contracts.Events;
using Concertable.Shared.Email.Application;

namespace Concertable.Auth.Hosting;

public static class AuthTopology
{
    public static AsbTopology AddAuthTopology(this AsbTopology topology) =>
        topology
            .Publish<CredentialRegisteredEvent>()
            .Queue<SendEmailCommand>(AuthConstants.ServiceName)
            .Queue<SendVerificationEmailCommand>(AuthConstants.ServiceName);
}
