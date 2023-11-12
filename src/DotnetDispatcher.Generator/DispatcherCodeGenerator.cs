using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using sf = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DotnetDispatcher.Generator;

[Generator]
public class DispatcherCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        RegisterCodeGenerator(context);
    }

    private static void RegisterCodeGenerator(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<DispatcherGenerationMetadata?>> generateDispatcherItems = context.SyntaxProvider
            .CreateSyntaxProvider(
                (sn, ct) => Helpers.IsNamedAttribute(sn, ct, Constants.GenerateDispatcherAttributeFull,
                    Constants.GenerateDispatcherAttributeShort), GetQueryDefinitionOrNull)
            .Where(_ => _ is not null)
            .Collect();
        context.RegisterSourceOutput(generateDispatcherItems, GenerateDispatcherItems);
    }

    private static void GenerateDispatcherItems(SourceProductionContext context,
        ImmutableArray<DispatcherGenerationMetadata?> metadata)
    {
        if (metadata.IsDefaultOrEmpty)
            return;

        Dictionary<string, string> codeToAdd = new Dictionary<string, string>();

        foreach (DispatcherGenerationMetadata queryGenerationMetadata in metadata.OfType<DispatcherGenerationMetadata>())
        {
            string code = GenerateCode(queryGenerationMetadata);
            codeToAdd.Add(queryGenerationMetadata.QueryHandler.ToDisplayString(), code);
        }

        foreach (KeyValuePair<string, string> item in codeToAdd)
            context.AddSource($"{item.Key}.g.cs", SourceText.From(item.Value, Encoding.UTF8));
    }

    private static string GenerateCode(DispatcherGenerationMetadata metadata)
    {
        string fullQueryName = metadata.QuerySymbol.ToDisplayString();
        string? fullResponseName = metadata.ResponseSymbol?.ToDisplayString();

        IEnumerable<UsingDirectiveSyntax> namespaceImports = new[]
            {
                metadata.QuerySymbol.ContainingNamespace.ToDisplayString(),
                metadata.ResponseSymbol?.ContainingNamespace.ToDisplayString()
            }
            .Where(item =>
                !string.IsNullOrWhiteSpace(item) &&
                item != metadata.DispatcherSymbol.ContainingNamespace.ToDisplayString())
            .Distinct()
            .Select(ns => sf.UsingDirective(sf.IdentifierName(ns!)))
            .Union(new[] { sf.UsingDirective(sf.IdentifierName("DotnetDispatcher.Core")) });

        ParameterSyntax cancellationTokenParameter = sf.Parameter(sf.Identifier("cancellationToken"))
            .WithType(sf.IdentifierName("CancellationToken"))
            .WithDefault(sf.EqualsValueClause(sf.LiteralExpression(SyntaxKind.DefaultLiteralExpression)));

        NameSyntax returnTypeSyntax = fullResponseName is null
            ? sf.IdentifierName(sf.Identifier("Task"))
            : sf.GenericName(sf.Identifier("Task"))
                .WithTypeArgumentList(sf.TypeArgumentList(
                    sf.SingletonSeparatedList<global::Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax>(sf.IdentifierName(fullResponseName))));

        MethodDeclarationSyntax dispatchMethodDeclarationSyntax = sf.MethodDeclaration(
                returnTypeSyntax,
                sf.Identifier("Dispatch"))
            .AddModifiers(sf.Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(
                sf.Parameter(sf.Identifier("unit"))
                    .WithType(sf.IdentifierName(fullQueryName)),
                cancellationTokenParameter
            );

        InterfaceDeclarationSyntax dispatcherInterface = sf.InterfaceDeclaration($"I{metadata.DispatcherSymbol.Name}")
            .AddModifiers(sf.Token(SyntaxKind.PublicKeyword), sf.Token(SyntaxKind.PartialKeyword))
            .AddMembers(dispatchMethodDeclarationSyntax.WithSemicolonToken(sf.Token(SyntaxKind.SemicolonToken)));

        GenericNameSyntax queryTypes = sf
            .GenericName(metadata.CqrsType == CqrsType.Query ? "IQueryHandler" : "ICommandHandler")
            .WithTypeArgumentList(sf.TypeArgumentList().AddArguments(sf.IdentifierName(fullQueryName)));

        if (fullResponseName != null)
            queryTypes = queryTypes.AddTypeArgumentListArguments(sf.IdentifierName(fullResponseName));

        InvocationExpressionSyntax queryExpression = sf.InvocationExpression(
            sf.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                sf.InvocationExpression(sf.GenericName("Get").WithTypeArgumentList(
                    sf.TypeArgumentList(sf.SingletonSeparatedList<TypeSyntax>(queryTypes)))
                ), sf.IdentifierName(metadata.CqrsType == CqrsType.Query ? "Query" : "Execute")),
            sf.ArgumentList().AddArguments(
                sf.Argument(sf.IdentifierName("unit")),
                sf.Argument(sf.IdentifierName("cancellationToken"))
            )
        );

        ClassDeclarationSyntax dispatcherClass = sf.ClassDeclaration(metadata.DispatcherSymbol.Name)
            .AddModifiers(sf.Token(SyntaxKind.PublicKeyword),
                sf.Token(SyntaxKind.PartialKeyword))
            .AddBaseListTypes(sf.SimpleBaseType(sf.IdentifierName($"I{metadata.DispatcherSymbol.Name}")))
            .AddMembers(
                dispatchMethodDeclarationSyntax.WithExpressionBody(sf.ArrowExpressionClause(queryExpression))
                    .WithSemicolonToken(sf.Token(SyntaxKind.SemicolonToken))
            );

        CompilationUnitSyntax compilationUnit = sf.CompilationUnit()
            .AddUsings(namespaceImports.ToArray())
            .AddMembers(sf.NamespaceDeclaration(sf.IdentifierName(metadata.DispatcherSymbol.ContainingNamespace.ToDisplayString()))
                .AddMembers(
                    dispatcherInterface,
                    dispatcherClass
                )
            );

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }

    private static DispatcherGenerationMetadata? GetQueryDefinitionOrNull(GeneratorSyntaxContext context,
        CancellationToken token)
    {
        AttributeSyntax attributeSyntax = (AttributeSyntax)context.Node;

        INamedTypeSymbol? dispatcherTypeSymbol = attributeSyntax.Parent?.Parent switch
        {
            ClassDeclarationSyntax classDeclaration =>
                context.SemanticModel.GetDeclaredSymbol(classDeclaration),
            _ => null
        };
        if (dispatcherTypeSymbol is null)
            return null;

        SeparatedSyntaxList<AttributeArgumentSyntax>? attributeArguments = attributeSyntax.ArgumentList?.Arguments;
        INamedTypeSymbol? queryType = attributeArguments?.FirstOrDefault()?.Expression switch
        {
            TypeOfExpressionSyntax typeOfExpressionSyntax =>
                context.SemanticModel.GetTypeInfo(typeOfExpressionSyntax.Type).Type as INamedTypeSymbol,
            _ => null
        };
        if (queryType is null)
            return null;


        INamedTypeSymbol? handlerType = null;
        if (attributeArguments?.Count == 2)
            handlerType = attributeArguments?.Skip(1).FirstOrDefault()?.Expression switch
            {
                TypeOfExpressionSyntax typeOfExpressionSyntax =>
                    context.SemanticModel.GetTypeInfo(typeOfExpressionSyntax.Type).Type as INamedTypeSymbol,
                _ => null
            };

        INamedTypeSymbol? cqrsInterface = queryType.AllInterfaces.FirstOrDefault(_ =>
            (_.Name == "IQuery" && _.TypeArguments.Length == 1) ||
            (_.Name == "ICommand" && _.TypeArguments.Length < 2));

        if (cqrsInterface is null)
            return null;

        INamedTypeSymbol? queryResponse = null;
        if (cqrsInterface.TypeArguments.Length == 1)
            queryResponse = cqrsInterface.TypeArguments[0] as INamedTypeSymbol;

        return new DispatcherGenerationMetadata(dispatcherTypeSymbol,
            queryType,
            queryResponse,
            cqrsInterface.Name == "IQuery" ? CqrsType.Query : CqrsType.Command, handlerType);
    }
}