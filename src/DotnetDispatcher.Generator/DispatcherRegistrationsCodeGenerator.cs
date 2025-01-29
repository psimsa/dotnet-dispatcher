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
public class DispatcherRegistrationsCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        RegisterCodeGenerator(context);
    }

    internal static void RegisterCodeGenerator(IncrementalGeneratorInitializationContext context)
    {
        var generateRegistrationsItems = context.SyntaxProvider
            .CreateSyntaxProvider(
                (sn, ct) => Helpers.IsNamedAttribute(sn, ct, Constants.GenerateDispatcherAttributeFull,
                    Constants.GenerateDispatcherAttributeShort), GetQueryDefinitionOrNull)
            .Where(_ => _ is not null)
            .Collect();
        context.RegisterSourceOutput(generateRegistrationsItems, GenerateRegistrationsItems);
    }

    private static void GenerateRegistrationsItems(SourceProductionContext context,
        ImmutableArray<DispatcherGenerationMetadata?> metadata)
    {
        if (metadata.IsDefaultOrEmpty)
            return;

        var codeToAdd = new Dictionary<string, string>();

        foreach (var queryGenerationMetadata in metadata.OfType<DispatcherGenerationMetadata>()
                     .GroupBy(_ => _.DispatcherSymbol.Name))
        {
            var fullName = $"{queryGenerationMetadata.Key}.Registrations";
            var code = GenerateCode(queryGenerationMetadata);
            codeToAdd.Add(fullName, code);
        }

        foreach (var item in codeToAdd)
        {
            var tree = sf.ParseSyntaxTree(item.Value);
            var formatted = tree.GetRoot().NormalizeWhitespace().ToFullString();
            context.AddSource($"{item.Key}.g.cs", SourceText.From(formatted, Encoding.UTF8));
        }
    }

    private static string GenerateCode(IGrouping<string, DispatcherGenerationMetadata> metadata)
    {
        var bodySyntaxStatements = new SyntaxList<StatementSyntax>();

        string? dispatcherNamespace = null;
        foreach (var dispatcherGenerationMetadata in metadata)
        {
            dispatcherNamespace ??= dispatcherGenerationMetadata.DispatcherSymbol.ContainingNamespace.ToDisplayString();
            if (dispatcherGenerationMetadata.QueryHandler is not null)
            {
                var statement = "{}";
                switch (dispatcherGenerationMetadata.CqrsType)
                {
                    case CqrsType.Query:
                        statement =
                            $"services.AddTransient(typeof(IQueryHandler<{dispatcherGenerationMetadata.QuerySymbol.ToDisplayString()}, {dispatcherGenerationMetadata.ResponseSymbol}>), typeof({dispatcherGenerationMetadata.QueryHandler.ToDisplayString()}));";
                        break;
                    case CqrsType.Command when dispatcherGenerationMetadata.ResponseSymbol is not null:
                        statement =
                            $"services.AddTransient(typeof(ICommandHandler<{dispatcherGenerationMetadata.QuerySymbol.ToDisplayString()}, {dispatcherGenerationMetadata.ResponseSymbol}>), typeof({dispatcherGenerationMetadata.QueryHandler.ToDisplayString()}));";
                        break;
                    case CqrsType.Command:
                        statement =
                            $"services.AddTransient(typeof(ICommandHandler<{dispatcherGenerationMetadata.QuerySymbol.ToDisplayString()}>), typeof({dispatcherGenerationMetadata.QueryHandler.ToDisplayString()}));";
                        break;
                }

                bodySyntaxStatements = bodySyntaxStatements.Add(sf.ParseStatement(statement));
            }
        }

        bodySyntaxStatements = bodySyntaxStatements.Add(sf.ParseStatement(
            $"services.AddSingleton(typeof({dispatcherNamespace}.I{metadata.Key}), typeof({dispatcherNamespace}.{metadata.Key}));"));

        var classMembers = new SyntaxList<MemberDeclarationSyntax>()
            .Add(sf.MethodDeclaration(
                    new SyntaxList<AttributeListSyntax>(),
                    sf.TokenList(sf.Token(SyntaxKind.PublicKeyword),
                        sf.Token(SyntaxKind.StaticKeyword)),
                    sf.PredefinedType(sf.Token(SyntaxKind.VoidKeyword)),
                    null,
                    sf.Identifier($"Register{metadata.Key}AndHandlers"),
                    null,
                    sf.ParameterList(),
                    new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                    sf.Block(bodySyntaxStatements),
                    sf.Token(SyntaxKind.None))
                .AddParameterListParameters(
                    sf.Parameter(sf.Identifier("services"))
                        .WithType(sf.IdentifierName("IServiceCollection"))
                        .WithModifiers(sf.TokenList(sf.Token(SyntaxKind.ThisKeyword)))
                )
            );

        var compilationUnit = sf.CompilationUnit()
            .AddUsings(
                sf.UsingDirective(sf.IdentifierName("Microsoft.Extensions.DependencyInjection")),
                sf.UsingDirective(sf.IdentifierName("DotnetDispatcher")))
            .AddMembers(
                sf
                    .NamespaceDeclaration(sf.IdentifierName(metadata.First().DispatcherSymbol
                        .ContainingNamespace.ToDisplayString()))
                    .AddMembers(
                        sf.ClassDeclaration(
                            new SyntaxList<AttributeListSyntax>(),
                            sf.TokenList(sf.Token(SyntaxKind.PublicKeyword),
                                sf.Token(SyntaxKind.StaticKeyword)),
                            sf.Identifier($"Register{metadata.Key}AndHandlersExtensions"),
                            null,
                            null,
                            new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                            classMembers))
            );

        return compilationUnit.WithLeadingTrivia(sf.Comment("/// <autogenerated />")).NormalizeWhitespace()
            .ToFullString();
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