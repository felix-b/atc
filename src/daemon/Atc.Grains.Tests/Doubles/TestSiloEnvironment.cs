namespace Atc.Grains.Tests.Doubles;

public class TestSiloEnvironment : ISiloEnvironment
{
    private DateTime? _presetUtcNow = null;

    public DateTime UtcNow
    {
        get => _presetUtcNow ?? DateTime.UtcNow;
        set => _presetUtcNow = value;
    }
}
