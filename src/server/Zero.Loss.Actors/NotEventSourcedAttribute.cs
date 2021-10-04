using System;

namespace Zero.Loss.Actors
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NotEventSourcedAttribute : Attribute
    {
    }
}