using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Atc.Telemetry.Generators.Descriptions;

public class TelemetryMethodDescription
{
    public TelemetryMethodDescription(
        string name,
        string eventName,
        CopyOfLogLevel logLevel,
        IMethodSymbol symbol, 
        IEnumerable<ParameterDescription> parameters,
        ITypeSymbol returnType,
        bool returnsException, 
        bool returnsTraceSpan)
    {
        Name = name;
        EventName = eventName;
        LogLevel = logLevel;
        Symbol = symbol;
        Parameters = parameters.ToList();
        ReturnType = returnType;
        ReturnsException = returnsException;
        ReturnsTraceSpan = returnsTraceSpan;
    }

    public string Name { get; private set; }
    public string EventName { get; private set; }
    public CopyOfLogLevel LogLevel { get; private set; }
    public IMethodSymbol Symbol { get; private set; }
    public List<ParameterDescription> Parameters { get; private set; }
    public ITypeSymbol ReturnType { get; private set; }
    public bool ReturnsException { get; private set; }
    public bool ReturnsTraceSpan { get; private set; }
}