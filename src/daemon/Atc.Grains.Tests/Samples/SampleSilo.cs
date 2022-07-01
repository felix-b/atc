namespace Atc.Grains.Tests.Samples;

public static class SampleSilo
{
    public static void Configure(SiloConfigurationBuilder config)
    {
        SampleGrainOne.RegisterGrainType(config);
        SampleGrainTwo.RegisterGrainType(config);
        SampleGrainThree.RegisterGrainType(config);
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
