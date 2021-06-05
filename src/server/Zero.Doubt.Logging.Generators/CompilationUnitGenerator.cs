using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zero.Doubt.Logging.Generators
{
    public static class CompilationUnitGenerator
    {
        public static CompilationUnitSyntax GenerateCompilationUnit(
            string namespaceName, 
            IEnumerable<MemberDeclarationSyntax> members, 
            IEnumerable<IHaveReferencedTypes> typeReferences,
            List<string> messages)
        {
            var usingDirectives = GetReferencedNamespaces().Select(GetUsingDirectiveSyntax);
            var unitSyntax = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(usingDirectives))
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName("GeneratedCode")).WithMembers(
                            SyntaxFactory.List<MemberDeclarationSyntax>(
                                members.ToArray()
                            )
                        )
                    )
                );

            return unitSyntax;
            
            UsingDirectiveSyntax GetUsingDirectiveSyntax(INamespaceSymbol namespaceSymbol)
            {
                var qualifiedName = GetQualifiedName(namespaceSymbol);
                return SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(qualifiedName));
                
                string GetQualifiedName(INamespaceSymbol symbol)
                {
                    var prefix = symbol.ContainingNamespace != null
                        ? GetQualifiedName(symbol.ContainingNamespace)
                        : string.Empty;
                    return prefix.Length > 0
                        ? prefix + "." + symbol.Name
                        : symbol.Name;
                }
            }
            
            INamespaceSymbol[] GetReferencedNamespaces()
            {
                var allTypes = new List<ITypeSymbol>();
                
                foreach (var typeRef in typeReferences)
                {
                    typeRef.IncludeReferencedTypes(allTypes);
                }

                return allTypes
                    .Select(t => t.ContainingNamespace)
                    .Distinct(SymbolEqualityComparer.Default)
                    .Cast<INamespaceSymbol>()
                    .ToArray();
            }
        }
    }
}