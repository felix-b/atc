using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zero.Doubt.Logging.Generators
{
    public class LoggerDescription : IHaveReferencedTypes
    {
        public LoggerDescription(
            string name, 
            ITypeSymbol interfaceSymbol, 
            IEnumerable<LoggerMethodDescription> methods)
        {
            Name = name;
            InterfaceSymbol = interfaceSymbol;
            Methods = methods.ToList();
        }

        public void IncludeReferencedTypes(List<ITypeSymbol> destination)
        {
            destination.Add(InterfaceSymbol);
            
            foreach (var method in Methods)
            {
                destination.Add(method.ReturnType);
                
                foreach (var parameter in method.Parameters)
                {
                    if (parameter.Type != null)
                    {
                        destination.Add(parameter.Type);
                    }
                }
            }
        }

        public string Name { get; private set; }
        public ITypeSymbol InterfaceSymbol { get; private set; }
        public List<LoggerMethodDescription> Methods { get; private set; }
    }
}