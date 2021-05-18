using System.Collections.Generic;
using System.IO;

namespace Atc.Data.Sources
{
    public static class DictionaryExtensions
    {
        public static string MakeMinimalUniqueStringKey<T>(
            this Dictionary<string, T> existingKeys, 
            string newKey, 
            int minSuffix = 2, 
            int maxSuffix = 200)
        {
            if (!existingKeys.ContainsKey(newKey))
            {
                return newKey;
            }

            int leftBound = minSuffix;
            int rightBound = maxSuffix;

            do
            {
                int suffix = (leftBound >> 1) + (rightBound >> 1); // leftBound + (rightBound-leftBound)/2
                var proposedKey = newKey + suffix;
                if (!existingKeys.ContainsKey(proposedKey))
                {
                    if (suffix == minSuffix || existingKeys.ContainsKey(newKey + (suffix - 1)))
                    {
                        return proposedKey;
                    }

                    rightBound = suffix;
                }
                else
                {
                    leftBound = suffix + 1;
                }
            } while (leftBound <= rightBound);

            throw new InvalidDataException($"Cannot make unique key for '{newKey}', all {maxSuffix} suffixes are in use");
        }
    }
}