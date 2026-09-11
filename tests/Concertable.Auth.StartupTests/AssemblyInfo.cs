using Xunit;

[assembly: AssemblyTrait("Category", "Startup")]

// AddDeveloperSigningCredential writes one tempkey.jwk per output directory; parallel collections race it.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
