using DotnetDispatcher.Core;

namespace DotnetDispatcher.Tests.Domain;

public record GreetingsQuery(string Name) : IQuery<GreetingsQueryResponse>;
public record GreetingsQueryResponse(string Greeting);

public class GreetingsQueryHandler : IQueryHandler<GreetingsQuery, GreetingsQueryResponse>
{
    public Task<GreetingsQueryResponse> Query(GreetingsQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GreetingsQueryResponse($"Hello {query.Name}!"));
    }
}
