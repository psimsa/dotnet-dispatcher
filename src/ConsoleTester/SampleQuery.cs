using ConsoleTester.cqrs;
using DotnetDispatcher.Attributes;
using DotnetDispatcher.Core;

namespace ConsoleTester;

public record SampleQuery : IQuery<SampleQueryResponse>;

public record SampleQueryResponse(string Value);

[GenerateDispatcher(typeof(SampleQuery))]
[GenerateDispatcher(typeof(SampleQueryWithGenerics))]
public partial class MyFirstDispatcher : DispatcherBase
{
    public MyFirstDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}

[GenerateDispatcher(typeof(MyQuery1), typeof(MyQuery1Handler))]
[GenerateDispatcher(typeof(MyQuery2))]
[GenerateDispatcher(typeof(MyCommand1))]
[GenerateDispatcher(typeof(MyCommand2))]
public partial class MySecondDispatcher : DispatcherBase
{
    public MySecondDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}