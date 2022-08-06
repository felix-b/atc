using System.Collections.Generic;
using Atc.Telemetry.Generators.Descriptions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atc.Telemetry.Generators.SyntaxGenerators;

public static class ImplementationFacadeSyntaxGenerator
{
    public static ClassDeclarationSyntax GenerateClass(string name, IReadOnlyList<TelemetryDescription> telemetries)
    {
        var codePathMapGenerator = new ImplementationMapSyntaxGenerator(CodePathTelemetrySyntaxGenerator.GeneratorId, telemetries);
        codePathMapGenerator.AddDependency("environment", IdentifierName("CodePathEnvironment"));

        var noopMapGenerator = new ImplementationMapSyntaxGenerator(NoopTelemetrySyntaxGenerator.GeneratorId, telemetries);
        var testDoubleMapGenerator = new ImplementationMapSyntaxGenerator(TestDoubleTelemetrySyntaxGenerator.GeneratorId, telemetries);

        return ClassDeclaration(name)
            .WithModifiers(TokenList(new[] {
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.StaticKeyword)
            }))
            .WithMembers(List<MemberDeclarationSyntax>(new MemberDeclarationSyntax[] {
                codePathMapGenerator.GenerateCreateTelemetryMethod(),
                noopMapGenerator.GenerateCreateTelemetryMethod(),
                testDoubleMapGenerator.GenerateCreateTelemetryMethod(),

                codePathMapGenerator.GenerateGetMapMethod(),
                noopMapGenerator.GenerateGetMapMethod(),
                testDoubleMapGenerator.GenerateGetMapMethod(),

                codePathMapGenerator.GenerateMapClass(),
                noopMapGenerator.GenerateMapClass(),
                testDoubleMapGenerator.GenerateMapClass(),
            }));
    }
}