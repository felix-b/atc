using System.Collections.Generic;
using System.Linq;
using Atc.Telemetry.Generators.Descriptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Atc.Telemetry.Generators.SyntaxGenerators.IdentifierSyntaxFactory;

namespace Atc.Telemetry.Generators.SyntaxGenerators;

public static class CodePathTelemetrySyntaxGenerator
{
    public const string GeneratorId = "CodePath";
    
    public static ClassDeclarationSyntax GenerateClassSyntax(
        GeneratorExecutionContext context,
        TelemetryDescription metaTelemetry, 
        out string generatedClassName)
    {
        var cachedStringIdByValue = new Dictionary<string, string>();

        generatedClassName = GetTelemetryClassName(metaTelemetry);
        return CreateClass(generatedClassName);

        ClassDeclarationSyntax CreateClass(string className)
        {
            var classDecl = ClassDeclaration(className)
                .WithModifiers(
                    TokenList(Token(SyntaxKind.PublicKeyword))
                )
                .WithBaseList(
                    BaseList(
                        SingletonSeparatedList<BaseTypeSyntax>(
                            SimpleBaseType(
                                metaTelemetry.InterfaceSymbol.GetQualifiedNameSyntax()
                            )
                        )
                    )
                );

            var members = CreateMembers(className);
            return classDecl.WithMembers(List(members));
        }

        MemberDeclarationSyntax[] CreateMembers(string className)
        {
            var cachedStringFields = CreateCachedStringFields(); 
                
            return cachedStringFields
                .Concat(CreateInitializationMembers(className))
                .Concat(CreateLogMethodMembers())
                .ToArray();
        }

        MemberDeclarationSyntax[] CreateInitializationMembers(string className)
        {
            return new MemberDeclarationSyntax[] {
                CreateWriterField(),
                CreateConstructor()
            };

            FieldDeclarationSyntax CreateWriterField()
            {
                return (
                    FieldDeclaration(VariableDeclaration(IdentifierName("CodePathWriter"))
                        .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier("_writer"))))
                    ).WithModifiers(TokenList(new [] {
                        Token(SyntaxKind.PrivateKeyword),
                        Token(SyntaxKind.ReadOnlyKeyword)
                    }))
                );
            }

            ConstructorDeclarationSyntax CreateConstructor()
            {
                return (
                    ConstructorDeclaration(
                        Identifier(className)
                    )
                    .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(ParameterList(SingletonSeparatedList(
                        Parameter(Identifier("environment"))
                            .WithType(IdentifierName("CodePathEnvironment"))))
                    )
                    .WithBody(Block(SingletonList<StatementSyntax>(
                        ExpressionStatement(AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName("_writer"),
                            GetWriterInstantiationSyntax()))
                    )))
                );

                ObjectCreationExpressionSyntax GetWriterInstantiationSyntax()
                {
                    return ObjectCreationExpression(
                        IdentifierName("CodePathWriter")
                    )
                    .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[] {
                        Argument(IdentifierName("environment")),
                        Token(SyntaxKind.CommaToken),
                        Argument(IdentifierName(cachedStringIdByValue![metaTelemetry.Name]))
                    })));
                }
            }
        }

        MemberDeclarationSyntax[] CreateCachedStringFields()
        {
            var allStrings = new Dictionary<string, string>();
            FindAllStrings();
                
            var result = allStrings
                .Select(pair => CreateField(name: pair.Key, value: pair.Value))
                .Cast<MemberDeclarationSyntax>()
                .ToArray();

            return result;

            void FindAllStrings()
            {
                AddString(metaTelemetry.Name);
                
                foreach (var method in metaTelemetry.Methods)
                {
                    AddString(method.EventName);
                        
                    foreach (var parameter in method.Parameters)
                    {
                        AddString(parameter.KeyName);
                    }
                }
            }
                
            void AddString(string value)
            {
                var withoutDots = value.Replace(".", "");
                var camelCase = char.ToLower(withoutDots[0]) + withoutDots.Substring(1);
                var identifier = $"_s_{camelCase}";

                if (!allStrings!.ContainsKey(identifier))
                {
                    allStrings.Add(identifier, value);
                    cachedStringIdByValue!.Add(value, identifier);
                }
                else if (allStrings[identifier] != value)
                {
                    //TODO context.ReportDiagnostic(...);                        
                }
            }

            FieldDeclarationSyntax CreateField(string name, string value)
            {
                return 
                    FieldDeclaration(VariableDeclaration(PredefinedType(Token(SyntaxKind.StringKeyword))).WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(Identifier(name))
                                    .WithInitializer(EqualsValueClause(LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal(value)
                                    )))
                            )
                        ))
                        .WithModifiers(TokenList(new[] {
                            Token(SyntaxKind.PrivateKeyword),
                            Token(SyntaxKind.StaticKeyword),
                            Token(SyntaxKind.ReadOnlyKeyword)
                        }));
            }
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
            var invocation = CreateWriterInvocation();

            if (metaMethod.ReturnsTraceSpan)
            {
                return Block(ReturnStatement(invocation));
            }
            else if (metaMethod.ReturnsException)
            {
                return Block(
                    ExpressionStatement(invocation),
                    CreateReturnException(metaMethod)
                );
            }
            else
            {
                return Block(ExpressionStatement(invocation));
            }

            InvocationExpressionSyntax CreateWriterInvocation()
            {
                return InvocationExpression(MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("_writer"),
                    IdentifierName(metaMethod.ReturnsTraceSpan ? "Span" : "Message")
                ))
                .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(
                    GetAllArguments()
                )));
            }

            IEnumerable<SyntaxNodeOrToken> GetAllArguments()
            {
                var arguments = new List<SyntaxNodeOrToken>() {
                    Argument(
                        IdentifierName(cachedStringIdByValue![metaMethod.EventName])
                    ),
                    Token(SyntaxKind.CommaToken),
                    Argument(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("LogLevel"),
                            IdentifierName(metaMethod.LogLevel.ToString())
                        )
                    )
                };

                foreach (var metaParam in metaMethod.Parameters)
                {
                    arguments.Add(Token(SyntaxKind.CommaToken));                
                    arguments.Add(GetKeyValueArgument(metaParam));                
                }
                    
                return arguments;
            }

            ArgumentSyntax GetKeyValueArgument(ParameterDescription metaParam)
            {
                return Argument(
                    TupleExpression(
                        SeparatedList<ArgumentSyntax>(
                            new SyntaxNodeOrToken[] {
                                Argument(
                                    IdentifierName(cachedStringIdByValue[metaParam.KeyName])
                                ),
                                Token(SyntaxKind.CommaToken),
                                Argument(
                                    SafeIdentifierName(metaParam.Name)
                                )
                            }
                        )
                    )
                );
            }
        }

        StatementSyntax CreateReturnException(TelemetryMethodDescription metaMethod)
        {
            return ReturnStatement(
                ObjectCreationExpression(IdentifierName(metaMethod.ReturnType.Name))
                    .WithArgumentList(ArgumentList(SingletonSeparatedList<ArgumentSyntax>(Argument(
                        GetMessageInterpolatedString()                      
                    ))))    
            );

            InterpolatedStringExpressionSyntax GetMessageInterpolatedString()
            {
                var tokens = new List<InterpolatedStringContentSyntax>();
                    
                tokens.Add(Interpolation(IdentifierName(cachedStringIdByValue![metaMethod.EventName])));

                foreach (var metaParam in metaMethod.Parameters)
                {
                    tokens.Add(GetInterpolatedText($" {metaParam.KeyName}="));
                    tokens.Add(Interpolation(SafeIdentifierName(metaParam.KeyName)));
                }
                    
                return InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken)).WithContents(
                    List<InterpolatedStringContentSyntax>(tokens)
                );
            }

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
        return "CodePathImpl__" + metaTelemetry.InterfaceSymbol
            .GetFullyQualifiedMetadataName()
            .Replace(".", "_")
            .Replace("+", "_");
    }
}
