namespace Concertable.Auth.IntegrationTests.Fixtures;

/// <summary>
/// Runs the same host under the <c>E2E</c> environment, which is the only environment that registers
/// the resource-owner-password client (<c>concertable-test</c>) and its validator. Isolated to the
/// password-grant token tests so the rest of the integration suite runs under <c>Testing</c> without
/// carrying an E2E-only capability.
/// </summary>
public sealed class E2ETokenApiFixture : ApiFixture
{
    protected override string EnvironmentName => "E2E";
}
