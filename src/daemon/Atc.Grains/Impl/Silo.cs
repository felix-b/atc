using System.Collections.Immutable;

namespace Atc.Grains.Impl;

public class Silo : ISilo
{
    private readonly ISiloDependencyContext _dependencyContext;
    private readonly SuperGrain _superGrain;
    
    public Silo(
        string siloId, 
        SiloConfigurationBuilder configuration)
    {
        SiloId = siloId;
        Telemetry = configuration.Telemetry;
        Environment = configuration.Environment;
        Dispatch = new EventDispatch(siloId, configuration.EventWriter, configuration.Telemetry, configuration.Environment);
        TaskQueue = new TaskQueue(configuration.Telemetry);

        _superGrain = new SuperGrain(
            Dispatch, 
            configuration.Telemetry, 
            getDependencyContext: () => _dependencyContext!, 
            configuration.GrainTypeRegistrations);

        var dependencies = configuration.DependencyBuilder;
        dependencies.AddSingleton<ISilo>(this);        
        dependencies.AddSingleton<ISiloGrains>(this.Grains);        
        dependencies.AddSingleton<ISiloEventDispatch>(this.Dispatch);
        dependencies.AddSingleton<ISiloTaskQueue>(this.TaskQueue);        
        dependencies.AddSingleton<ISiloTimeTravel>(this.TimeTravel);
        dependencies.AddSingleton<ISiloTelemetry>(this.Telemetry);

        _dependencyContext = dependencies.GetContext();
    }

    public string SiloId { get; }
    public ISiloGrains Grains => _superGrain;
    public ISiloEventDispatch Dispatch { get; }
    public ISiloTaskQueue TaskQueue { get; }
    public ISiloTimeTravel TimeTravel => _superGrain;
    public ISiloTelemetry Telemetry { get; }
    public ISiloEnvironment Environment { get; }
}
