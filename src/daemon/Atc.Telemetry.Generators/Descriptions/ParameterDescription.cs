using System;
using Microsoft.CodeAnalysis;

namespace Atc.Telemetry.Generators.Descriptions;

public class ParameterDescription
{
    public ParameterDescription(string name, string keyName, ITypeSymbol? type, bool isException)
    {
        Name = name;
        KeyName = keyName;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        IsException = isException;
    }

    public ITypeSymbol Type { get; private set; }
    public string Name { get; private set; }
    public string KeyName { get; private set; }
    public bool IsException { get; private set; }
}