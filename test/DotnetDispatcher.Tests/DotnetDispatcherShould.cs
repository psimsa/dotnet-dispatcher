using DotnetDispatcher.Core;
using DotnetDispatcher.Tests.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDispatcher.Tests;

public class DotnetDispatcherShould
{
    [Fact]
    public async Task GenerateAndDispatchQuery()
    {
        var services = new ServiceCollection();
        services.RegisterTestDispatcherAndHandlers();

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetService<ITestDispatcher>()!;

        var result = await dispatcher.Dispatch(new GreetingsQuery("John"));

        Assert.Equal("Hello John!", result.Greeting);
    }
}