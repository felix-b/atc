using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Zero.Doubt.Logging.Generators
{
    [Generator]
    public class LoggerGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new LoggerDiscoverySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is LoggerDiscoverySyntaxReceiver discovery)
            {
                var generatorMessages = new List<string>();

                foreach (var logger in discovery.Loggers)
                {
                    generatorMessages.Add($"############ LOGGER {logger.Name} ############");

                    foreach (var method in logger.Methods)
                    {
                        generatorMessages.Add($"--- method [{method.Name}] ret-span[{method.ReturnsLogSpan}] ret-exception[{method.ReturnsException}]");

                        foreach (var parameter in method.Parameters)
                        {
                            generatorMessages.Add($"    + param [{parameter.Name}] type[{parameter.Type}] is-exception[{parameter.IsException}]");
                        }
                    }
                    
                    AddImplementationToSource(context, logger, generatorMessages);
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

        private void AddImplementationToSource(GeneratorExecutionContext context, LoggerDescription metaLogger, List<string> generatorMessages)
        {
            var classSyntax = LoggerClassSyntaxGenerator.GenerateSyntax(context, metaLogger, out var className);
            var unitSyntax = CompilationUnitGenerator.GenerateCompilationUnit(
                "GeneratedCode",
                new MemberDeclarationSyntax[] {
                    classSyntax
                },
                new IHaveReferencedTypes[] {
                    metaLogger, 
                    new AlwaysImportTypesProvider(context)
                },
                generatorMessages);

            var normalizedSyntax = unitSyntax.NormalizeWhitespace(indentation: "    ");
            context.AddSource(className, SourceText.From(normalizedSyntax.ToFullString(), Encoding.UTF8));
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
                var symbol = _context.Compilation.GetTypeByMetadataName("Zero.Doubt.Logging.LogWriter");
                if (symbol != null)
                {
                    destination.Add(symbol);
                }
            }
        }
    }
}
