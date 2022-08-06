using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atc.Telemetry.Generators;

public static class SymbolExtensions
{
    public static readonly SymbolDisplayFormat CSharpSymbolDisplayFormat = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);
        
    public static bool InheritsFrom(this ITypeSymbol? symbol, string parentMetadataName)
    {
        while (symbol != null)
        {
            if (symbol.GetFullyQualifiedMetadataName() == parentMetadataName)
            {
                return true;
            }
                
            symbol = symbol.BaseType;
        }

        return false;
    }

    public static string GetFullNamespaceName(this ISymbol symbol)
    {
        var namespaceHierarchy = new List<string>();
            
        for (
            var namespaceSymbol = symbol.ContainingNamespace;
            namespaceSymbol != null && !namespaceSymbol.IsGlobalNamespace;
            namespaceSymbol = namespaceSymbol.ContainingNamespace)
        {
            namespaceHierarchy.Add(namespaceSymbol.Name);
        }

        return string.Join(".", Enumerable.Reverse(namespaceHierarchy));
    }

    public static string GetFullyQualifiedMetadataName(this INamespaceOrTypeSymbol symbol, char nestedTypeSeparator = '+')
    {
        var result = new StringBuilder(symbol.MetadataName, capacity: 255);
            
        for (
            var outerSymbol = symbol.ContainingSymbol;
            outerSymbol != null && !outerSymbol.IsGlobalNamespace();
            outerSymbol = outerSymbol.ContainingSymbol)
        {
            result.Insert(0, outerSymbol is INamespaceSymbol ? '.' : nestedTypeSeparator);
            result.Insert(0, outerSymbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        }

        return result.ToString();
    }

    public static string GetSystemTypeMetadataName(this INamedTypeSymbol symbol)
    {
        var result = new StringBuilder(capacity: 255);
        AppendSystemTypeMetadataName(symbol, result);            
        return result.ToString();
    }
        
    public static bool IsGlobalNamespace(this ISymbol symbol)
    {
        return (symbol is INamespaceSymbol ns && ns.IsGlobalNamespace);
    }

    public static bool HasSourceCode(this ISymbol symbol)
    {
        return (symbol.DeclaringSyntaxReferences.Length > 0);
    }

    public static TypeSyntax GetQualifiedNameSyntax(this ITypeSymbol type)
    {
        if (IsSystemNullable(type, out var nullableUnderlyingType))
        {
            return SyntaxFactory.NullableType(GetQualifiedNameSyntax(nullableUnderlyingType!));
        }
            
        var syntax = TryGetKeywordSyntax(type) ?? GetFullNameSyntax(type);
            
        return type.NullableAnnotation == NullableAnnotation.Annotated
            ? SyntaxFactory.NullableType(syntax)
            : syntax;

        static TypeSyntax? TryGetKeywordSyntax(ITypeSymbol type)
        {
            return type.SpecialType switch {
                SpecialType.System_Void => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                SpecialType.System_Boolean => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                SpecialType.System_String => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                SpecialType.System_Int32 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                SpecialType.System_UInt32 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword)),
                SpecialType.System_Int64 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword)),
                SpecialType.System_UInt64 => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword)),
                SpecialType.System_Double => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword)),
                SpecialType.System_Single => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
                SpecialType.System_Decimal => SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword)),
                _ => null
            };
        }

        static bool IsSystemNullable(ITypeSymbol type, out ITypeSymbol? underlyingType)
        {
            var nullable = (
                type is INamedTypeSymbol namedType &&
                type.NullableAnnotation == NullableAnnotation.Annotated &&
                type.TypeKind == TypeKind.Struct &&
                namedType.IsGenericType == true &&
                namedType.TypeArguments.Length == 1);

            if (nullable)
            {
                underlyingType = ((INamedTypeSymbol) type).TypeArguments[0];
                return true;
            }

            underlyingType = null;
            return false;
        }
            
        static TypeSyntax GetFullNameSyntax(ITypeSymbol type)
        {
            var nameParts = new List<string>(capacity: 10) {
                type.Name
            };
            
            for (var symbol = type.ContainingSymbol;
                 symbol != null && !symbol.IsGlobalNamespace();
                 symbol = symbol.ContainingSymbol)
            {
                nameParts.Add(symbol.Name);
            }
            
            NameSyntax syntax = SyntaxFactory.IdentifierName(nameParts[nameParts.Count - 1]);
            
            for (int i = nameParts.Count - 2; i >= 0; i--)
            {
                syntax = SyntaxFactory.QualifiedName(syntax, SyntaxFactory.IdentifierName(nameParts[i]));
            }

            return syntax;
        }
    }
        
    private static void AppendSystemTypeMetadataName(INamedTypeSymbol symbol, StringBuilder output, bool isContainingType = false)
    {
        if (!ShouldApplySystemTypeNameLogic(symbol))
        {
            output.Append(symbol.ToDisplayString(CSharpSymbolDisplayFormat));
            return;
        }

        AppendSystemTypeNameQualifiers(symbol, output);

        output.Append(symbol.Name);

        if (symbol.IsGenericType)
        {
            AppendSystemTypeNameGenerics(symbol, output);
        }

        if (!isContainingType)
        {
            AppendSystemTypeNameAssembly(symbol, output);
        }
    }

    private static bool ShouldApplySystemTypeNameLogic(INamedTypeSymbol symbol)
    {
        return (
            symbol.TypeKind == TypeKind.Class || 
            symbol.TypeKind == TypeKind.Struct || 
            symbol.TypeKind == TypeKind.Interface || 
            symbol.IsGenericType);
    }

    private static void AppendSystemTypeNameQualifiers(INamedTypeSymbol symbol, StringBuilder output)
    {
        if (symbol.ContainingSymbol is INamedTypeSymbol containingType &&
            (containingType.TypeKind == TypeKind.Class || containingType.TypeKind == TypeKind.Struct))
        {
            AppendSystemTypeMetadataName(containingType, output, isContainingType: true);
            output.Append('+');
        }
        else if (symbol.ContainingNamespace != null && !symbol.ContainingNamespace.IsGlobalNamespace)
        {
            output.Append(symbol.ContainingNamespace.ToDisplayString(CSharpSymbolDisplayFormat));
            output.Append('.');
        }
    }

    private static void AppendSystemTypeNameGenerics(INamedTypeSymbol symbol, StringBuilder output)
    {
        output.Append('`');
        output.Append(symbol.Arity);

        if (symbol.TypeArguments != null && symbol.TypeArguments.All(t => t is INamedTypeSymbol))
        {
            output.Append('[');

            for (int i = 0; i < symbol.TypeArguments.Length; i++)
            {
                if (i > 0)
                {
                    output.Append(',');
                }

                output.Append('[');
                AppendSystemTypeMetadataName((INamedTypeSymbol)symbol.TypeArguments[i], output);
                output.Append(']');
            }

            output.Append(']');
        }
    }

    private static void AppendSystemTypeNameAssembly(INamedTypeSymbol symbol, StringBuilder output)
    {
        if (symbol.SpecialType == SpecialType.None && symbol.ContainingAssembly != null)
        {
            output.Append(',');
            output.Append(symbol.ContainingAssembly.Name);
        }
    }
}