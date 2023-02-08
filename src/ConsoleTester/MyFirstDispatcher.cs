using DotnetDispatcher.Attributes;
using DotnetDispatcher.Core;

namespace ConsoleTester;

[GenerateDispatcher(typeof(SampleQuery), typeof(SampleQueryHandler))]
[GenerateDispatcher(typeof(SampleQueryWithGenerics), typeof(SampleQueryWithGenericsHandler))]
public partial class MyFirstDispatcher : DispatcherBase
{
    public MyFirstDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}