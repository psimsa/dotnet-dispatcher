using ConsoleTester;
using ConsoleTester.cqrs;
using DotnetDispatcher.Core;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddSingleton<IQueryHandler<SampleQuery, SampleQueryResponse>, SampleQueryHandler>()
    .AddSingleton<IQueryHandler<MyQuery1, MyQuery1Response>, MyQuery1Handler>()
    .AddSingleton<IQueryHandler<MyQuery2, MyQuery2Response>, MyQuery2Handler>()
    .AddSingleton<IMyFirstDispatcher, MyFirstDispatcher>()
    .AddSingleton<IMySecondDispatcher, MySecondDispatcher>()
    .BuildServiceProvider();

var firstDispatcher = services.GetRequiredService<IMyFirstDispatcher>();
var firstDispatcherResponse = await firstDispatcher.Dispatch(new SampleQuery(), CancellationToken.None);
Console.WriteLine(firstDispatcherResponse.Value);

var secondDispatcher = services.GetRequiredService<IMySecondDispatcher>();
var secondDispatcherResponse = await secondDispatcher.Dispatch(new MyQuery1(1), CancellationToken.None);
Console.WriteLine(secondDispatcherResponse.Value);

var secondDispatcherResponse2 = await secondDispatcher.Dispatch(new MyQuery2("sadf"), CancellationToken.None);
Console.WriteLine(secondDispatcherResponse2.Id);

public class SampleQueryHandler : IQueryHandler<SampleQuery, SampleQueryResponse>
{
    public Task<SampleQueryResponse> Query(SampleQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SampleQueryResponse("nah"));
    }
}

class MyQuery1Handler : IQueryHandler<MyQuery1, MyQuery1Response>
{
    public Task<MyQuery1Response> Query(MyQuery1 query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MyQuery1Response("blah"));
    }
}

class MyQuery2Handler : IQueryHandler<MyQuery2, MyQuery2Response>
{
    public Task<MyQuery2Response> Query(MyQuery2 query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MyQuery2Response(1));
    }
}