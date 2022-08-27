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

var allFirIcaoCodes = new[]{ "LLLL"};

Console.WriteLine("atcd - Air Traffic & Control simulation daemon");
await RunDaemon(allFirIcaoCodes);
Console.WriteLine("atcd - down");

static async Task RunDaemon(string[] allFirIcaoCodes)
{
    Console.WriteLine("atcd - starting up");
    //var exporterTelemetry = AtcServerTelemetry.CreateTestDoubleTelemetry<IEndpointTelemetry>();
    var exporterTelemetry = AtcServerTelemetry.CreateNoopTelemetry<IEndpointTelemetry>();
    //var exporterTelemetryForDebug = (TelemetryTestDoubleBase) exporterTelemetry;
    var telemetryExporter = BeginTelemetryExport(
        LogLevel.Debug,
        listenPortNumber: 3003,
        exporterTelemetry,
        out var telemetryEnvironment,
        out var telemetryUrl);

    // using var timer = new Timer(
    //     (state) => {
    //         Console.WriteLine(
    //             $"TELDBG> {DateTime.UtcNow:HH:mm:ss.fff} | " +
    //             $"BufferCount={telemetryExporter.TotalBufferCount} " +
    //             $"BroadcastCount={telemetryExporter.TotalBroadcastCount} " +
    //             $"SendBufferCount={telemetryExporter.TotalSendBufferCount} " +
    //             $"ObserveBuffersCount={telemetryExporter.TotalObserveBuffersCount} " +
    //             $"FireMessageCount={telemetryExporter.TotalFireMessageCount} " +
    //             $"TelemetryBytes={telemetryExporter.TotalTelemetryBytes}");
    //     }, 
    //     null, 
    //     TimeSpan.FromSeconds(1), 
    //     TimeSpan.FromSeconds(1));
    //
    
    using (telemetryExporter) //TODO: convert to IAsyncDisposable
    {
        await RunWhileExportingTelemetry();
    
        //await Task.Delay(5000);
        //Console.WriteLine($"using telemetryExporter - exiting @ {DateTime.UtcNow:HH:mm:ss.fff}");
    }

    //exporterTelemetryForDebug.PrintAllToConsole();
    
    async Task RunWhileExportingTelemetry()
    {
        var telemetry = AtcDaemonTelemetry.CreateCodePathTelemetry<IAtcdTelemetry>(telemetryEnvironment); 
    
        using var audioContext = InitializeOpenAL(telemetryEnvironment);
        using var runLoop = BeginRunLoop(allFirIcaoCodes, telemetry, telemetryEnvironment);
        await using var serviceEndpoint = InitializeServiceEndpoint(
            telemetryEnvironment,
            runLoop,
            listenPortNumber: 3001,
            out var serviceListenUrl);

        await serviceEndpoint.StartAsync();

        Console.WriteLine("atcd - up");
        Console.WriteLine($"- service endpoint:   {serviceListenUrl}");
        Console.WriteLine($"- telemetry endpoint: {telemetryUrl}");
        Console.WriteLine($"- exit:               CTRL+C");
        Console.WriteLine();

        //await Task.Delay(TimeSpan.FromSeconds(30));
        await PosixTerminationSignal.Receive();

        Console.WriteLine("atcd - shutting down");
        //Console.WriteLine($"RunWhileExportingTelemetry - exiting @ {DateTime.UtcNow:HH:mm:ss.fff}");
    }
}

static RunLoop BeginRunLoop(string[] firIcaoCodes, IAtcdTelemetry telemetry, CodePathEnvironment telemetryEnvironment)
{
    var siloBuilder = new FirSiloBuilder(telemetry, telemetryEnvironment);
    
    return new RunLoop(
        siloBuilder,
        allFirIds: firIcaoCodes,
        leaderFirIds: firIcaoCodes, //offline mode
        telemetry: AtcDaemonTelemetry.CreateCodePathTelemetry<RunLoop.IMyTelemetry>(telemetryEnvironment));
}

// static FirEntry[] InitializeFirs(
//     IAtcdTelemetry telemetry,
//     CodePathEnvironment telemetryEnvironment)
// {
//     var configuration = CreateConfiguration();
//     var allFirIcaoCodes = new[] {
//         "LLLL"
//     };
//     var allFirs = allFirIcaoCodes.Select(InitializeSingleFir);
//     return allFirs.ToArray();
//
//     SiloConfigurationBuilder CreateConfiguration()
//     {
//         var environment = new AtcdSiloEnvironment(telemetry, assetRootPath: null); 
//         var configuration = new SiloConfigurationBuilder(
//             dependencyBuilder: new AtcdSiloDependencyBuilder(),
//             environment: environment,
//             telemetry: AtcGrainsTelemetry.CreateCodePathTelemetry<ISiloTelemetry>(telemetryEnvironment),
//             eventWriter: new AtcdOfflineSiloEventStream()
//         );
//         RegisterAllDependencies(configuration.DependencyBuilder, environment);
//         RegisterAllGrainTypes(configuration);     
//         return configuration;
//     }
//     
//     FirEntry InitializeSingleFir(string icaoCode)
//     {
//         using var traceSpan = telemetry.InitializeFir(icaoCode);
//
//         try
//         {
//             return InitializeSingleFirMonitored(icaoCode);
//         }
//         catch (Exception e)
//         {
//             traceSpan.Fail(e);
//             throw;
//         }
//     }
//
//     FirEntry InitializeSingleFirMonitored(string icaoCode)
//     {
//         var eventStream = new AtcdOfflineSiloEventStream();
//         var silo = ISilo.Create(
//             siloId: icaoCode,
//             configuration: configuration,
//             configure: config => { }
//         );
//
//         var world = silo.Grains.CreateGrain<WorldGrain>(
//             grainId => new WorldGrain.GrainActivationEvent(grainId)
//         ).As<IWorldGrain>();
//         
//         if (icaoCode == "LLLL")
//         {
//             LlllFirFactory.InitializeFir(silo, world, out var allParkedAircraft);
//             LlllFirFactory.BeginPatternFlightsAtLlhz(silo, world, allParkedAircraft, TimeSpan.FromMinutes(15));
//         }
//         
//         return new FirEntry(
//             Icao: icaoCode, 
//             Silo: silo, 
//             EventStream: eventStream);
//     }
    
    // void RegisterAllDependencies(ISiloDependencyBuilder dependencies, ISiloEnvironment siloEnvironment)
    // {
    //     var telemetryProvider = new TelemetryProvider(new[] {
    //         AtcDaemonTelemetry.GetCodePathImplementations(telemetryEnvironment),
    //         AtcWorldTelemetry.GetCodePathImplementations(telemetryEnvironment),
    //         AtcSpeechAzurePluginTelemetry.GetCodePathImplementations(telemetryEnvironment),
    //         AtcSoundOpenALTelemetry.GetCodePathImplementations(telemetryEnvironment),
    //     });
    //
    //     var audioStreamCache = new AtcdOfflineAudioStreamCache();
    //     var speechSynthesisPlugin = new AzureSpeechSynthesisPlugin(
    //         audioStreamCache,
    //         telemetryProvider.GetTelemetry<AzureSpeechSynthesisPlugin.IMyTelemetry>());
    //     var verbalizationService = new VerbalizationService();
    //     var speechService = new SpeechService(
    //         verbalizationService, 
    //         speechSynthesisPlugin,
    //         telemetryProvider.GetTelemetry<SpeechService.IMyTelemetry>());
    //     var radioSpeechPlayer = new OpenalRadioSpeechPlayer(siloEnvironment);
    //     var aviationDatabase = new LlllDatabase();
    //     
    //     dependencies.AddSingleton<ITelemetryProvider>(telemetryProvider);
    //     dependencies.AddSingleton<IVerbalizationService>(verbalizationService);
    //     dependencies.AddSingleton<ISpeechService>(speechService);
    //     dependencies.AddSingleton<IAudioStreamCache>(audioStreamCache);
    //     dependencies.AddSingleton<ISpeechSynthesisPlugin>(speechSynthesisPlugin);
    //     dependencies.AddSingleton<IRadioSpeechPlayer>(radioSpeechPlayer);
    //     dependencies.AddSingleton<IAviationDatabase>(aviationDatabase);
    // }
// }

static AudioContextScope InitializeOpenAL(CodePathEnvironment telemetryEnvironment)
{
    var telemetry = AtcSoundOpenALTelemetry.CreateCodePathTelemetry<AudioContextScope.IMyTelemetry>(telemetryEnvironment); 
    return new AudioContextScope(telemetry);
}

static WebSocketEndpoint InitializeServiceEndpoint(
    CodePathEnvironment telemetryEnvironment,
    RunLoop runLoop,
    int listenPortNumber, 
    out string serviceUrl)
{
    var serviceTelemetry = AtcDaemonTelemetry.CreateCodePathTelemetry<AtcdService.IMyTelemetry>(telemetryEnvironment);
    var service = new AtcdService(serviceTelemetry, runLoop);

    var endpointTelemetry = AtcServerTelemetry.CreateCodePathTelemetry<IEndpointTelemetry>(telemetryEnvironment);
    var endpoint = WebSocketEndpoint
        .Define()
        .ReceiveMessagesOfType<AtcTelemetryCodepathProto.CodePathClientToServer>()
        .WithDiscriminator(m => m.PayloadCase)
        .SendMessagesOfType<AtcTelemetryCodepathProto.CodePathServerToClient>()
        .ListenOn(listenPortNumber, urlPath: "/atc")
        .BindToServiceInstance(service)
        .Create(endpointTelemetry, out var taskSynchronizer);

    serviceUrl = $"ws://localhost:{listenPortNumber}/atc";
    return endpoint;
}

static CodePathWebSocketExporter BeginTelemetryExport(
    LogLevel logLevel, 
    int listenPortNumber, 
    IEndpointTelemetry exporterTelemetry,
    out CodePathEnvironment environment,
    out string telemetryUrl)
{
    var telemetryExporter = new CodePathWebSocketExporter(
        listenPortNumber, 
        exporterTelemetry, 
        delayBeforeFirstPush: TimeSpan.FromSeconds(3));
    environment = new CodePathEnvironment(LogLevel.Debug, telemetryExporter);
    telemetryUrl = $"ws://localhost:{listenPortNumber}/telemetry";
    return telemetryExporter;
}

