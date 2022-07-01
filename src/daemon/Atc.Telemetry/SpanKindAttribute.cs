namespace Atc.Telemetry;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SpanKindAttribute : Attribute
{
    public SpanKind Kind { get; set; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class InternalSpanAttribute : SpanKindAttribute
{
    public InternalSpanAttribute()
    {
        Kind = SpanKind.Internal;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ProducerSpanAttribute : SpanKindAttribute
{
    public ProducerSpanAttribute()
    {
        Kind = SpanKind.Producer;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ConsumerSpanAttribute : SpanKindAttribute
{
    public ConsumerSpanAttribute()
    {
        Kind = SpanKind.Consumer;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ClientSpanAttribute : SpanKindAttribute
{
    public ClientSpanAttribute()
    {
        Kind = SpanKind.Client;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ServerSpanAttribute : SpanKindAttribute
{
    public ServerSpanAttribute()
    {
        Kind = SpanKind.Server;
    }
}

