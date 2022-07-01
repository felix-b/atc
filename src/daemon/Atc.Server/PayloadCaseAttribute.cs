namespace Atc.Server;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class PayloadCaseAttribute : Attribute
{
    public PayloadCaseAttribute(object discriminator)
    {
        Discriminator = discriminator;
    }

    public object Discriminator { get; set; }
}