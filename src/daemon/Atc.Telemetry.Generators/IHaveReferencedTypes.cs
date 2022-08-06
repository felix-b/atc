using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Atc.Telemetry.Generators;

public interface IHaveReferencedTypes
{
    void IncludeReferencedTypes(List<ITypeSymbol> destination);
}