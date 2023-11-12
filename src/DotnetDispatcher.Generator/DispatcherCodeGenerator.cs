using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotnetDispatcher.Generator;

[Generator]
public class DispatcherCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        RegisterCodeGenerator(context);
    }

    internal static void RegisterCodeGenerator(IncrementalGeneratorInitializationContext context)
    {
        var generateDispatcherItems = context.SyntaxProvider
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

        var codeToAdd = new Dictionary<string, string>();

        foreach (var queryGenerationMetadata in metadata.OfType<DispatcherGenerationMetadata>())
        {
            var code = GenerateCode(queryGenerationMetadata);
            codeToAdd.Add(queryGenerationMetadata.QueryHandler.ToDisplayString(), code);
        }

        foreach (var item in codeToAdd)
            context.AddSource($"{item.Key}.g.cs", SourceText.From(item.Value, Encoding.UTF8));
    }

    private static string GenerateCode(DispatcherGenerationMetadata metadata)
    {
        var fullQueryName = metadata.QuerySymbol.ToDisplayString();
        var fullResponseName = metadata.ResponseSymbol?.ToDisplayString();

        var namespaceImports = new[]
            {
                metadata.QuerySymbol.ContainingNamespace.ToDisplayString(),
                metadata.ResponseSymbol?.ContainingNamespace.ToDisplayString()
            }
            .Where(item =>
                !string.IsNullOrWhiteSpace(item) &&
                item != metadata.DispatcherSymbol.ContainingNamespace.ToDisplayString())
            .Distinct()
            .Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(ns)))
            .Union(new[] { SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("DotnetDispatcher.Core")) });

        var cancellationTokenParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken"))
            .WithType(SyntaxFactory.IdentifierName("CancellationToken"))
            .WithDefault(
                SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)));

        NameSyntax returnTypeSyntax = fullResponseName is null
            ? SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Task"))
            : SyntaxFactory.GenericName(SyntaxFactory.Identifier("Task"))
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.IdentifierName(fullResponseName))));

        var dispatchMethodDeclarationSyntax = SyntaxFactory.MethodDeclaration(
                returnTypeSyntax,
                SyntaxFactory.Identifier("Dispatch"))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("unit"))
                    .WithType(SyntaxFactory.IdentifierName(fullQueryName)))
            .AddParameterListParameters(
                cancellationTokenParameter
            );
        var dispatcherInterface = SyntaxFactory.InterfaceDeclaration($"I{metadata.DispatcherSymbol.Name}")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword))
            .AddMembers(
                dispatchMethodDeclarationSyntax
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            );

        var statement = "";
        switch (metadata.CqrsType)
        {
            case CqrsType.Query:
                statement = $"Get<IQueryHandler<{fullQueryName}, {fullResponseName}>>().Query(unit, cancellationToken)";
                break;
            case CqrsType.Command when !string.IsNullOrEmpty(fullResponseName):
                statement =
                    $"Get<ICommandHandler<{fullQueryName}, {fullResponseName}>>().Execute(unit, cancellationToken)";
                break;
            case CqrsType.Command:
                statement = $"Get<ICommandHandler<{fullQueryName}>>().Execute(unit, cancellationToken)";
                break;
        }

        var cu = SyntaxFactory.CompilationUnit()
            .AddUsings(namespaceImports.ToArray())
            .AddMembers(
                SyntaxFactory
                    .NamespaceDeclaration(
                        SyntaxFactory.IdentifierName(metadata.DispatcherSymbol.ContainingNamespace.ToDisplayString()))
                    .AddMembers(
                        dispatcherInterface,
                        SyntaxFactory.ClassDeclaration(metadata.DispatcherSymbol.Name)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                            .AddBaseListTypes(
                                SyntaxFactory.SimpleBaseType(
                                    SyntaxFactory.IdentifierName($"I{metadata.DispatcherSymbol.Name}")))
                            .AddMembers(
                                dispatchMethodDeclarationSyntax
                                    .WithExpressionBody(
                                        SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression(statement))
                                    )
                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            )
                    )
            );

        return cu.NormalizeWhitespace().ToFullString();
    }

    private static DispatcherGenerationMetadata? GetQueryDefinitionOrNull(GeneratorSyntaxContext context,
        CancellationToken token)
    {
        var attributeSyntax = (AttributeSyntax)context.Node;

        var dispatcherTypeSymbol = attributeSyntax.Parent?.Parent switch
        {
            ClassDeclarationSyntax classDeclaration =>
                context.SemanticModel.GetDeclaredSymbol(classDeclaration),
            _ => null
        };
        if (dispatcherTypeSymbol is null)
            return null;

        var attributeArguments = attributeSyntax.ArgumentList?.Arguments;
        var queryType = attributeArguments?.FirstOrDefault()?.Expression switch
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

        var cqrsInterface = queryType.AllInterfaces.FirstOrDefault(_ =>
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