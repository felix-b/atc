using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atc.Telemetry.Generators.Descriptions;
using Atc.Telemetry.Generators.SyntaxGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Atc.Telemetry.Generators;

[Generator]
public class TelemetrySourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new TelemetryDiscoverySyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is TelemetryDiscoverySyntaxReceiver discovery)
        {
            var generatorMessages = new List<string>();
            generatorMessages.Add($"############ ASSEMBLY [{context.Compilation.AssemblyName}] ############");

            foreach (var telemetry in discovery.Telemetries)
            {
                generatorMessages.Add($"############ TELEMETRY {telemetry.Name} ############");

                try
                {
                    foreach (var method in telemetry.Methods)
                    {
                        generatorMessages.Add($"--- method [{method.Name}] ret-span[{method.ReturnsTraceSpan}] ret-exception[{method.ReturnsException}]");

                        foreach (var parameter in method.Parameters)
                        {
                            generatorMessages.Add($"    + param [{parameter.Name}] type[{parameter.Type}] is-exception[{parameter.IsException}]");
                        }
                    }

                    AddImplementationToSource(context, telemetry, generatorMessages);
                }
                catch (Exception e)
                {
                    generatorMessages.Add(e.ToString());
                }
            }

            try
            {
                AddImplementationFacadeToSource(context, discovery.Telemetries, generatorMessages);
            }
            catch (Exception e)
            {
                generatorMessages.Add(e.ToString());
            }

            var allMessages = generatorMessages.Concat(discovery.Messages);
            AddLogsToSource(context, allMessages);            
        }
        else
        {
            AddLogsToSource(context, new[] {
                "//Hello world", 
                $"// context.SyntaxContextReceiver type: {(context.SyntaxContextReceiver?.GetType()?.ToString() ?? "N/A")}"
            });            
        }
    }

    private void AddImplementationToSource(
        GeneratorExecutionContext context, 
        TelemetryDescription metaTelemetry, 
        List<string> generatorMessages)
    {
        var classSyntaxCodePath = CodePathTelemetrySyntaxGenerator.GenerateClassSyntax(context, metaTelemetry, out _);
        var classSyntaxNoop = NoopTelemetrySyntaxGenerator.GenerateClassSyntax(context, metaTelemetry, out _);
        var classSyntaxTestDouble = TestDoubleTelemetrySyntaxGenerator.GenerateClassSyntax(context, metaTelemetry, out _);

        metaTelemetry.ImplementationByGeneratorId[CodePathTelemetrySyntaxGenerator.GeneratorId] = classSyntaxCodePath;
        metaTelemetry.ImplementationByGeneratorId[NoopTelemetrySyntaxGenerator.GeneratorId] = classSyntaxNoop;
        metaTelemetry.ImplementationByGeneratorId[TestDoubleTelemetrySyntaxGenerator.GeneratorId] = classSyntaxTestDouble;

        var unitSyntax = CompilationUnitGenerator.GenerateCompilationUnit(
            "GeneratedCode",
            new MemberDeclarationSyntax[] {
                classSyntaxCodePath,
                classSyntaxNoop,
                classSyntaxTestDouble,
            },
            new IHaveReferencedTypes[] {
                metaTelemetry, 
                new AlwaysImportTypesProvider(context)
            },
            generatorMessages);

        var normalizedSyntax = unitSyntax.NormalizeWhitespace(indentation: "    ");
        context.AddSource($"TelemetryImpl_{metaTelemetry.Name}", SourceText.From(normalizedSyntax.ToFullString(), Encoding.UTF8));
    }
        
    private void AddImplementationFacadeToSource(
        GeneratorExecutionContext context, 
        IReadOnlyList<TelemetryDescription> telemetries, 
        List<string> generatorMessages)
    {
        var facadeClassName = context.Compilation.AssemblyName!.Replace(".", "") + "Telemetry";
        var classSyntaxImplementationFacade =
            ImplementationFacadeSyntaxGenerator.GenerateClass(facadeClassName, telemetries);
        var unitSyntax = CompilationUnitGenerator.GenerateCompilationUnit(
            "GeneratedCode",
            new MemberDeclarationSyntax[] {
                classSyntaxImplementationFacade
            },
            new IHaveReferencedTypes[] {
                new AlwaysImportTypesProvider(context)
            },
            generatorMessages);

        var normalizedSyntax = unitSyntax.NormalizeWhitespace(indentation: "    ");
        context.AddSource(
            facadeClassName, 
            SourceText.From(normalizedSyntax.ToFullString(), Encoding.UTF8));
    }
    
    
    private void AddLogsToSource(GeneratorExecutionContext context, IEnumerable<string> log)
    {
        var logSourceText =
            $@"/*{Environment.NewLine +
                  string.Join(Environment.NewLine, log) +
                  Environment.NewLine}*/";
            
        context.AddSource("Logs", SourceText.From(logSourceText, Encoding.UTF8));
    }

    private class AlwaysImportTypesProvider : IHaveReferencedTypes
    {
        private readonly GeneratorExecutionContext _context;

        public AlwaysImportTypesProvider(GeneratorExecutionContext context)
        {
            _context = context;
        }

        public void IncludeReferencedTypes(List<ITypeSymbol> destination)
        {
            AddSymbol(destination, "Atc.Telemetry.NoopTraceSpan");
            AddSymbol(destination, "Atc.Telemetry.CodePath.CodePathWriter");
            AddSymbol(destination, "Atc.Telemetry.TelemetryTestDoubleBase");
        }

        private void AddSymbol(List<ITypeSymbol> destination, string fullyQualifiedMetadataName)
        {
            var symbol = _context.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
            if (symbol != null)
            {
                destination.Add(symbol);
            }
        }
    }
}
