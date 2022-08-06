using System.Collections.Generic;
using System.Linq;
using Atc.Telemetry.Generators.Descriptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Atc.Telemetry.Generators.SyntaxGenerators.IdentifierSyntaxFactory;

namespace Atc.Telemetry.Generators.SyntaxGenerators;

public static class TestDoubleTelemetrySyntaxGenerator
{
    public const string GeneratorId = "TestDouble";

    public static ClassDeclarationSyntax GenerateClassSyntax(
        GeneratorExecutionContext context,
        TelemetryDescription metaTelemetry, 
        out string generatedClassName)
    {
        generatedClassName = GetTelemetryClassName(metaTelemetry);
        return CreateClass(generatedClassName);

        ClassDeclarationSyntax CreateClass(string className)
        {
            var classDecl = ClassDeclaration(className)
                .WithModifiers(
                    TokenList(Token(SyntaxKind.PublicKeyword))
                )
                .WithBaseList(BaseList(
                    SeparatedList<BaseTypeSyntax>(new SyntaxNodeOrToken[] {
                        SimpleBaseType(IdentifierName("TelemetryTestDoubleBase")),
                        Token(SyntaxKind.CommaToken),
                        SimpleBaseType(metaTelemetry.InterfaceSymbol.GetQualifiedNameSyntax())
                    })                        
                ));

            var members = CreateMembers(className);
            return classDecl.WithMembers(List(members));
        }

        MemberDeclarationSyntax[] CreateMembers(string className)
        {
            return CreateLogMethodMembers();
        }
            
        MemberDeclarationSyntax[] CreateLogMethodMembers()
        {
            return metaTelemetry.Methods
                .Select(CreateTelemetryMethod)
                .Cast<MemberDeclarationSyntax>()
                .ToArray();
        }

        MethodDeclarationSyntax CreateTelemetryMethod(TelemetryMethodDescription metaMethod)
        {
            return MethodDeclaration(
                    metaMethod.Symbol.ReturnType.GetQualifiedNameSyntax(),
                    Identifier(metaMethod.Name)
                )
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword)
                ))
                .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(
                    CreateParameters()
                )))
                .WithBody(
                    CreateLogMethodBody(metaMethod)
                );

            IEnumerable<SyntaxNodeOrToken> CreateParameters()
            {
                var result = new List<SyntaxNodeOrToken>(capacity: metaMethod.Parameters.Count * 2);

                foreach (var metaParam in metaMethod.Parameters)
                {
                    if (result.Count > 0)
                    {
                        result.Add(Token(SyntaxKind.CommaToken));
                    }
                        
                    result.Add(
                        Parameter(
                            SafeIdentifier(metaParam.Name)
                        ).WithType(
                            metaParam.Type.GetQualifiedNameSyntax()
                        )
                    );
                }

                return result;
            }
        }

        BlockSyntax CreateLogMethodBody(TelemetryMethodDescription metaMethod)
        {
            var reportInvocation = CreateReportInvocation();

            if (metaMethod.ReturnsTraceSpan)
            {
                return Block(ReturnStatement(CreateTraceSpanInstantiation()));
            }
            else if (metaMethod.ReturnsException)
            {
                return Block(
                    ExpressionStatement(reportInvocation),
                    CreateReturnException(metaMethod)
                );
            }
            else
            {
                return Block(ExpressionStatement(reportInvocation));
            }

            ObjectCreationExpressionSyntax CreateTraceSpanInstantiation()
            {
                return ObjectCreationExpression(
                    IdentifierName("TestSpan")
                )
                .WithArgumentList(ArgumentList(
                    SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[] {
                        Argument(ThisExpression()),
                        Token(SyntaxKind.CommaToken),
                        Argument(GetMessageInterpolatedString(metaMethod))
                    })
                ));
            }

            InvocationExpressionSyntax CreateReportInvocation()
            {
                return InvocationExpression(
                    IdentifierName($"Report{metaMethod.LogLevel}")
                )
                .WithArgumentList(ArgumentList(SingletonSeparatedList<ArgumentSyntax>(
                    Argument(GetMessageInterpolatedString(metaMethod))
                )));
            }
        }

        StatementSyntax CreateReturnException(TelemetryMethodDescription metaMethod)
        {
            return ReturnStatement(
                ObjectCreationExpression(IdentifierName(metaMethod.ReturnType.Name))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList<ArgumentSyntax>(Argument(
                        GetMessageInterpolatedString(metaMethod)                      
                    ))))    
            );

        }

        InterpolatedStringExpressionSyntax GetMessageInterpolatedString(TelemetryMethodDescription metaMethod)
        {
            var tokens = new List<InterpolatedStringContentSyntax>();
                    
            tokens.Add(GetInterpolatedText(metaMethod.EventName));

            foreach (var metaParam in metaMethod.Parameters)
            {
                tokens.Add(GetInterpolatedText($" {metaParam.KeyName}="));
                tokens.Add(Interpolation(SafeIdentifierName(metaParam.KeyName)));
            }
                    
            return InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken)).WithContents(
                List<InterpolatedStringContentSyntax>(tokens)
            );

            InterpolatedStringContentSyntax GetInterpolatedText(string text)
            {
                return InterpolatedStringText().WithTextToken(Token(
                    TriviaList(),
                    SyntaxKind.InterpolatedStringTextToken,
                    text,
                    text,
                    TriviaList()
                ));
            }
        }
    }
    
    public static string GetTelemetryClassName(TelemetryDescription metaTelemetry)
    {
        return "TestDoubleImpl__" + metaTelemetry.InterfaceSymbol
            .GetFullyQualifiedMetadataName()
            .Replace(".", "_")
            .Replace("+", "_");
    }
}
