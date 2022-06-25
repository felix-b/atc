using Atc.Grains.Tests.Samples;

namespace Atc.Grains.Tests.Doubles;

public static class TestDoubles
{
    public static ISilo CreateConfiguredSilo(
        string siloId,
        ISiloTelemetry? telemetry = null,
        ISiloEventStreamWriter? eventWriter = null,
        ISiloDependencyBuilder? dependencies = null,
        ISiloEnvironment? environment = null)
    {
        var silo = ISilo.Create(
            siloId,
            configuration: new SiloConfigurationBuilder(
                telemetry ?? new NoopSiloTelemetry(), 
                eventWriter ?? new TestEventStreamWriter(), 
                dependencies ?? new TestSiloDependencyContext(),
                environment ?? new TestSiloEnvironment()),
            configure: config => {
                SampleGrainOne.RegisterGrainType(config);
                SampleGrainTwo.RegisterGrainType(config);
            });
        return silo;
    }
}
