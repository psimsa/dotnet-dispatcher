using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotnetDispatcher.Generator;

internal static class Helpers
{
    internal static string? ExtractName(NameSyntax? name)
    {
        return name switch
        {
            SimpleNameSyntax ins => ins.Identifier.Text,
            QualifiedNameSyntax qns => qns.Right.Identifier.Text,
            _ => null
        };
    }

    internal static bool IsNamedAttribute(SyntaxNode syntaxNode,
        CancellationToken _, params string[] attributes)
    {
        if (syntaxNode is not AttributeSyntax attribute)
            return false;

        var name = ExtractName(attribute.Name);
        return name is not null && attributes.Contains(name);
    }
}