using System;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDispatcher.Core;

public abstract class DispatcherBase
{
    protected readonly IServiceProvider ServiceProvider;
    protected T Get<T>() => ServiceProvider.GetRequiredService<T>();

    protected DispatcherBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}