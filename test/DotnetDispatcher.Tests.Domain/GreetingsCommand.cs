using DotnetDispatcher.Core;

namespace DotnetDispatcher.Tests.Domain;

public record DeleteDatabaseCommand : ICommand<Result>;

public class DeleteDatabaseCommandHandler : ICommandHandler<DeleteDatabaseCommand, Result>
{
    public Task<Result> Execute(DeleteDatabaseCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Result(true));
    }
}