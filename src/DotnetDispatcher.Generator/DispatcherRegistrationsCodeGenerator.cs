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
            var tree = SyntaxFactory.ParseSyntaxTree(item.Value);
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

                bodySyntaxStatements = bodySyntaxStatements.Add(SyntaxFactory.ParseStatement(statement));
            }
        }

        bodySyntaxStatements = bodySyntaxStatements.Add(SyntaxFactory.ParseStatement(
            $"services.AddSingleton(typeof({dispatcherNamespace}.I{metadata.Key}), typeof({dispatcherNamespace}.{metadata.Key}));"));

        var classMembers = new SyntaxList<MemberDeclarationSyntax>()
            .Add(SyntaxFactory.MethodDeclaration(
                    new SyntaxList<AttributeListSyntax>(),
                    SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    null,
                    SyntaxFactory.Identifier($"Register{metadata.Key}AndHandlers"),
                    null,
                    SyntaxFactory.ParameterList(),
                    new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                    SyntaxFactory.Block(bodySyntaxStatements),
                    SyntaxFactory.Token(SyntaxKind.None))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("services"))
                        .WithType(SyntaxFactory.IdentifierName("IServiceCollection"))
                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ThisKeyword)))
                )
            );

        var cu = SyntaxFactory.CompilationUnit()
            .AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Microsoft.Extensions.DependencyInjection")),
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("DotnetDispatcher.Core")))
            .AddMembers(
                SyntaxFactory
                    .NamespaceDeclaration(SyntaxFactory.IdentifierName(metadata.First().DispatcherSymbol
                        .ContainingNamespace.ToDisplayString()))
                    .AddMembers(
                        SyntaxFactory.ClassDeclaration(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                            SyntaxFactory.Identifier($"Register{metadata.Key}AndHandlersExtensions"),
                            null,
                            null,
                            new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                            classMembers))
            );

        var z = cu.NormalizeWhitespace().ToFullString();
        return z;
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