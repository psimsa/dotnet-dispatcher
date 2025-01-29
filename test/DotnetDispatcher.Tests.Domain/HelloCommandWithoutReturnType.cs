using DotnetDispatcher;

namespace DotnetDispatcher.Tests.Domain;

public record HelloWithoutReturnTypeCommand : ICommand;

public class HelloWithoutReturnTypeCommandHandler : ICommandHandler<HelloWithoutReturnTypeCommand>
{
    public Task Execute(HelloWithoutReturnTypeCommand command, CancellationToken cancellationToken)
    {
        Console.WriteLine("Hello, World!");
        return Task.CompletedTask;
    }
}
