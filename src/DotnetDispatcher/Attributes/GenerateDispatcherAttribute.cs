using System;

namespace DotnetDispatcher.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GenerateDispatcherAttribute : Attribute
{
    private readonly Type _handlerType;
    private readonly Type _queryType;

    public GenerateDispatcherAttribute(Type queryType)
    {
        _queryType = queryType;
    }

    public GenerateDispatcherAttribute(Type queryType, Type handlerType)
    {
        _queryType = queryType;
        _handlerType = handlerType;
    }
}
