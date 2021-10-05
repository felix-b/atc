using System;
using System.Reflection;

namespace Zero.Doubt.Logging
{
    public static class ZLoggerFactory
    {
        public static T CreateLogger<T>(LogWriter writer) where T : class
        {
            var loggerType = GetGeneratedLoggerType<T>(lookupAssembly: Assembly.GetCallingAssembly());
            return (T)Activator.CreateInstance(loggerType, new object[] { writer });
        }

        public static Type GetGeneratedLoggerType<T>(Assembly? lookupAssembly = null) where T : class
        {
            var effectiveLookupAssembly = lookupAssembly ?? Assembly.GetCallingAssembly();
            var loggerTypeMetadataName = GetGeneratedLoggerTypeMetadataName<T>();
            var loggerType = effectiveLookupAssembly.GetType(loggerTypeMetadataName, throwOnError: true);
            return loggerType;
        }

        public static string GetGeneratedLoggerTypeMetadataName<T>() where T : class
        {
            var normalizedName = typeof(T).FullName!.Replace(".", "_").Replace("+", "_");
            return $"GeneratedCode.Impl__{normalizedName}";
        }
    }
}
