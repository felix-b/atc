using System;

namespace Zero.Loss.Actors
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Class)]
    public class NotEventSourcedAttribute : Attribute
    {
    }
}
