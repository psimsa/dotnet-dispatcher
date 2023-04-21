using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotnetDispatcher.Core;

namespace DotnetDispatcher.Tests.Domain;

public record QueryWithResultType(string Name) : IQuery<Result<QueryWithResultTypeResponse>>;

public record QueryWithResultTypeResponse(string Greeting);

public class QueryWithResultTypeHandler : IQueryHandler<QueryWithResultType, Result<QueryWithResultTypeResponse>>
{
    public Task<Result<QueryWithResultTypeResponse>> Query(QueryWithResultType query, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Result<QueryWithResultTypeResponse>(true, new QueryWithResultTypeResponse($"Greetings for {query.Name}...")));
    }
}
