using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DotnetDispatcher.Generator;

[Generator]
public class DispatcherRegistrationsCodeGenerator : IIncrementalGenerator
{
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

        foreach (var queryGenerationMetadata in metadata.OfType<DispatcherGenerationMetadata>().GroupBy(_ => _.DispatcherName))
        {
            var fullName = $"{queryGenerationMetadata.Key}.Registrations";
            var code = GenerateCode(queryGenerationMetadata);
            codeToAdd.Add(fullName, code);
        }

        foreach (var item in codeToAdd)
        {
            context.AddSource($"{item.Key}.g.cs", SourceText.From(item.Value, Encoding.UTF8));
        }
    }

    private static string GenerateCode(IGrouping<string, DispatcherGenerationMetadata> metadata)
    {
        var sb = new IndentedStringBuilder();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();

        sb.AppendLine($"namespace DotnetDispatcher.Core");
        sb.AppendLine("{");
        sb.Indent();

        sb.AppendLine($"public static class Register{metadata.Key}AndHandlersExtensions");
        sb.AppendLine("{");
        sb.Indent();

        sb.AppendLine($"public static void Register{metadata.Key}AndHandlers(this IServiceCollection services)");
        sb.AppendLine("{");
        sb.Indent();
        string? dispatcherNamespace = null;
        foreach (var dispatcherGenerationMetadata in metadata)
        {
            dispatcherNamespace ??= dispatcherGenerationMetadata.Namespace;
            if (dispatcherGenerationMetadata.QueryHandler is not null)
            {
                switch (dispatcherGenerationMetadata.CqrsType)
                {
                    case CqrsType.Query:
                        sb.AppendLine(
                            $"services.AddTransient(typeof(IQueryHandler<{dispatcherGenerationMetadata.QuerySymbol.ToDisplayString()}, {dispatcherGenerationMetadata.ResponseSymbol}>), typeof({dispatcherGenerationMetadata.QueryHandler.ToDisplayString()}));");
                        break;
                    case CqrsType.Command when dispatcherGenerationMetadata.ResponseSymbol is not null:
                        sb.AppendLine(
                            $"services.AddTransient(typeof(ICommandHandler<{dispatcherGenerationMetadata.QuerySymbol.ToDisplayString()}, {dispatcherGenerationMetadata.ResponseSymbol}>), typeof({dispatcherGenerationMetadata.QueryHandler.ToDisplayString()}));");
                        break;
                    case CqrsType.Command:
                        sb.AppendLine(
                            $"services.AddTransient(typeof(ICommandHandler<{dispatcherGenerationMetadata.QuerySymbol.ToDisplayString()}>), typeof({dispatcherGenerationMetadata.QueryHandler.ToDisplayString()}));");
                        break;
                }
            }
        }

        sb.AppendLine(
            $"services.AddSingleton<{dispatcherNamespace}.I{metadata.Key}, {dispatcherNamespace}.{metadata.Key}>();");

        sb.Unindent();

        sb.AppendLine("}");

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
