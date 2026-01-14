using Backgammon.IntegrationTests.Fixtures;

namespace Backgammon.IntegrationTests.Collections;

/// <summary>
/// xUnit collection for DynamoDB integration tests.
/// Tests in this collection share the same DynamoDbFixture instance.
/// </summary>
[CollectionDefinition("DynamoDB")]
public class DynamoDbCollection : ICollectionFixture<DynamoDbFixture>
{
}
