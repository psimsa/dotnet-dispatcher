using DotnetDispatcher.Attributes;
using DotnetDispatcher.Core;
using DotnetDispatcher.Tests.Domain;

namespace DotnetDispatcher.Tests;

[GenerateDispatcher(typeof(GreetingsQuery), typeof(GreetingsQueryHandler))]
[GenerateDispatcher(typeof(QueryWithResultType), typeof(QueryWithResultTypeHandler))]
public partial class QueryDispatcher : DispatcherBase
{
    public QueryDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}