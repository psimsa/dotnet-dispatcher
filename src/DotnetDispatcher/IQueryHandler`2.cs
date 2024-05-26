using System.Threading;
using System.Threading.Tasks;

namespace DotnetDispatcher.Core
{
    public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
    {
        Task<TResponse> Query(TQuery query, CancellationToken cancellationToken);
    }
}