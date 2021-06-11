using System;

namespace Zero.Serialization.Buffers
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class BufferInfoProviderAttribute : Attribute
    {
        public BufferInfoProviderAttribute(Type providerType)
        {
            ProviderType = providerType;
        }

        public Type ProviderType { get; set; }       
    }
}