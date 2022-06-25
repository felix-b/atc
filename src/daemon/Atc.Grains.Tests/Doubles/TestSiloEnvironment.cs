namespace Atc.Grains.Tests.Doubles;

public class TestSiloEnvironment : ISiloEnvironment
{
    public DateTime UtcNow => DateTime.UtcNow;
}
