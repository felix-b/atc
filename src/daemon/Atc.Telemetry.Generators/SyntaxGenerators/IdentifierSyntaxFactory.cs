using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atc.Telemetry.Generators.SyntaxGenerators;

public static class IdentifierSyntaxFactory
{
    private static readonly HashSet<string> __csharpKeywords = new HashSet<string>(new[] {
        "event",
        "switch",
        "case",
        "throw"
        //TODO: add the rest of the keywords
    });

    public static IdentifierNameSyntax SafeIdentifierName(string name)
    {
        var safeName = __csharpKeywords.Contains(name)
            ? '@' + name
            : name;

        return SyntaxFactory.IdentifierName(safeName);
    }

    public static SyntaxToken SafeIdentifier(string name)
    {
        var safeName = __csharpKeywords.Contains(name)
            ? '@' + name
            : name;

        return SyntaxFactory.Identifier(safeName);
    }
}
