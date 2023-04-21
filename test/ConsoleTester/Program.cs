using System.Reflection;
using ConsoleTester;
using ConsoleTester.cqrs;
using DotnetDispatcher.Core;
using Microsoft.Extensions.DependencyInjection;

var serviceCollection = new ServiceCollection();

var allTypes = Assembly.GetAssembly(typeof(MyFirstDispatcher))?.GetTypes().ToList() ?? new List<Type>();

serviceCollection.RegisterMyFirstDispatcherAndHandlers();
serviceCollection.RegisterMySecondDispatcherAndHandlers();

var services = serviceCollection
    .BuildServiceProvider();

var firstDispatcher = services.GetRequiredService<IMyFirstDispatcher>();
var sampleQueryResponse = await firstDispatcher.Dispatch(new SampleQuery(), CancellationToken.None);
Console.WriteLine(sampleQueryResponse.Value);
var sampleQueryWithGenericsResponse =
    await firstDispatcher.Dispatch(new SampleQueryWithGenerics(), CancellationToken.None);
Console.WriteLine(sampleQueryWithGenericsResponse.Data.Value);

var secondDispatcher = services.GetRequiredService<IMySecondDispatcher>();
var result = await secondDispatcher.Dispatch(new MyCommand1(1));
Console.WriteLine(result.Value);