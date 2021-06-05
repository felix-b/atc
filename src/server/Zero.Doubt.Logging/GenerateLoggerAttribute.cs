using System;

namespace Zero.Doubt.Logging
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class GenerateLoggerAttribute : Attribute
    {
        public GenerateLoggerAttribute(Type loggerInterfaceType)
        {
            LoggerInterfaceType = loggerInterfaceType;
        }

        public Type LoggerInterfaceType { get; set; }
    }
}