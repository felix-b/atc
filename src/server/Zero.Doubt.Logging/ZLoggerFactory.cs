using System;
using System.Reflection;

namespace Zero.Doubt.Logging
{
    public static class ZLoggerFactory
    {
        public static T CreateGeneratedLogger<T>(LogWriter writer) where T : class
        {
            var loggerType = GetGeneratedLoggerType<T>();
            return (T)Activator.CreateInstance(loggerType, new object[] { writer });
        }

        public static Type GetGeneratedLoggerType<T>() where T : class
        {
            var loggerTypeMetadataName = GetGeneratedLoggerTypeMetadataName<T>();
            var loggerType = Assembly.GetCallingAssembly().GetType(loggerTypeMetadataName, throwOnError: true);
            return loggerType;
        }

        public static string GetGeneratedLoggerTypeMetadataName<T>() where T : class
        {
            var normalizedName = typeof(T).FullName!.Replace(".", "_").Replace("+", "_");
            return $"GeneratedCode.Impl__{normalizedName}";
        }
    }
}
