using System;
using System.Collections.Generic;
using System.Linq;
using Atc.Telemetry.Generators.Descriptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Atc.Telemetry.Generators;

public class TelemetryDiscoverySyntaxReceiver : ISyntaxContextReceiver
{
    private readonly HashSet<string> _telemetryInterfaceTypeNames = new();
    private readonly List<TelemetryDescription> _telemetries = new();
    private readonly List<string> _messages = new() {
        "Initialized new instance of TelemetryDiscoverySyntaxReceiver"
    };
        
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        try
        {
            if (context.Node is AttributeSyntax attrSyntax && IsGenerateTelemetryAttribute(attrSyntax, out var interfaceSymbol))
            {
                AddTelemetry(interfaceSymbol!);
            }
            else if (context.Node is InterfaceDeclarationSyntax interfaceDecl && IsTelemetryInterface(interfaceDecl))
            {
                AddTelemetry(TryGetTypeDeclarationSymbol(interfaceDecl));
            }
        }
        catch (Exception e)
        {
            _messages.Add(e.ToString());
        }

        bool IsGenerateTelemetryAttribute(AttributeSyntax attrSyntax, out ITypeSymbol? interfaceSymbol)
        {
            var ctorSymbol = context.SemanticModel.GetSymbolInfo(attrSyntax).Symbol as IMethodSymbol;
            //_messages.Add($"IsGenerateTelemetryAttribute: ctorSymbol => {ctorSymbol?.GetType().FullName ?? "N/A"}");
                
            if (ctorSymbol?.ContainingType.GetSystemTypeMetadataName() == "Atc.Telemetry.GenerateTelemetryImplementationAttribute,Atc.Telemetry")
            {
                _messages.Add($"GenerateTelemetry => {ctorSymbol.ContainingType.GetSystemTypeMetadataName()}.{ctorSymbol.Name}");

                var argument = attrSyntax.ArgumentList?.Arguments.First();
                if (argument != null && argument.Expression is TypeOfExpressionSyntax typeofSyntax)
                {
                    interfaceSymbol = TryGetTypeSymbol(typeofSyntax.Type);
                    _messages.Add($" + telemetry interface -> [{interfaceSymbol?.GetFullyQualifiedMetadataName() ?? "N/A"}]");
                    return true;
                }
            }
                
            interfaceSymbol = null;
            return false;
        }
            
        static bool IsTelemetryInterface(InterfaceDeclarationSyntax syntax)
        {
            var interfaceName = syntax.Identifier.Text;
            return (
                interfaceName.StartsWith("I") && 
                interfaceName.EndsWith("Telemetry") &&
                (interfaceName.Length > 7 || syntax.Parent is TypeDeclarationSyntax)
            );
        }

        static string GetTelemetryName(ITypeSymbol interfaceSymbol)
        {
            if (TryGetTelemetryNameAssignedByAttribute(interfaceSymbol, out var assignedName))
            {
                return assignedName;
            }
            
            var interfaceName = interfaceSymbol.Name;
            var telemetryName = interfaceName.Substring(1, interfaceName.Length - 10);
            
            return interfaceSymbol.ContainingType != null
                ? $"{interfaceSymbol.ContainingType.Name}{(telemetryName.Length > 0 ? "." : string.Empty)}{telemetryName}"
                : telemetryName;
        }

        static bool TryGetTelemetryNameAssignedByAttribute(ITypeSymbol interfaceSymbol, out string assignedName)
        {
            var telemetryAttribute = interfaceSymbol.GetAttributes()
                .FirstOrDefault(attr =>
                    attr.AttributeClass?.GetFullyQualifiedMetadataName() == "Atc.Telemetry.TelemetryNameAttribute");

            assignedName = telemetryAttribute?.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
            return !string.IsNullOrEmpty(assignedName);
        }

        void AddTelemetry(ITypeSymbol? interfaceSymbol)
        {
            if (interfaceSymbol != null && _telemetryInterfaceTypeNames.Add(interfaceSymbol.GetFullyQualifiedMetadataName()))
            {
                var description = CreateTelemetryDescription(interfaceSymbol);
                
                if (description != null)
                {
                    _telemetries.Add(description);
                }
            }
        }

        TelemetryDescription? CreateTelemetryDescription(ITypeSymbol interfaceTypeSymbol)
        {
            _messages.Add($"CreateTelemetryDescription: {interfaceTypeSymbol.GetFullyQualifiedMetadataName()}");

            var telemetryName = GetTelemetryName(interfaceTypeSymbol);
            var allInterfaces = interfaceTypeSymbol
                .AllInterfaces.CastArray<ITypeSymbol>()
                .Add(interfaceTypeSymbol);
            var allMethods = allInterfaces
                .SelectMany(symbol => symbol.GetMembers().OfType<IMethodSymbol>())
                .ToArray();
            
            return new TelemetryDescription(
                name: telemetryName,
                interfaceSymbol: interfaceTypeSymbol!,
                methods: allMethods.Select(methodSymbol => CreateTelemetryMethodDescription(methodSymbol, telemetryName))
                // methods: interfaceTypeSymbol.GetMembers()
                //     .OfType<IMethodSymbol>()
                //     .Select(methodSymbol => CreateTelemetryMethodDescription(methodSymbol, telemetryName))
            );
        }
            
        TelemetryMethodDescription CreateTelemetryMethodDescription(IMethodSymbol symbol, string telemetryName)
        {
            var returnTypeSymbol = symbol.ReturnType;

            _messages.Add($"+ CreateTelemetryMethodDescription: {symbol.Name} ret-type[{returnTypeSymbol?.GetFullyQualifiedMetadataName() ?? "N/A"}]");
                
            if (returnTypeSymbol == null)
            {
                _messages.Add($"  > Warning: return type symbol not found");
            }

            var returnsException = IsExceptionType(returnTypeSymbol);
            var returnsTraceSpan = IsTraceSpanType(returnTypeSymbol);
            AnalyzeMethodName(symbol.Name, returnsException, returnsTraceSpan, out var eventName, out var logLevel);
            
            return new TelemetryMethodDescription(
                name: symbol.Name,
                eventName: $"{telemetryName}.{eventName}",
                logLevel: logLevel,
                symbol: symbol,
                parameters: symbol.Parameters.Select(CreateParameterDescription),
                returnType: returnTypeSymbol!,
                returnsException: returnsException, 
                returnsTraceSpan: returnsTraceSpan
            );
        }

        string AnalyzeMethodName(
            string methodName,
            bool returnsException,
            bool returnsTraceSpan,
            out string eventName, 
            out CopyOfLogLevel logLevel)
        {
            var returnsVoid = !returnsException && !returnsTraceSpan;
            
            eventName = methodName;
            logLevel = CopyOfLogLevel.Debug;

            CheckMethodPrefix(eventName, "Span", condition: returnsTraceSpan, CopyOfLogLevel.Verbose,  ref eventName, ref logLevel);
            CheckMethodPrefix(eventName, "Debug", condition: returnsVoid, CopyOfLogLevel.Debug,  ref eventName, ref logLevel);
            CheckMethodPrefix(eventName, "Verbose", condition: returnsVoid, CopyOfLogLevel.Verbose,  ref eventName, ref logLevel);
            CheckMethodPrefix(eventName, "Info", condition: returnsVoid, CopyOfLogLevel.Info,  ref eventName, ref logLevel);
            CheckMethodPrefix(eventName, "Warning", condition: returnsVoid, CopyOfLogLevel.Warning,  ref eventName, ref logLevel);
            CheckMethodPrefix(eventName, "Error", condition: returnsVoid, CopyOfLogLevel.Error,  ref eventName, ref logLevel);
            CheckMethodPrefix(eventName, "Exception", condition: returnsException, CopyOfLogLevel.Error,  ref eventName, ref logLevel);

            return eventName;
        }

        bool CheckMethodPrefix(
            string eventName,
            string prefix, 
            bool condition, 
            CopyOfLogLevel logLevel, 
            ref string resultEventName,
            ref CopyOfLogLevel resultLogLevel)
        {
            if (eventName.Length > prefix.Length && eventName.StartsWith(prefix) && condition)
            {
                resultEventName = eventName.Substring(prefix.Length);
                resultLogLevel = logLevel;
                return true;
            }
            return false;
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

        bool IsTraceSpanType(ITypeSymbol? type)
        {
            return type?.GetFullyQualifiedMetadataName() == "Atc.Telemetry.ITraceSpan";
        }
    }

    public IReadOnlyList<TelemetryDescription> Telemetries => _telemetries;
    public IReadOnlyList<string> Messages => _messages;
}