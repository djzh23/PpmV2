namespace PpmV2.Tests.Integration;

/// <summary>
/// Defines the shared xUnit collection for all integration tests.
///
/// All test classes that inherit IntegrationTestBase are part of this collection,
/// which means they share a single PpmV2WebApplicationFactory instance.
/// This prevents concurrent MigrateAsync calls from causing race conditions
/// (e.g. "column already exists") when multiple test classes start in parallel.
/// </summary>
[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<PpmV2WebApplicationFactory>
{
}
