namespace Atc.Grains.Tests.Samples;

public static class TestSilo
{
    public static ISilo Create(
        string siloId,
        ISiloTelemetry? telemetry = null,
        ISiloEventStreamWriter? eventWriter = null,
        ISiloDependencyBuilder? dependencies = null)
    {
        var silo = ISilo.Create(
            siloId,
            configuration: new SiloConfigurationBuilder(
                telemetry ?? new NoopSiloTelemetry(), 
                eventWriter ?? new TestEventStreamWriter(), 
                dependencies ?? new TestSiloDependencyContext()),
            configure: config => {
                SampleGrainOne.RegisterGrainType(config);
                SampleGrainTwo.RegisterGrainType(config);
            });
        return silo;
    }
}
