using Atc.Grains;
using Atc.Sound.OpenAL;
using Atc.Speech.AzurePlugin;
using Atc.Telemetry;
using Atc.Telemetry.CodePath;
using Atc.World;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;
using Atc.World.Contracts.Sound;
using Atc.World.LLLL;
using Atc.World.Traffic;
using GeneratedCode;

namespace Atc.Daemon;

public class FirSiloBuilder
{
    private readonly IAtcdTelemetry _telemetry;
    private readonly CodePathEnvironment _telemetryEnvironment;
    private readonly SiloConfigurationBuilder _siloConfiguration;

    public FirSiloBuilder(IAtcdTelemetry telemetry, CodePathEnvironment telemetryEnvironment)
    {
        _telemetryEnvironment = telemetryEnvironment;
        _telemetry = telemetry;
        _siloConfiguration = CreateSiloConfiguration();
    }
    
    public FirEntry InitializeFir(string icao)
    {
        using var traceSpan = _telemetry.InitializeFir(icao);

        try
        {
            return InitializeSingleFirMonitored(icao);
        }
        catch (Exception e)
        {
            traceSpan.Fail(e);
            throw;
        }
    }

    private FirEntry InitializeSingleFirMonitored(string icaoCode)
    {
        var eventStream = new AtcdOfflineSiloEventStream();
        var silo = ISilo.Create(
            siloId: icaoCode,
            configuration: _siloConfiguration,
            configure: config => { }
        );

        var world = silo.Grains.CreateGrain<WorldGrain>(
            grainId => new WorldGrain.GrainActivationEvent(grainId)
        ).As<IWorldGrain>();
            
        if (icaoCode == "LLLL")
        {
            LlllFirFactory.InitializeFir(silo, world, out var allParkedAircraft);
            LlllFirFactory.BeginPatternFlightsAtLlhz(silo, world, allParkedAircraft, TimeSpan.FromMinutes(15));
        }
            
        return new FirEntry(
            Icao: icaoCode, 
            Silo: silo, 
            EventStream: eventStream,
            RunLoopThread: Thread.CurrentThread);
    }

    private SiloConfigurationBuilder CreateSiloConfiguration()
    {
        var environment = new AtcdSiloEnvironment(_telemetry, assetRootPath: null); 
        var configuration = new SiloConfigurationBuilder(
            dependencyBuilder: new AtcdSiloDependencyBuilder(),
            environment: environment,
            telemetry: AtcGrainsTelemetry.CreateCodePathTelemetry<ISiloTelemetry>(_telemetryEnvironment),
            eventWriter: new AtcdOfflineSiloEventStream()
        );
        RegisterAllDependencies(configuration.DependencyBuilder, environment);
        RegisterAllGrainTypes(configuration);     
        return configuration;
    }

    private void RegisterAllDependencies(ISiloDependencyBuilder dependencies, ISiloEnvironment siloEnvironment)
    {
        var telemetryProvider = new TelemetryProvider(new[] {
            AtcDaemonTelemetry.GetCodePathImplementations(_telemetryEnvironment),
            AtcWorldTelemetry.GetCodePathImplementations(_telemetryEnvironment),
            AtcSpeechAzurePluginTelemetry.GetCodePathImplementations(_telemetryEnvironment),
            AtcSoundOpenALTelemetry.GetCodePathImplementations(_telemetryEnvironment),
        });

        var audioStreamCache = new AtcdOfflineAudioStreamCache();
        var speechSynthesisPlugin = new AzureSpeechSynthesisPlugin(
            audioStreamCache,
            telemetryProvider.GetTelemetry<AzureSpeechSynthesisPlugin.IMyTelemetry>());
        var verbalizationService = new VerbalizationService();
        var speechService = new SpeechService(
            verbalizationService, 
            speechSynthesisPlugin,
            telemetryProvider.GetTelemetry<SpeechService.IMyTelemetry>());
        var radioSpeechPlayer = new OpenalRadioSpeechPlayer(siloEnvironment);
        var aviationDatabase = new LlllDatabase();
            
        dependencies.AddSingleton<ITelemetryProvider>(telemetryProvider);
        dependencies.AddSingleton<IVerbalizationService>(verbalizationService);
        dependencies.AddSingleton<ISpeechService>(speechService);
        dependencies.AddSingleton<IAudioStreamCache>(audioStreamCache);
        dependencies.AddSingleton<ISpeechSynthesisPlugin>(speechSynthesisPlugin);
        dependencies.AddSingleton<IRadioSpeechPlayer>(radioSpeechPlayer);
        dependencies.AddSingleton<IAviationDatabase>(aviationDatabase);
    }

    private static void RegisterAllGrainTypes(SiloConfigurationBuilder configuration)
    {
        WorldGrain.RegisterGrainType(configuration);
        RadioStationGrain.RegisterGrainType(configuration);
        GroundStationRadioMediumGrain.RegisterGrainType(configuration);
        AircraftGrain.RegisterGrainType(configuration);
        LlhzAirportGrain.RegisterGrainType(configuration);
        LlhzAIClearanceControllerGrain.RegisterGrainType(configuration);
        LlhzAITowerControllerGrain.RegisterGrainType(configuration);
        LlhzAIPilotFlyingGrain.RegisterGrainType(configuration);
        LlhzAIPilotMonitoringGrain.RegisterGrainType(configuration);
    }
}