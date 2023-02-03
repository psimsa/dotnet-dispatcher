using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTester.cqrs;
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

[GenerateDispatcher(typeof(MyQuery1), typeof(MyQuery1Handler))]
[GenerateDispatcher(typeof(MyQuery2))]
public partial class MySecondDispatcher : DispatcherBase
{
    public MySecondDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}