using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Zero.Doubt.Logging.Generators
{
    public static class LoggerClassSyntaxGenerator
    {
        public static ClassDeclarationSyntax GenerateSyntax(
            GeneratorExecutionContext context,
            LoggerDescription metaLogger, 
            out string generatedClassName)
        {
            var cachedStringIdByValue = new Dictionary<string, string>();
            generatedClassName = GetLoggerClassName();
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
                                    metaLogger.InterfaceSymbol.GetQualifiedNameSyntax()
                                )
                            )
                        )
                    );

                var members = CreateMembers(className);
                return classDecl.WithMembers(List(members));
            }

            string GetLoggerClassName()
            {
                return "Impl__" + metaLogger.InterfaceSymbol
                    .GetFullyQualifiedMetadataName()
                    .Replace(".", "_")
                    .Replace("+", "_");
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
                    CreateLogWriterField(),
                    CreateConstructor()
                };

                FieldDeclarationSyntax CreateLogWriterField()
                {
                    return (
                        FieldDeclaration(VariableDeclaration(IdentifierName("LogWriter"))
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
                        ConstructorDeclaration(Identifier(className))
                            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                            .WithParameterList(ParameterList(SingletonSeparatedList(
                                Parameter(Identifier("writer"))
                                    .WithType(IdentifierName("LogWriter"))))
                            )
                            .WithBody(Block(SingletonList<StatementSyntax>(
                                ExpressionStatement(AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName("_writer"),
                                    IdentifierName("writer")))
                            )))
                    );
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
                    foreach (var method in metaLogger.Methods)
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
                return metaLogger.Methods
                    .Select(CreateLogMethod)
                    .Cast<MemberDeclarationSyntax>()
                    .ToArray();
            }

            MethodDeclarationSyntax CreateLogMethod(LoggerMethodDescription metaMethod)
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
                                Identifier(metaParam.Name)
                            ).WithType(
                                metaParam.Type.GetQualifiedNameSyntax()
                            )
                        );
                    }

                    return result;
                }
            }

            BlockSyntax CreateLogMethodBody(LoggerMethodDescription metaMethod)
            {
                var invocation = CreateLogWriterInvocation();

                if (metaMethod.ReturnsLogSpan)
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

                InvocationExpressionSyntax CreateLogWriterInvocation()
                {
                    return InvocationExpression(MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("_writer"),
                        IdentifierName(metaMethod.ReturnsLogSpan ? "Span" : "Message")
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
                                IdentifierName("Debug")
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
                                        IdentifierName(metaParam.Name)
                                    )
                                }
                            )
                        )
                    );
                }
            }

            StatementSyntax CreateReturnException(LoggerMethodDescription metaMethod)
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
                        tokens.Add(Interpolation(IdentifierName(metaParam.KeyName)));
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
    }
}
