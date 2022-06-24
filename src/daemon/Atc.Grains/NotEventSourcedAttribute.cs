namespace Atc.Grains;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Class)]
public class NotEventSourcedAttribute : Attribute
{
}
