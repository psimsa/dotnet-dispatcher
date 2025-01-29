using System;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMember.Global

namespace DotnetDispatcher;

public abstract class DispatcherBase
{
    private readonly IServiceProvider _serviceProvider;

    protected DispatcherBase(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected T Get<T>()
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}
