using System.Runtime.CompilerServices;

namespace Atc.Utilities;

public static class EnumExtensions
{
    public static int AsInt<T>(this T enumValue) where T : Enum
    {
        return Unsafe.As<T, int>(ref enumValue);
    }
}
