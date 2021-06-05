#if false

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zero.Doubt.Logging.Generators
{
    public class LoggerInterfaceSyntaxReceiver : ISyntaxContextReceiver
    {
        private readonly HashSet<string> _loggerInterfaceTypeNames = new();
        private readonly List<LoggerDescription> _loggers = new();
        private readonly List<string> _messages = new() {
            "Initialized new instance of LoggerInterfaceSyntaxReceiver"
        };
        
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            try
            {
                if (context.Node is InterfaceDeclarationSyntax interfaceDecl && IsLoggerInterface(interfaceDecl))
                {
                    AddLogger(interfaceDecl);
                }
                else if (context.Node is TypeOfExpressionSyntax typeofExpr && IsTypeOfLoggerInterface(typeofExpr))//, out var interfaceDecl2))
                {
                    //AddLogger(interfaceDecl2);
                }
            }
            catch (Exception e)
            {
                _messages.Add(e.ToString());
            }

            bool IsLoggerInterface(InterfaceDeclarationSyntax syntax)
            {
                var interfaceName = syntax.Identifier.Text;
                return (
                    interfaceName.StartsWith("I") && 
                    interfaceName.EndsWith("Logger") &&
                    (interfaceName.Length > 7 || syntax.Parent is TypeDeclarationSyntax)
                );
            }

            bool IsTypeOfLoggerInterface(TypeOfExpressionSyntax typeofExpr)//, out InterfaceDeclarationSyntax interfaceDecl)
            {
                var symbol = TryGetTypeSymbol(typeofExpr.Type);
                _messages.Add($"Lookup>> expression[{typeofExpr.ToFullString()}] -> symbol[{symbol?.GetType().FullName ?? "N/A"}]");

                if (symbol != null)
                {
                }
                
                return false;
            }
            
            static string GetLoggerName(InterfaceDeclarationSyntax syntax, ITypeSymbol symbol)
            {
                var interfaceName = syntax.Identifier.Text;
                var loggerName = interfaceName.Substring(1, interfaceName.Length - 7);

                return symbol.ContainingType != null
                    ? $"{symbol.ContainingType.Name}.{loggerName}"
                    : loggerName;
            }

            void AddLogger(InterfaceDeclarationSyntax interfaceSyntax)
            {
                var symbol = TryGetTypeDeclarationSymbol(interfaceSyntax);
            
                if (symbol != null && _loggerInterfaceTypeNames.Add(symbol.GetFullyQualifiedMetadataName()))
                {
                    var description = CreateLoggerDescription(interfaceSyntax);
                
                    if (description != null)
                    {
                        _loggers.Add(description);
                    }
                }
            }

            LoggerDescription? CreateLoggerDescription(InterfaceDeclarationSyntax syntax)
            {
                _messages.Add($"CreateLoggerDescription: {syntax.Identifier.Text}");

                var interfaceTypeSymbol = TryGetTypeDeclarationSymbol(syntax);
                if (interfaceTypeSymbol == null)
                {
                    _messages.Add($"> Error: interface declaration symbol not found");
                    return null;
                }

                var loggerName = GetLoggerName(syntax, interfaceTypeSymbol); 
                return new LoggerDescription(
                    name: loggerName,
                    interfaceSymbol: interfaceTypeSymbol!,
                    methods: syntax.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Select(methodSyntax => CreateLoggerMethodDescription(methodSyntax, loggerName))
                );
            }
            
            LoggerMethodDescription CreateLoggerMethodDescription(MethodDeclarationSyntax syntax, string loggerName)
            {
                var methodSymbol = context.SemanticModel.GetDeclaredSymbol(syntax);
                var returnTypeSymbol = TryGetTypeSymbol(syntax.ReturnType);

                _messages.Add($"+ CreateLoggerMethodDescription: {syntax.Identifier.Text} ret-type[{returnTypeSymbol?.GetFullyQualifiedMetadataName() ?? "N/A"}]");
                
                if (methodSymbol == null || returnTypeSymbol == null)
                {
                    _messages.Add($"  > WARNING: method or return type symbol not found");
                }

                return new LoggerMethodDescription(
                    name: syntax.Identifier.Text,
                    eventName: $"{loggerName}.{syntax.Identifier.Text}",
                    logLevel: LogLevel.Debug,
                    symbol: methodSymbol!,
                    parameters: syntax.ParameterList.Parameters.Select(CreateParameterDescription),
                    returnType: returnTypeSymbol!,
                    returnsException: IsExceptionType(returnTypeSymbol), 
                    returnsLogSpan: IsLogSpanType(returnTypeSymbol)
                );
            }

            ParameterDescription CreateParameterDescription(ParameterSyntax syntax)
            {
                var typeSymbol = TryGetTypeSymbol(syntax.Type);
                _messages.Add($"   + CreateParameterDescription: {syntax.Identifier.Text} type [{typeSymbol?.GetFullyQualifiedMetadataName() ?? "N/A"}]");

                if (typeSymbol == null)
                {
                    _messages.Add($"     > Warning: parameter type symbol not found");
                }
                
                return new ParameterDescription(
                    name: syntax.Identifier.Text,
                    keyName: syntax.Identifier.Text,
                    type: typeSymbol,  
                    isException: IsExceptionType(typeSymbol)
                );
            }

            ITypeSymbol? TryGetTypeSymbol(TypeSyntax? syntax)
            {
                return syntax != null
                    ? ModelExtensions.GetSymbolInfo(context.SemanticModel, syntax).Symbol as ITypeSymbol
                    : null;
            }

            ITypeSymbol? TryGetTypeDeclarationSymbol(TypeDeclarationSyntax? syntax)
            {
                return syntax != null
                    ? ModelExtensions.GetDeclaredSymbol(context.SemanticModel, syntax) as ITypeSymbol
                    : null;
            }

            bool IsExceptionType(ITypeSymbol? type)
            {
                return type.InheritsFrom("System.Exception");
            }

            bool IsLogSpanType(ITypeSymbol? type)
            {
                return type?.GetFullyQualifiedMetadataName() == "Zero.Doubt.Logging.LogWriter+LogSpan";
            }
        }

        public IReadOnlyList<LoggerDescription> Loggers => _loggers;
        public IReadOnlyList<string> Messages => _messages;
    }
}

#endif