using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotnetDispatcher.Generator;

internal static class QueryGenerator
{
    internal static void RegisterQueryGenerator(IncrementalGeneratorInitializationContext context)
    {
        var generateQueryItems = context.SyntaxProvider
            .CreateSyntaxProvider(
                (sn, ct) => Helpers.IsNamedAttribute(sn, ct, Constants.GenerateDispatcherAttributeFull,
                    Constants.GenerateDispatcherAttributeShort), GetQueryDefinitionOrNull)
            .Where(_ => _ is not null)
            .Collect();
        context.RegisterSourceOutput(generateQueryItems, GenerateQueryItems);
    }

    private static void GenerateQueryItems(SourceProductionContext context,
        ImmutableArray<QueryGenerationMetadata> metadata)
    {
        if (metadata.IsDefaultOrEmpty)
            return;

        var codeToAdd = new Dictionary<string, string>();          

        foreach (var queryGenerationMetadata in metadata)
        {
            var fullName =
                $"{queryGenerationMetadata.Namespace}.{queryGenerationMetadata.DispatcherName}.{queryGenerationMetadata.QuerySymbol.Name}";
            var code = GenerateDispatchingCode(queryGenerationMetadata);
            codeToAdd.Add(fullName, code);
        }

        foreach (var item in codeToAdd)
        {
            context.AddSource($"{item.Key}.g.cs", SourceText.From(item.Value, Encoding.UTF8));
        }
    }

    private static string GenerateDispatchingCode(QueryGenerationMetadata metadata)
    {
        var fullQueryName = metadata.QuerySymbol.Name;
        var fullResponseName = metadata.ResponseSymbol.Name;

        var namespaceImports = new[]
        {
            metadata.QuerySymbol.ContainingNamespace.ToDisplayString(),
            metadata.ResponseSymbol.ContainingNamespace.ToDisplayString()
        }.Where(_ => _ != metadata.Namespace).Distinct();

        var sb = new IndentedStringBuilder();
        sb.AppendLine("using DotnetDispatcher.Core;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        foreach (var namespaceImport in namespaceImports)
        {
            sb.AppendLine($"using {namespaceImport};");
        }

        sb.AppendLine();

        sb.AppendLine($"namespace {metadata.Namespace}");
        sb.AppendLine("{");
        sb.Indent();

        sb.AppendLine($"public partial interface I{metadata.DispatcherName}");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine(
            $"Task<{fullResponseName}> Dispatch({fullQueryName} query, CancellationToken cancellationToken = default);");
        sb.Unindent();
        sb.AppendLine("}");

        sb.AppendLine($"public partial class {metadata.DispatcherName} : I{metadata.DispatcherName}");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine(
            $"public Task<{fullResponseName}> Dispatch({fullQueryName} query, CancellationToken cancellationToken = default) =>");
        sb.Indent();
        sb.AppendLine($"Get<IQueryHandler<{fullQueryName}, {fullResponseName}>>().Query(query, cancellationToken);");
        sb.Unindent();
        sb.Unindent();
        sb.AppendLine("}");

        sb.Unindent();
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static QueryGenerationMetadata? GetQueryDefinitionOrNull(GeneratorSyntaxContext context,
        CancellationToken token)
    {
        var attributeSyntax = (AttributeSyntax) context.Node;

        var typeSymbol = attributeSyntax.Parent?.Parent switch
        {
            ClassDeclarationSyntax classDeclaration =>
                context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol,
            _ => null
        };
        if (typeSymbol is null)
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
        {
            handlerType = attributeArguments?.Skip(1).FirstOrDefault()?.Expression switch
            {
                TypeOfExpressionSyntax typeOfExpressionSyntax =>
                    context.SemanticModel.GetTypeInfo(typeOfExpressionSyntax.Type).Type as INamedTypeSymbol,
                _ => null
            };
        }

        var queryInterface = queryType.AllInterfaces.FirstOrDefault(_ =>
            _.Name == "IQuery" && _.TypeArguments.Length == 1);

        if (queryInterface is null)
            return null;

        var queryResponse = queryInterface.TypeArguments[0] as INamedTypeSymbol;
        if (queryResponse is null)
            return null;

        return new QueryGenerationMetadata(typeSymbol.ContainingNamespace.ToDisplayString(), typeSymbol.Name, queryType,
            queryResponse, handlerType);
    }
}

internal static class Helpers
{
    internal static string? ExtractName(NameSyntax? name) =>
        name switch
        {
            SimpleNameSyntax ins => ins.Identifier.Text,
            QualifiedNameSyntax qns => qns.Right.Identifier.Text,
            _ => null
        };

    internal static bool IsNamedAttribute(SyntaxNode syntaxNode,
        CancellationToken _, params string[] attributes)
    {
        if (syntaxNode is not AttributeSyntax attribute)
            return false;

        var name = Helpers.ExtractName(attribute.Name);
        return name is not null && attributes.Contains(name);
    }
}