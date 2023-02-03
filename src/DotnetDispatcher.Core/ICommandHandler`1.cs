using System.Threading;
using System.Threading.Tasks;

namespace DotnetDispatcher.Core;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task Execute(TCommand command, CancellationToken cancellationToken);
}