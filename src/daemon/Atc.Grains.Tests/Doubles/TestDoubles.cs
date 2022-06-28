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
    
    public static class GrainIds
    {
        public const string One1 = $"{nameof(SampleGrainOne)}/#1";
        public const string One2 = $"{nameof(SampleGrainOne)}/#2";
        public const string One3 = $"{nameof(SampleGrainOne)}/#3";
        public const string Two1 = $"{nameof(SampleGrainTwo)}/#1";
        public const string Two2 = $"{nameof(SampleGrainTwo)}/#2";
        public const string Two3 = $"{nameof(SampleGrainTwo)}/#3";
    }
}
