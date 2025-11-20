using Snapshooter.Xunit;

namespace NexaMediaServer.Tests.Integration;

/// <summary>
/// Ensures the GraphQL schema snapshot does not change unexpectedly.
/// </summary>
public class GraphQLSchemaSnapshotTest
{
    /// <summary>
    /// Ensures the generated GraphQL schema matches the stored snapshot.
    /// </summary>
    [Fact]
    public async Task SchemaChangeTest()
    {
        var schema = await TestServices.Executor.GetSchemaAsync(default);

        schema.ToString().MatchSnapshot();
    }
}
