using Microsoft.CodeAnalysis;

namespace DotnetDispatcher.Generator;

internal record QueryGenerationMetadata(string Namespace, string DispatcherName, INamedTypeSymbol QuerySymbol, INamedTypeSymbol ResponseSymbol, INamedTypeSymbol? HandlerType)
{
    public INamedTypeSymbol QuerySymbol { get; } = QuerySymbol;
    public INamedTypeSymbol ResponseSymbol { get; } = ResponseSymbol;
    public string Namespace { get; } = Namespace;
    public string DispatcherName { get; } = DispatcherName;
    public INamedTypeSymbol? HandlerType { get; } = HandlerType;
}