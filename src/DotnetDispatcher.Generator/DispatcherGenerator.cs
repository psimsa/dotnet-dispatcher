using Microsoft.CodeAnalysis;

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