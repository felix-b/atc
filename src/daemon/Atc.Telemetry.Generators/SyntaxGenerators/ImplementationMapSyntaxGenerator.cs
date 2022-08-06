using System;
using System.Collections.Generic;
using System.Linq;
using Atc.Telemetry.Generators.Descriptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Atc.Telemetry.Generators.SyntaxGenerators;

public class ImplementationMapSyntaxGenerator
{
    private readonly string _generatorId;
    private readonly IEnumerable<TelemetryDescription> _telemetries;
    private readonly Dictionary<string, TypeSyntax> _dependencyTypeByName;

    public ImplementationMapSyntaxGenerator(string generatorId, IEnumerable<TelemetryDescription> telemetries)
    {
        _generatorId = generatorId;
        _telemetries = telemetries;
        _dependencyTypeByName = new();
    }
    
    public void AddDependency(string dependencyName, IdentifierNameSyntax typeIdentifier)
    {
        _dependencyTypeByName.Add(dependencyName, typeIdentifier);
    }
    
    public MethodDeclarationSyntax GenerateGetMapMethod()
    {
        var declararion = MethodDeclaration(
            IdentifierName("ITelemetryImplementationMap"),
            Identifier($"Get{_generatorId}Implementations")
        )
        .WithModifiers(TokenList(new[] {
            Token(SyntaxKind.PublicKeyword),
            Token(SyntaxKind.StaticKeyword)
        }))
        .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(
            GetParameterListTokens()
        )))
        .WithBody(Block(SingletonList<StatementSyntax>(
            ReturnStatement(
                GetMapInstantiationSyntax()
            )
        )));

        return declararion;
    }

    public MethodDeclarationSyntax GenerateCreateTelemetryMethod()
    {
        var declararion = MethodDeclaration(
            IdentifierName("T"),
            Identifier($"Create{_generatorId}Telemetry")
        )
        .WithModifiers(TokenList(new[] {
            Token(SyntaxKind.PublicKeyword),
            Token(SyntaxKind.StaticKeyword)
        }))
        .WithTypeParameterList(TypeParameterList(SingletonSeparatedList<TypeParameterSyntax>(
            TypeParameter(Identifier("T"))
        )))
        .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(
            GetParameterListTokens()
        )))
        .WithConstraintClauses(SingletonList<TypeParameterConstraintClauseSyntax>(
            TypeParameterConstraintClause(
                IdentifierName("T")
            )
            .WithConstraints(SingletonSeparatedList<TypeParameterConstraintSyntax>(
                TypeConstraint(IdentifierName("ITelemetry"))
            ))
        ))            
        .WithBody(Block(SingletonList<StatementSyntax>(
            ReturnStatement(
                CastExpression(
                    IdentifierName("T"),
                    ParenthesizedExpression(GetFactoryInvocationSyntax())
                )
            )
        )));

        return declararion;

        InvocationExpressionSyntax GetFactoryInvocationSyntax()
        {
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    GetEntryLookupSyntax(),
                    IdentifierName("Factory")
                )
            );
        }
        
        InvocationExpressionSyntax GetEntryLookupSyntax()
        {
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            GetMapInstantiationSyntax(),
                            IdentifierName("GetEntries")
                        )
                    ),
                    IdentifierName("First")
                )
            )
            .WithArgumentList(ArgumentList(SingletonSeparatedList<ArgumentSyntax>(Argument(
                SimpleLambdaExpression(Parameter(
                        Identifier("e")
                ))
                .WithExpressionBody(
                    BinaryExpression(
                        SyntaxKind.EqualsExpression,
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("e"), IdentifierName("InterfaceType")),
                        TypeOfExpression(IdentifierName("T"))
                    )
                )
            ))));
        }
    }

    public ClassDeclarationSyntax GenerateMapClass()
    {
        var className = $"{_generatorId}ImplementationMap";
        var classDeclaration = ClassDeclaration(className)
            .WithModifiers(
                TokenList(Token(SyntaxKind.PrivateKeyword))
            )
            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(IdentifierName("ITelemetryImplementationMap"))
            )))
            .WithMembers(List<MemberDeclarationSyntax>(
                GetMembersSyntaxes()
            ));

        return classDeclaration;

        IEnumerable<MemberDeclarationSyntax> GetMembersSyntaxes()
        {
            var members = new List<MemberDeclarationSyntax>();

            foreach (var nameTypePair in _dependencyTypeByName)
            {
                members.Add(GetDependencyFieldSyntax(nameTypePair));
            }

            if (_dependencyTypeByName.Count > 0)
            {
                members.Add(GetConstructorSyntax());
            }

            members.Add(CreateGetEntriesMethodSyntax());
            return members;
        }

        FieldDeclarationSyntax GetDependencyFieldSyntax(KeyValuePair<string, TypeSyntax> nameTypePair)
        {
            return FieldDeclaration(
                VariableDeclaration(
                    nameTypePair.Value
                )
                .WithVariables(SingletonSeparatedList<VariableDeclaratorSyntax>(
                    VariableDeclarator(Identifier($"_{nameTypePair.Key}"))
                ))
            )
            .WithModifiers(TokenList(new [] {
                Token(SyntaxKind.PrivateKeyword),
                Token(SyntaxKind.ReadOnlyKeyword)
            }));
        }

        ConstructorDeclarationSyntax GetConstructorSyntax()
        {
            return ConstructorDeclaration(
                Identifier(className)
            )
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(
                GetParameterListTokens()
            )))
            .WithBody(Block(
                _dependencyTypeByName.Select(GetDependencyAssignmentSyntax)
            ));

            StatementSyntax GetDependencyAssignmentSyntax(KeyValuePair<string, TypeSyntax> nameTypePair)
            {
                return ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName($"_{nameTypePair.Key}"),
                    IdentifierName(nameTypePair.Key)
                ));
            }
        }

        MethodDeclarationSyntax CreateGetEntriesMethodSyntax()
        {
            return MethodDeclaration(
                ArrayType(
                    IdentifierName("TelemetryImplementationEntry")
                )
                .WithRankSpecifiers(SingletonList<ArrayRankSpecifierSyntax>(ArrayRankSpecifier(
                    SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression())
                ))),
                Identifier("GetEntries")
            )
            .WithModifiers(
                TokenList(Token(SyntaxKind.PublicKeyword))
            )
            .WithBody(Block(SingletonList<StatementSyntax>(ReturnStatement(
                ImplicitArrayCreationExpression(InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SeparatedList<ExpressionSyntax>(
                        GetEntryArrayInitializerTokens()
                    )
                ))
            ))));

            IEnumerable<SyntaxNodeOrToken> GetEntryArrayInitializerTokens()
            {
                var result = new List<SyntaxNodeOrToken>();
                foreach (var metaTelemetry in _telemetries)
                {
                    if (result.Count > 0)
                    {
                        result.Add(Token(SyntaxKind.CommaToken));
                    }
                    result.Add(GetEntryCreationSyntax(metaTelemetry));
                }
                return result;
            }

            ObjectCreationExpressionSyntax GetEntryCreationSyntax(TelemetryDescription metaTelemetry)
            {
                return ObjectCreationExpression(
                    IdentifierName("TelemetryImplementationEntry")
                )
                .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[] {
                    Argument(TypeOfExpression(metaTelemetry.InterfaceSymbol.GetQualifiedNameSyntax())),
                    Token(SyntaxKind.CommaToken),
                    Argument(GetFactoryLambdaSyntax(metaTelemetry))
                })));
            }

            LambdaExpressionSyntax GetFactoryLambdaSyntax(TelemetryDescription metaTelemetry)
            {
                var implementationSyntax = metaTelemetry.ImplementationByGeneratorId[_generatorId];
                
                return ParenthesizedLambdaExpression().WithExpressionBody(
                    ObjectCreationExpression(
                        IdentifierName(implementationSyntax.Identifier)
                    )
                    .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(
                        GetDependencyArgumentListTokens(useInstanceFields: true)
                    )))
                );
            }
        }
    }

    private ObjectCreationExpressionSyntax GetMapInstantiationSyntax()
    {
        return ObjectCreationExpression(
            IdentifierName($"{_generatorId}ImplementationMap")
        )
        .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(
            GetDependencyArgumentListTokens()
        )));
    }

    private IEnumerable<SyntaxNodeOrToken> GetParameterListTokens()
    {
        var result = new List<SyntaxNodeOrToken>();

        foreach (var nameTypePair in _dependencyTypeByName)
        {
            if (result.Count > 0)
            {
                result.Add(Token(SyntaxKind.CommaToken));
            }
            result.Add(
                Parameter(Identifier(nameTypePair.Key)).WithType(nameTypePair.Value)
            );
        }
            
        return result;
    }

    private IEnumerable<SyntaxNodeOrToken> GetDependencyArgumentListTokens(bool useInstanceFields = false)
    {
        var result = new List<SyntaxNodeOrToken>();

        foreach (var nameTypePair in _dependencyTypeByName)
        {
            if (result.Count > 0)
            {
                result.Add(Token(SyntaxKind.CommaToken));
            }

            var identifierName = useInstanceFields
                ? "_" + nameTypePair.Key
                : nameTypePair.Key;
            
            result.Add(
                Argument(IdentifierName(identifierName))
            );
        }
            
        return result;
    }
}

