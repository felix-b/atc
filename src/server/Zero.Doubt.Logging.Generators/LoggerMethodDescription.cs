using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zero.Doubt.Logging.Generators
{
    public class LoggerMethodDescription
    {
        public LoggerMethodDescription(
            string name,
            string eventName,
            LogLevel logLevel,
            IMethodSymbol symbol, 
            IEnumerable<ParameterDescription> parameters,
            ITypeSymbol returnType,
            bool returnsException, 
            bool returnsLogSpan)
        {
            Name = name;
            EventName = eventName;
            LogLevel = logLevel;
            Symbol = symbol;
            Parameters = parameters.ToList();
            ReturnType = returnType;
            ReturnsException = returnsException;
            ReturnsLogSpan = returnsLogSpan;
        }

        public string Name { get; private set; }
        public string EventName { get; private set; }
        public LogLevel LogLevel { get; private set; }
        public IMethodSymbol Symbol { get; private set; }
        public List<ParameterDescription> Parameters { get; private set; }
        public ITypeSymbol ReturnType { get; private set; }
        public bool ReturnsException { get; private set; }
        public bool ReturnsLogSpan { get; private set; }
    }
}