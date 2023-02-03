using System;

namespace DotnetDispatcher.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Class, AllowMultiple = true)]
    public class GenerateDispatcherAttribute : Attribute
    {
        private readonly Type _queryType;
        private readonly Type _handlerType;

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
}