using DotnetDispatcher.Attributes;
using DotnetDispatcher.Core;
using DotnetDispatcher.Tests.Domain;

namespace DotnetDispatcher.Tests;

[GenerateDispatcher(typeof(GreetingsQuery), typeof(GreetingsQueryHandler))]
public partial class TestDispatcher : DispatcherBase
{
    public TestDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}