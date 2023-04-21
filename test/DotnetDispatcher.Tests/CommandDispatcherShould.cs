using DotnetDispatcher.Core;
using DotnetDispatcher.Tests.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDispatcher.Tests;

public class CommandDispatcherShould
{
    [Fact]
    public async Task GenerateAndDispatchSimpleCommand()
    {
        var dispatcher = GetDispatcher();
        
        var response = await dispatcher.Dispatch(new DeleteDatabaseCommand());

        Assert.True(response.IsSuccess);
    }

    private ICommandDispatcher GetDispatcher()
    {
        var services = new ServiceCollection();
        services.RegisterCommandDispatcherAndHandlers();

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetService<ICommandDispatcher>()!;
    }
}