using System.Threading;
using System.Threading.Tasks;

namespace DotnetDispatcher;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task Execute(TCommand command, CancellationToken cancellationToken);
}
