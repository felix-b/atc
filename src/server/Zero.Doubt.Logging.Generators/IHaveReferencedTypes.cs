using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Zero.Doubt.Logging.Generators
{
    public interface IHaveReferencedTypes
    {
        void IncludeReferencedTypes(List<ITypeSymbol> destination);
    }
}
