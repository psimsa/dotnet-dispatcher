using System.Threading;
using System.Threading.Tasks;

namespace DotnetDispatcher;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> Execute(TCommand command, CancellationToken cancellationToken);
}
