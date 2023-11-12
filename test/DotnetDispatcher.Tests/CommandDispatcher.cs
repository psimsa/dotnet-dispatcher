using DotnetDispatcher.Attributes;
using DotnetDispatcher.Core;
using DotnetDispatcher.Tests.Domain;

namespace DotnetDispatcher.Tests;

[GenerateDispatcher(typeof(DeleteDatabaseCommand), typeof(DeleteDatabaseCommandHandler))]
[GenerateDispatcher(typeof(HelloWithoutReturnTypeCommand), typeof(HelloWithoutReturnTypeCommandHandler))]
public partial class CommandDispatcher : DispatcherBase
{
    public CommandDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}