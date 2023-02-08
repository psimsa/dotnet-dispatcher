using ConsoleTester;
using DotnetDispatcher.Core;
namespace ConsoleTester;

public class SampleQueryHandler : IQueryHandler<SampleQuery, SampleQueryResponse>
{
    public Task<SampleQueryResponse> Query(SampleQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SampleQueryResponse("nah"));
    }
}
