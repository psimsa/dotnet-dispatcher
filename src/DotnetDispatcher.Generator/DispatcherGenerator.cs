using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetDispatcher.Generator;

[Generator]
public class DispatcherGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        // Do not remove this commented code. Uncomment when debugging.
        /*if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }*/
#endif // DEBUG

        QueryGenerator.RegisterQueryGenerator(context);
    }
}