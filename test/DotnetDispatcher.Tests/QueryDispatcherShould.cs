using DotnetDispatcher.Tests.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDispatcher.Tests;

public class QueryDispatcherShould
{
    [Fact]
    public async Task GenerateAndDispatchSimpleQuery()
    {
        var dispatcher = GetDispatcher();

        var result = await dispatcher.Dispatch(new GreetingsQuery("John"));

        Assert.Equal("Hello John!", result.Greeting);
    }

    [Fact]
    public async Task GenerateAndDispatchQueryWithResultType()
    {
        var dispatcher = GetDispatcher();

        var result = await dispatcher.Dispatch(new QueryWithResultType("Pete"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Greetings for Pete...", result.Data.Greeting);
    }

    private IQueryDispatcher GetDispatcher()
    {
        var services = new ServiceCollection();
        services.RegisterQueryDispatcherAndHandlers();

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetService<IQueryDispatcher>()!;
    }
}
