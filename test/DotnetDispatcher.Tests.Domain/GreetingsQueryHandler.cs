namespace DotnetDispatcher.Tests.Domain;

public class GreetingsQueryHandler : IQueryHandler<GreetingsQuery, GreetingsQueryResponse>
{
    public Task<GreetingsQueryResponse> Query(
        GreetingsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(new GreetingsQueryResponse($"Hello {query.Name}!"));
    }
}
