using Xunit;

[assembly: AssemblyTrait("Category", "Startup")]
// Serialized: AddAuthHost() writes a shared "tempkey.jwk" file; parallel classes race on it.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
