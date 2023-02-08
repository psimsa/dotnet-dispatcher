using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotnetDispatcher.Generator;

[Generator]
public class DispatcherCodeGenerator : IIncrementalGenerator
{
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
            var fullName =
                $"{queryGenerationMetadata.Namespace}.{queryGenerationMetadata.DispatcherName}.{queryGenerationMetadata.QuerySymbol.Name}";
            var code = GenerateCode(queryGenerationMetadata);
            codeToAdd.Add(fullName, code);
        }

        foreach (var item in codeToAdd)
        {
            context.AddSource($"{item.Key}.g.cs", SourceText.From(item.Value, Encoding.UTF8));
        }
    }

    private static string GenerateCode(DispatcherGenerationMetadata metadata)
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

        sb.AppendLine();

        sb.AppendLine($"public partial class {metadata.DispatcherName} : I{metadata.DispatcherName}");
        sb.AppendLine("{");
        sb.Indent();

        switch (metadata.CqrsType)
        {
            case CqrsType.Query:
                sb.AppendLine(
                    $"public Task<{fullResponseName}> Dispatch({fullQueryName} unit, CancellationToken cancellationToken = default) =>");
                sb.Indent();
                sb.AppendLine(
                    $"Get<IQueryHandler<{fullQueryName}, {fullResponseName}>>().Query(unit, cancellationToken);");
                break;
            case CqrsType.Command when !string.IsNullOrEmpty(fullResponseName):
                sb.AppendLine(
                    $"public Task<{fullResponseName}> Dispatch({fullQueryName} unit, CancellationToken cancellationToken = default) =>");
                sb.Indent();
                sb.AppendLine(
                    $"Get<ICommandHandler<{fullQueryName}, {fullResponseName}>>().Execute(unit, cancellationToken);");
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

        var cqrsInterface = queryType.AllInterfaces.FirstOrDefault(_ =>
            (_.Name == "IQuery" && _.TypeArguments.Length == 1) ||
            (_.Name == "ICommand" && _.TypeArguments.Length < 2));

        if (cqrsInterface is null)
            return null;

        INamedTypeSymbol? queryResponse = null;
        if (cqrsInterface.TypeArguments.Length == 1)
            queryResponse = cqrsInterface.TypeArguments[0] as INamedTypeSymbol;

        return new DispatcherGenerationMetadata(typeSymbol.ContainingNamespace.ToDisplayString(), typeSymbol.Name,
            queryType,
            queryResponse, cqrsInterface.Name == "IQuery" ? CqrsType.Query : CqrsType.Command, handlerType);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        RegisterCodeGenerator(context);
    }
}