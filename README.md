# dotnet-dispatcher 0.1
A simple dispatcher for .NET Core that uses Roslyn code generation to emit CQRS dispatcher code. Currently in extremely early stage, but I feel there's something to it.

## Why?
I'm a great 'not-friend' of things like reflection, Activator and runtime type determination. When writing CQRS code, I would like to have my dispatcher code generated in compile time, with all optimalizations and other goodies that come with it.

## Enter DotnetDispatcher
To generate a dispatcher is as simple as this:

```csharp
using DotnetDispatcher.Attributes;
using DotnetDispatcher.Core;

namespace ConsoleTester; 

public record SampleQuery : IQuery<SampleQueryResponse>;

public record SampleQueryResponse(string Value);

[GenerateDispatcher(typeof(SampleQuery))]
public partial class MyAppDispatcher : DispatcherBase
{
    public MyAppDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
```

This will generate a dispatcher that looks like this:

```csharp
namespace ConsoleTester
{
    public partial interface IMyAppDispatcher
    {
        Task<SampleQueryResponse> Dispatch(SampleQuery unit, CancellationToken cancellationToken = default);
    }
    public partial class MyAppDispatcher : IMyAppDispatcher
    {
        public Task<SampleQueryResponse> Dispatch(SampleQuery unit, CancellationToken cancellationToken = default) =>
            Get<IQueryHandler<SampleQuery, SampleQueryResponse>>().Query(unit, cancellationToken);
    }
}
```
Aside from the Dispatch code, a matching interface is also generated. This allows for easy mocking of the dispatcher as well as using dependency injection. You can have multiple dispatchers in a project, if you want to. Alternatively, you can repeat the use of the GenerateDispatcher attribute with other types that implement `IQuery<TResponse>`, `ICommand<TResponse>`  or `ICommand`.


## Download and install
There are three nuget packages to install:
- `DotnetDispatcher.Core` - contains interfaces your commands and queries should implement, as well as the `DispatcherBase` class
- `DotnetDispatcher.Attributes` - contains the `GenerateDispatcher` attribute
- `DotnetDispatcher.Generator` - contains the code generator

## How to use
Assuming you have an API project and a Domain project in your solution, where the generated dispatcher is part of the API project and the commands and queries are part of the Domain project:
- The API project should reference the Domain project, as well as the `DotnetDispatcher.Generator` and `DotnetDispatcher.Attributes` packages
- The Domain project should reference the `DotnetDispatcher.Core` package

The actual implementation in the projects is as such:
- The domain project contains the query and the query handler
```csharp
public record SampleQuery : IQuery<SampleQueryResponse>;

public record SampleQueryResponse(string Value);

public class SampleQueryHandler : IQueryHandler<SampleQuery, SampleQueryResponse>
{
    public Task<SampleQueryResponse> Query(SampleQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SampleQueryResponse("nah"));
    }
}
```

- The API project contains the dispatcher itself
```csharp
[GenerateDispatcher(typeof(SampleQuery))]
public partial class MyAppDispatcher : DispatcherBase
{
    public MyAppDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
```
This generates the dispatcher code as well as an IServiceCollection extension method that allows you to register the dispatcher in the DI container. You can use it in the API project like this:
```csharp
var serviceCollection = new ServiceCollection();

serviceCollection.RegisterMyAppDispatcherAndHandlers();

var services = serviceCollection.BuildServiceProvider();

var appDispatcher = services.GetRequiredService<IMyAppDispatcher>();
var sampleQueryResponse = await appDispatcher.Dispatch(new SampleQuery(), CancellationToken.None);

Console.WriteLine(sampleQueryResponse.Value);
```