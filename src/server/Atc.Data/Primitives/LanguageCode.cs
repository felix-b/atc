using System;
using System.Collections.Generic;
using System.Globalization;

namespace Atc.Data.Primitives
{
    public readonly struct LanguageCode : IEquatable<LanguageCode>
    {
        // Format same as CultureInfo: languagecode2-country/regioncode2
        public readonly string Code;
        
        public LanguageCode(string code)
        {
            Code = code;
        }

        public readonly CultureInfo GetCultureInfo()
        {
            return CultureInfo.GetCultureInfo(Code);            
        }

        public bool Equals(LanguageCode other)
        {
            return Code == other.Code;
        }

        public override bool Equals(object? obj)
        {
            return obj is LanguageCode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Code.GetHashCode();
        }

        public override string ToString()
        {
            return Code;
        }

        public static bool operator ==(LanguageCode left, LanguageCode right)
        {
            return left.Code == right.Code;
        }

        public static bool operator !=(LanguageCode left, LanguageCode right)
        {
            return !(left == right);
        }

        public static implicit operator LanguageCode(string cultureName)
        {
            return new LanguageCode(cultureName);
        }

        public static LanguageCode FromCulture(CultureInfo culture)
        {
            return new LanguageCode(culture.Name);
        }
    }
}
