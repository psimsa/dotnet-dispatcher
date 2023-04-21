using ConsoleTester.cqrs;
using DotnetDispatcher.Attributes;
using DotnetDispatcher.Core;

namespace ConsoleTester;

[GenerateDispatcher(typeof(MyCommand1), typeof(MyCommand1Handler))]
[GenerateDispatcher(typeof(SampleQueryWithGenerics), typeof(SampleQueryWithGenericsHandler))]
public partial class MySecondDispatcher : DispatcherBase
{
    public MySecondDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
