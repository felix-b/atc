using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using Zero.Doubt.Logging.Generators;

namespace Zero.Doubt.Logging.Tests.Generators
{
    [TestFixture]
    public class SymbolExtensionsTests
    {
        [Test]
        public void CanGenerateNullableEnumTypeSyntax()
        {
            const string sourceCode =
                "enum MyEnum { First, Second }\n\n" +
                "class A { void F(MyEnum? e) { } public static void Main() { } }\n\n";

            var compilation = CompileCode(sourceCode, out var syntaxTree);
            var diagnostic = compilation.GetDiagnostics();
            var methodSyntax = syntaxTree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First(s => s.Identifier.Text == "F");
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var method = (IMethodSymbol)semanticModel.GetDeclaredSymbol(methodSyntax);

            var typeSymbol = method.Parameters[0].Type;
            var generatedTypeSyntax = typeSymbol.GetQualifiedNameSyntax();

            generatedTypeSyntax.ToString().Should().Be("MyEnum?");
        }
        
        private Compilation CompileCode(string sourceCode, out SyntaxTree syntaxTree)
        {
            syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            var compilation = CSharpCompilation.Create("HelloWorld")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(syntaxTree);

            return compilation;
        }
    }
}