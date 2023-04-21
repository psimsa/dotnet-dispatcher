using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotnetDispatcher.Core;

namespace ConsoleTester;

public record SampleQueryWithGenerics : IQuery<Result<SampleQueryWithGenericsResponse>>;

public record SampleQueryWithGenericsResponse(string Value);

public record Result<T>(T Data);

public class SampleQueryWithGenericsHandler : IQueryHandler<SampleQueryWithGenerics, Result<SampleQueryWithGenericsResponse>>
{
    public Task<Result<SampleQueryWithGenericsResponse>> Query(SampleQueryWithGenerics query, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Result<SampleQueryWithGenericsResponse>(new SampleQueryWithGenericsResponse("Hello World")));
    }
}
