using DotnetDispatcher.Core;

namespace ConsoleTester.cqrs;

public record MyCommand1(int Id) : ICommand<MyCommand1Response>;

public class MyCommand1Handler : ICommandHandler<MyCommand1, MyCommand1Response>
{
    public Task<MyCommand1Response> Execute(MyCommand1 command, CancellationToken cancellationToken) =>
        Task.FromResult(new MyCommand1Response(command.Id.ToString()));
}
