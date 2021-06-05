using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zero.Doubt.Logging.Generators
{
    public class LoggerDiscoverySyntaxReceiver : ISyntaxContextReceiver
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
                if (context.Node is AttributeSyntax attrSyntax && IsGenerateLoggerAttribute(attrSyntax, out var interfaceSymbol))
                {
                    AddLogger(interfaceSymbol!);
                }
                else if (context.Node is InterfaceDeclarationSyntax interfaceDecl && IsLoggerInterface(interfaceDecl))
                {
                    AddLogger(TryGetTypeDeclarationSymbol(interfaceDecl));
                }
            }
            catch (Exception e)
            {
                _messages.Add(e.ToString());
            }

            bool IsGenerateLoggerAttribute(AttributeSyntax attrSyntax, out ITypeSymbol? interfaceSymbol)
            {
                var ctorSymbol = context.SemanticModel.GetSymbolInfo(attrSyntax).Symbol as IMethodSymbol;
                //_messages.Add($"IsGenerateLoggerAttribute: ctorSymbol => {ctorSymbol?.GetType().FullName ?? "N/A"}");
                
                if (ctorSymbol?.ContainingType.GetSystemTypeMetadataName() == "Zero.Doubt.Logging.GenerateLoggerAttribute,Zero.Doubt.Logging")
                {
                    _messages.Add($"GenerateLogger => {ctorSymbol.ContainingType.GetSystemTypeMetadataName()}.{ctorSymbol.Name}");

                    var argument = attrSyntax.ArgumentList?.Arguments.First();
                    if (argument != null && argument.Expression is TypeOfExpressionSyntax typeofSyntax)
                    {
                        interfaceSymbol = TryGetTypeSymbol(typeofSyntax.Type);
                        _messages.Add($" + logger interface -> [{interfaceSymbol?.GetFullyQualifiedMetadataName() ?? "N/A"}]");
                        return true;
                    }
                }
                
                interfaceSymbol = null;
                return false;
            }
            
            static bool IsLoggerInterface(InterfaceDeclarationSyntax syntax)
            {
                var interfaceName = syntax.Identifier.Text;
                return (
                    interfaceName.StartsWith("I") && 
                    interfaceName.EndsWith("Logger") &&
                    (interfaceName.Length > 7 || syntax.Parent is TypeDeclarationSyntax)
                );
            }

            static string GetLoggerName(ITypeSymbol interfaceSymbol)
            {
                var interfaceName = interfaceSymbol.Name;
                var loggerName = interfaceName.Substring(1, interfaceName.Length - 7);

                return interfaceSymbol.ContainingType != null
                    ? $"{interfaceSymbol.ContainingType.Name}{(loggerName.Length > 0 ? "." : string.Empty)}{loggerName}"
                    : loggerName;
            }

            void AddLogger(ITypeSymbol? interfaceSymbol)
            {
                if (interfaceSymbol != null && _loggerInterfaceTypeNames.Add(interfaceSymbol.GetFullyQualifiedMetadataName()))
                {
                    var description = CreateLoggerDescription(interfaceSymbol);
                
                    if (description != null)
                    {
                        _loggers.Add(description);
                    }
                }
            }

            LoggerDescription? CreateLoggerDescription(ITypeSymbol interfaceTypeSymbol)
            {
                _messages.Add($"CreateLoggerDescription: {interfaceTypeSymbol.GetFullyQualifiedMetadataName()}");

                var loggerName = GetLoggerName(interfaceTypeSymbol); 
                return new LoggerDescription(
                    name: loggerName,
                    interfaceSymbol: interfaceTypeSymbol!,
                    methods: interfaceTypeSymbol.GetMembers()
                        .OfType<IMethodSymbol>()
                        .Select(methodSymbol => CreateLoggerMethodDescription(methodSymbol, loggerName))
                );
            }
            
            LoggerMethodDescription CreateLoggerMethodDescription(IMethodSymbol symbol, string loggerName)
            {
                var returnTypeSymbol = symbol.ReturnType;

                _messages.Add($"+ CreateLoggerMethodDescription: {symbol.Name} ret-type[{returnTypeSymbol?.GetFullyQualifiedMetadataName() ?? "N/A"}]");
                
                if (returnTypeSymbol == null)
                {
                    _messages.Add($"  > Warning: return type symbol not found");
                }

                return new LoggerMethodDescription(
                    name: symbol.Name,
                    eventName: $"{loggerName}.{symbol.Name}",
                    logLevel: LogLevel.Debug,
                    symbol: symbol,
                    parameters: symbol.Parameters.Select(CreateParameterDescription),
                    returnType: returnTypeSymbol!,
                    returnsException: IsExceptionType(returnTypeSymbol), 
                    returnsLogSpan: IsLogSpanType(returnTypeSymbol)
                );
            }

            ParameterDescription CreateParameterDescription(IParameterSymbol symbol)
            {
                var typeSymbol = symbol.Type;
                _messages.Add($"   + CreateParameterDescription: {symbol.Name} type [{typeSymbol?.GetFullyQualifiedMetadataName() ?? "N/A"}]");

                if (typeSymbol == null)
                {
                    _messages.Add($"     > Warning: parameter type symbol not found");
                }
                
                return new ParameterDescription(
                    name: symbol.Name,
                    keyName: symbol.Name,
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
