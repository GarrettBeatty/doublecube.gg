using Backgammon.IntegrationTests.Fixtures;

namespace Backgammon.IntegrationTests.Collections;

/// <summary>
/// xUnit collection for SignalR integration tests.
/// Tests in this collection share the same WebApplicationFixture instance.
/// </summary>
[CollectionDefinition("SignalR")]
public class SignalRCollection : ICollectionFixture<WebApplicationFixture>
{
}
