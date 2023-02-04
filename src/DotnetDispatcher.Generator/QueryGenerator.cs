using System.Collections.Generic;
using System.Collections.Immutable;
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
        ImmutableArray<DispatcherGenerationMetadata> metadata)
    {
        if (metadata.IsDefaultOrEmpty)
            return;

        var codeToAdd = new Dictionary<string, string>();

        foreach (var queryGenerationMetadata in metadata)
        {
            var fullName =
                $"{queryGenerationMetadata.Namespace}.{queryGenerationMetadata.DispatcherName}.{queryGenerationMetadata.QuerySymbol.Name}";
            var code = GenerateQueryDispatchingCode(queryGenerationMetadata);
            codeToAdd.Add(fullName, code);
        }

        foreach (var item in codeToAdd)
        {
            context.AddSource($"{item.Key}.g.cs", SourceText.From(item.Value, Encoding.UTF8));
        }
    }

    private static string GenerateQueryDispatchingCode(DispatcherGenerationMetadata metadata)
    {
        var fullQueryName = metadata.QuerySymbol.ToDisplayString();
        var fullResponseName = metadata.ResponseSymbol?.ToDisplayString();

        var namespaceImports = new[]
        {
            metadata.QuerySymbol.ContainingNamespace.ToDisplayString(),
            metadata.ResponseSymbol?.ContainingNamespace.ToDisplayString()
        }.Where(item => !string.IsNullOrWhiteSpace(item) && item != metadata.Namespace).Distinct();

        var sb = new IndentedStringBuilder();
        sb.AppendLine("using DotnetDispatcher.Core;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        foreach (var namespaceImport in namespaceImports)
        {
           // sb.AppendLine($"using {namespaceImport};");
        }

        sb.AppendLine();

        sb.AppendLine($"namespace {metadata.Namespace}");
        sb.AppendLine("{");
        sb.Indent();

        sb.AppendLine($"public partial interface I{metadata.DispatcherName}");
        sb.AppendLine("{");
        sb.Indent();
        switch (metadata.CqrsType)
        {
            case CqrsType.Query:
                sb.AppendLine(
                    $"Task<{fullResponseName}> Dispatch({fullQueryName} unit, CancellationToken cancellationToken = default);");
                break;
            case CqrsType.Command when !string.IsNullOrEmpty(fullResponseName):
                sb.AppendLine(
                    $"Task<{fullResponseName}> Dispatch({fullQueryName} unit, CancellationToken cancellationToken = default);");
                break;
            case CqrsType.Command:
                sb.AppendLine(
                    $"Task Dispatch({fullQueryName} unit, CancellationToken cancellationToken = default);");
                break;
        }
        sb.Unindent();
        sb.AppendLine("}");

        sb.AppendLine($"public partial class {metadata.DispatcherName} : I{metadata.DispatcherName}");
        sb.AppendLine("{");
        sb.Indent();

        switch (metadata.CqrsType)
        {
            case CqrsType.Query:
                sb.AppendLine(
                    $"public Task<{fullResponseName}> Dispatch({fullQueryName} unit, CancellationToken cancellationToken = default) =>");
                sb.Indent();
                sb.AppendLine($"Get<IQueryHandler<{fullQueryName}, {fullResponseName}>>().Query(unit, cancellationToken);");
                break;
            case CqrsType.Command when !string.IsNullOrEmpty(fullResponseName):
                sb.AppendLine(
                    $"public Task<{fullResponseName}> Dispatch({fullQueryName} unit, CancellationToken cancellationToken = default) =>");
                sb.Indent();
                sb.AppendLine($"Get<ICommandHandler<{fullQueryName}, {fullResponseName}>>().Execute(unit, cancellationToken);");
                break;
            case CqrsType.Command:
                sb.AppendLine(
                    $"public Task Dispatch({fullQueryName} unit, CancellationToken cancellationToken = default) =>");
                sb.Indent();
                sb.AppendLine($"Get<ICommandHandler<{fullQueryName}>>().Execute(unit, cancellationToken);");
                break;
        }
        sb.Unindent();
        sb.Unindent();
        sb.AppendLine("}");

        sb.Unindent();
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static DispatcherGenerationMetadata? GetQueryDefinitionOrNull(GeneratorSyntaxContext context,
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


        /*INamedTypeSymbol? handlerType = null;
        if (attributeArguments?.Count == 2)
        {
            handlerType = attributeArguments?.Skip(1).FirstOrDefault()?.Expression switch
            {
                TypeOfExpressionSyntax typeOfExpressionSyntax =>
                    context.SemanticModel.GetTypeInfo(typeOfExpressionSyntax.Type).Type as INamedTypeSymbol,
                _ => null
            };
        }*/

        var cqrsInterface = queryType.AllInterfaces.FirstOrDefault(_ =>
            (_.Name == "IQuery" && _.TypeArguments.Length == 1) ||
            (_.Name == "ICommand" && _.TypeArguments.Length < 2));

        if (cqrsInterface is null)
            return null;

        /*var cqrsResponse = cqrsInterface switch
        {
            var _ when cqrsInterface.Name == "IQuery" => cqrsInterface.TypeArguments[0] as INamedTypeSymbol,
            var _ when cqrsInterface is {Name: "ICommand", TypeArguments.Length: 1} =>
                cqrsInterface.TypeArguments[0] as INamedTypeSymbol,
            var _ when cqrsInterface is {Name: "ICommand", TypeArguments.Length: 0} => null as INamedTypeSymbol,
            _ => null
        };*/

        INamedTypeSymbol? queryResponse = null;
        if (cqrsInterface.TypeArguments.Length == 1)
            queryResponse = cqrsInterface.TypeArguments[0] as INamedTypeSymbol;

        return new DispatcherGenerationMetadata(typeSymbol.ContainingNamespace.ToDisplayString(), typeSymbol.Name,
            queryType,
            queryResponse, cqrsInterface.Name == "IQuery" ? CqrsType.Query : CqrsType.Command);
    }
}