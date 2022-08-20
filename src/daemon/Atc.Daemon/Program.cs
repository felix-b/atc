// See https://aka.ms/new-console-template for more information

using Atc.Daemon;
using Atc.Grains;
using Atc.Server;using Atc.Sound.OpenAL;
using Atc.Speech.AzurePlugin;
using Atc.Telemetry;
using Atc.Telemetry.CodePath;
using Atc.Telemetry.Exporters.CodePath;
using Atc.World;
using Atc.World.Communications;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Data;
using Atc.World.Contracts.Sound;
using Atc.World.LLLL;
using Atc.World.Traffic;
using GeneratedCode;

Console.WriteLine("atcd - Air Traffic & Control simulation daemon");
await RunDaemon();
Console.WriteLine("atcd - down");

static async Task RunDaemon()
{
    Console.WriteLine("atcd - starting up");

    //TODO: convert to IAsyncDisposable
    using var telemetryExporter = InitializeTelemetry(
        LogLevel.Debug,
        listenPortNumber: 3003,
        out var telemetryEnvironment,
        out var telemetryUrl);

    await Task.Delay(3000);
    
    var telemetry = AtcDaemonTelemetry.CreateCodePathTelemetry<IAtcdTelemetry>(telemetryEnvironment); 
    
    using var audioContext = InitializeOpenAL(telemetryEnvironment);

    var firs = InitializeFirs(telemetry, telemetryEnvironment);
    
    await using var serviceEndpoint = InitializeServiceEndpoint(
        telemetryEnvironment,
        listenPortNumber: 3001,
        out var serviceListenUrl);

    Console.WriteLine("atcd - up");
    Console.WriteLine($"- service endpoint:   {serviceListenUrl}");
    Console.WriteLine($"- telemetry endpoint: {telemetryUrl}");
    Console.WriteLine($"- exit:               CTRL+C");
    Console.WriteLine();

    await serviceEndpoint.RunAsync();

    Console.WriteLine("atcd - shutting down");
}

static FirEntry[] InitializeFirs(
    IAtcdTelemetry telemetry,
    CodePathEnvironment telemetryEnvironment)
{
    var configuration = CreateConfiguration();
    var allFirIds = new[] {
        "LLLL"
    };
    var allFirs = allFirIds.Select(InitializeSingleFir);
    return allFirs.ToArray();

    SiloConfigurationBuilder CreateConfiguration()
    {
        var environment = new AtcdSiloEnvironment(telemetry, assetRootPath: null); 
        var configuration = new SiloConfigurationBuilder(
            dependencyBuilder: new AtcdSiloDependencyBuilder(),
            environment: environment,
            telemetry: AtcGrainsTelemetry.CreateCodePathTelemetry<ISiloTelemetry>(telemetryEnvironment),
            eventWriter: new AtcdOfflineSiloEventStream()
        );
        RegisterAllDependencies(configuration.DependencyBuilder, environment);
        RegisterAllGrainTypes(configuration);     
        return configuration;
    }
    
    FirEntry InitializeSingleFir(string firId)
    {
        using var traceSpan = telemetry.InitializeFir(firId);

        try
        {
            return InitializeSingleFirMonitored(firId);
        }
        catch (Exception e)
        {
            traceSpan.Fail(e);
            throw;
        }
    }

    FirEntry InitializeSingleFirMonitored(string firId)
    {
        var eventStream = new AtcdOfflineSiloEventStream();
        var silo = ISilo.Create(
            siloId: firId,
            configuration: configuration,
            configure: config => { }
        );

        var world = silo.Grains.CreateGrain<WorldGrain>(
            grainId => new WorldGrain.GrainActivationEvent(grainId)
        ).As<IWorldGrain>();
        
        if (firId == "LLLL")
        {
            LlllFirFactory.InitializeFir(silo, world, out var allParkedAircraft);
            LlllFirFactory.BeginPatternFlightsAtLlhz(silo, world, allParkedAircraft, TimeSpan.FromMinutes(15));
        }
        
        return new FirEntry(silo, eventStream);
    }
    
    void RegisterAllDependencies(ISiloDependencyBuilder dependencies, ISiloEnvironment siloEnvironment)
    {
        var telemetryProvider = new TelemetryProvider(new[] {
            AtcDaemonTelemetry.GetCodePathImplementations(telemetryEnvironment),
            AtcWorldTelemetry.GetCodePathImplementations(telemetryEnvironment),
            AtcSpeechAzurePluginTelemetry.GetCodePathImplementations(telemetryEnvironment),
            AtcSoundOpenALTelemetry.GetCodePathImplementations(telemetryEnvironment),
        });

        var audioStreamCache = new AtcdOfflineAudioStreamCache();
        var speechSynthesisPlugin = new AzureSpeechSynthesisPlugin(
            audioStreamCache,
            telemetryProvider.GetTelemetry<AzureSpeechSynthesisPlugin.IMyTelemetry>());
        var radioSpeechPlayer = new OpenalRadioSpeechPlayer(siloEnvironment);
        var aviationDatabase = new LlllDatabase();
        
        dependencies.AddSingleton<ITelemetryProvider>(telemetryProvider);
        dependencies.AddSingleton<IVerbalizationService>(new VerbalizationService());
        dependencies.AddSingleton<IAudioStreamCache>(audioStreamCache);
        dependencies.AddSingleton<ISpeechSynthesisPlugin>(speechSynthesisPlugin);
        dependencies.AddSingleton<IRadioSpeechPlayer>(radioSpeechPlayer);
        dependencies.AddSingleton<IAviationDatabase>(aviationDatabase);
        //dependencies.AddTransient<RadioStationSoundMonitor>(() => new RadioStationSoundMonitor(siloEnvironment));
    }
}

static AudioContextScope InitializeOpenAL(CodePathEnvironment telemetryEnvironment)
{
    var telemetry = AtcSoundOpenALTelemetry.CreateCodePathTelemetry<AudioContextScope.IMyTelemetry>(telemetryEnvironment); 
    return new AudioContextScope(telemetry);
}

static WebSocketEndpoint InitializeServiceEndpoint(
    CodePathEnvironment telemetryEnvironment, 
    int listenPortNumber, 
    out string serviceUrl)
{
    var service = new AtcdService();
    var telemetry = AtcServerTelemetry.CreateCodePathTelemetry<IEndpointTelemetry>(telemetryEnvironment);
    var endpoint = WebSocketEndpoint
        .Define()
        .ReceiveMessagesOfType<AtcTelemetryCodepathProto.CodePathClientToServer>()
        .WithDiscriminator(m => m.PayloadCase)
        .SendMessagesOfType<AtcTelemetryCodepathProto.CodePathServerToClient>()
        .ListenOn(listenPortNumber, urlPath: "/atc")
        .BindToServiceInstance(service)
        .Create(telemetry, out var taskSynchronizer);

    serviceUrl = $"ws://localhost:{listenPortNumber}/atc";
    return endpoint;
}

static CodePathWebSocketExporter InitializeTelemetry(
    LogLevel logLevel, 
    int listenPortNumber, 
    out CodePathEnvironment environment,
    out string telemetryUrl)
{
    var exporterTelemetry = AtcServerTelemetry.CreateNoopTelemetry<IEndpointTelemetry>();
    var telemetryExporter = new CodePathWebSocketExporter(listenPortNumber, exporterTelemetry);
    environment = new CodePathEnvironment(LogLevel.Debug, telemetryExporter);
    telemetryUrl = $"ws://localhost:{listenPortNumber}/telemetry";
    return telemetryExporter;
}

static void RegisterAllGrainTypes(SiloConfigurationBuilder configuration)
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

