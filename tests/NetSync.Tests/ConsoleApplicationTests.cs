namespace NetSync.Tests;

[CollectionDefinition(nameof(ApplicationIntegrationCollection))]
public sealed class ApplicationIntegrationCollection : ICollectionFixture<ApplicationFixture>
{
}
[Collection(nameof(ApplicationIntegrationCollection))]
public class ConsoleApplicationTests : IAsyncLifetime
{
    private readonly ApplicationFixture _applicationFixture;

    public ConsoleApplicationTests(ApplicationFixture applicationFixture)
    {
        _applicationFixture = applicationFixture;
    }

    
    
    [Fact]
    public async Task TestIfAllStarted()
    {
        Assert.Equal(3, _applicationFixture.Consoles.Count);
        await Assert.AllAsync(_applicationFixture.Consoles, async console =>
        {
            Assert.NotEqual("", await console.Read(DateTime.MinValue));
        });
    }
    
    [Fact]
    public async Task WhenOneConsoleSetsData_ThenAllConsolesReceiveIt()
    {
        var console1 = _applicationFixture.Consoles[0];
        var console2 = _applicationFixture.Consoles[1];
        var console3 = _applicationFixture.Consoles[2];

        var data = "World!";
        
        await console1.Write($"set hello:{data}");
        var since = DateTime.Now;
        await console2.Write("get hello");
        Assert.Equal(data, await console2.Read(since, 1));
        await console3.Write("get hello");
        Assert.Equal(data, await console3.Read(since, 1));
    }

    public Task InitializeAsync()
    {
        // This method is called before any tests in the collection are run.
        // You can use it to set up any shared resources needed for the tests.
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        // This method is called after all tests in the collection have run.
        // You can use it to clean up any shared resources.
        _applicationFixture.ResetState();
        return Task.CompletedTask;
    }
}