using System.Runtime.CompilerServices;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace NexaMediaServer.Tests.Integration;

/// <summary>
/// Provides shared Hot Chocolate services for integration tests.
/// </summary>
public static class TestServices
{
    static TestServices()
    {
        var services = new ServiceCollection();

        services.AddMemoryCache();

        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddTypes()
            .AddGlobalObjectIdentification()
            .AddQueryFieldToMutationPayloads()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddInMemoryOperationDocumentStorage()
            .UseAutomaticPersistedOperationPipeline();

        services.AddSingleton(sp => new RequestExecutorProxy(
            sp.GetRequiredService<IRequestExecutorProvider>(),
            sp.GetRequiredService<IRequestExecutorEvents>(),
            ISchemaDefinition.DefaultName
        ));

        Services = services.BuildServiceProvider();
        Executor = Services.GetRequiredService<RequestExecutorProxy>();
    }

    /// <summary>
    /// Root service provider configured for integration tests.
    /// </summary>
    public static IServiceProvider Services { get; }

    /// <summary>
    /// Proxy that exposes the active <see cref="IRequestExecutor"/>.
    /// </summary>
    public static RequestExecutorProxy Executor { get; }

    /// <summary>
    /// Executes a GraphQL request and returns the JSON payload.
    /// </summary>
    /// <param name="configureRequest">Delegate used to configure the request builder.</param>
    /// <param name="cancellationToken">Token used to cancel the request execution.</param>
    public static async Task<string> ExecuteRequestAsync(
        Action<OperationRequestBuilder> configureRequest,
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = Services.CreateAsyncScope();

        var requestBuilder = OperationRequestBuilder.New().SetServices(scope.ServiceProvider);
        configureRequest(requestBuilder);
        var request = requestBuilder.Build();

        await using var result = await Executor.ExecuteAsync(request, cancellationToken);

        result.ExpectOperationResult();

        return result.ToJson();
    }

    /// <summary>
    /// Executes a GraphQL request that yields a response stream and returns each chunk as JSON.
    /// </summary>
    /// <param name="configureRequest">Delegate used to configure the request builder.</param>
    /// <param name="cancellationToken">Token used to cancel the request execution.</param>
    public static async IAsyncEnumerable<string> ExecuteRequestAsStreamAsync(
        Action<OperationRequestBuilder> configureRequest,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var scope = Services.CreateAsyncScope();

        var requestBuilder = OperationRequestBuilder.New().SetServices(scope.ServiceProvider);
        configureRequest(requestBuilder);
        var request = requestBuilder.Build();

        await using var result = await Executor.ExecuteAsync(request, cancellationToken);

        await foreach (
            var element in result
                .ExpectResponseStream()
                .ReadResultsAsync()
                .WithCancellation(cancellationToken)
        )
        {
            await using (element)
            {
                yield return element.ToJson();
            }
        }
    }
}
