using Fleet.Blazor.Agents.PipelineSteps;
using Fleet.Blazor.Pipeline;
using Fleet.Runtime.Adapters;
using Fleet.Runtime.Contracts;
using Fleet.Runtime.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Fleet.Blazor.Tests.RuntimeExtraction;

public class PipelineAdapterIntegrationTests
{
    [Fact]
    public async Task SaveOutputStep_PersistsFinalResult_ThroughOutputStoreAdapter()
    {
        var outputStore = new FakeOutputStore();
        var step = new SaveOutputStep(outputStore);
        var context = CreateContext();
        context.FinalResult = "persist-me";

        await step.ExecuteAsync(context);

        Assert.Single(outputStore.Saved);
        Assert.Equal("persist-me", outputStore.Saved[0]);
    }

    [Fact]
    public async Task CleanupAgentsStep_ReleasesAllPluginIds_ThroughPluginAdapter()
    {
        var pluginAdapter = new FakePluginClientAdapter();
        var step = new CleanupAgentsStep();
        var context = CreateContext(pluginAdapter);
        context.Agents["assistant"] = new AgentContext(new List<string> { "time", "filesystem" });

        await step.ExecuteAsync(context);

        Assert.Equal(new[] { "time", "filesystem" }, pluginAdapter.ReleasedIds);
        Assert.Empty(context.Agents);
    }

    [Fact]
    public void PipelineContextFactory_UsesRuntimeAdapterAndClonesKernel()
    {
        var rootKernel = new Kernel(new ServiceCollection().BuildServiceProvider());
        var pluginAdapter = new FakePluginClientAdapter();
        var factory = new PipelineContextFactory(rootKernel, pluginAdapter);
        var history = new List<AgentRequestItem> { new() { Content = "hello", Role = MessageType.User } };

        var context = factory.Create(history);

        Assert.Same(history, context.RequestHistory);
        Assert.NotSame(rootKernel, context.Kernel);
        Assert.Same(pluginAdapter, context.PluginClientAdapter);
    }

    private static PipelineContext CreateContext(IPluginClientAdapter? pluginAdapter = null)
    {
        var kernel = new Kernel(new ServiceCollection().BuildServiceProvider());
        return new PipelineContext([], kernel, pluginAdapter ?? new FakePluginClientAdapter());
    }

    private sealed class FakeOutputStore : IAgentOutputStore
    {
        public List<string> Saved { get; } = [];

        public Task SaveOutputAsync(string content, CancellationToken cancellationToken = default)
        {
            Saved.Add(content);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePluginClientAdapter : IPluginClientAdapter
    {
        public List<string> ReleasedIds { get; } = [];

        public Task<object> AcquireClientAsync(string idOrName, CancellationToken cancellationToken = default)
            => Task.FromResult((object)new object());

        public Task ReleaseClientAsync(string idOrName, bool dispose = false, CancellationToken cancellationToken = default)
        {
            ReleasedIds.Add(idOrName);
            return Task.CompletedTask;
        }
    }
}
