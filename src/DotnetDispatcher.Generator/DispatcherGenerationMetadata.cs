using Microsoft.CodeAnalysis;

namespace DotnetDispatcher.Generator;

internal record DispatcherGenerationMetadata(INamedTypeSymbol DispatcherSymbol, INamedTypeSymbol QuerySymbol,
    INamedTypeSymbol? ResponseSymbol, CqrsType CqrsType, INamedTypeSymbol? QueryHandler = null)
{
    public INamedTypeSymbol QuerySymbol { get; } = QuerySymbol;
    public INamedTypeSymbol? ResponseSymbol { get; } = ResponseSymbol;
    public INamedTypeSymbol DispatcherSymbol { get; } = DispatcherSymbol;
    public CqrsType CqrsType { get; } = CqrsType;
    public INamedTypeSymbol? QueryHandler { get; } = QueryHandler;
}

internal enum CqrsType
{
    Query,
    Command
}