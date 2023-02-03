# dotnet-dispatcher 0.1
A simple dispatcher for .NET Core that uses Roslyn code generation to emit CQRS dispatcher code. Currently in extremely early stage, but I feel there's something to it.

## Why?
I'm a great 'unfriend' of things like reflection, Activator and runtime type determination. When writing CQRS code, I would like to have my dispatcher code generated in compile time, with all optimalizations and other goodies that come with it.

## Enter DotnetDispatcher
To generate a dispatcher is as simple as this:

```csharp
using DotnetDispatcher.Attributes;
using DotnetDispatcher.Core;

namespace ConsoleTester; 

public record SampleQuery : IQuery<SampleQueryResponse>;

public record SampleQueryResponse(string Value);

[GenerateDispatcher(typeof(SampleQuery))]
public partial class MyFirstDispatcher : DispatcherBase
{
    public MyFirstDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
```

This will generate a dispatcher that looks like this:

```csharp
namespace ConsoleTester
{
    public partial interface IMyFirstDispatcher
    {
        Task<SampleQueryResponse> Dispatch(SampleQuery unit, CancellationToken cancellationToken = default);
    }
    public partial class MyFirstDispatcher : IMyFirstDispatcher
    {
        public Task<SampleQueryResponse> Dispatch(SampleQuery unit, CancellationToken cancellationToken = default) =>
            Get<IQueryHandler<SampleQuery, SampleQueryResponse>>().Query(unit, cancellationToken);
    }
}
```
Aside from the Dispatch code, a matching interface is also generated. This allows for easy mocking of the dispatcher as well as using dependency injection. You can have multiple dispatchers in a project, if you want to. Alternatively, you can repeat the use of the GenerateDispatcher attribute with other types that implement `IQuery<TResponse>`, `ICommand<TResponse>`  or `ICommand`.