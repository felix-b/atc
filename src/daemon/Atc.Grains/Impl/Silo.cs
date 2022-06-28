using System.Collections.Immutable;

namespace Atc.Grains.Impl;

public class Silo : ISilo
{
    private readonly ISiloDependencyContext _dependencyContext;
    private readonly SuperGrain _superGrain;
    private readonly TaskQueueGrain _taskQueueGrain;
    
    public Silo(
        string siloId, 
        SiloConfigurationBuilder configuration)
    {
        SiloId = siloId;
        Telemetry = configuration.Telemetry;
        Environment = configuration.Environment;
        Dispatch = new EventDispatch(
            siloId, 
            configuration.EventWriter, 
            configuration.Telemetry, 
            configuration.Environment,
            getTaskQueue: () => _taskQueueGrain!);

        TaskQueueGrain.RegisterGrainType(configuration);
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
        dependencies.AddSingleton<ISiloEnvironment>(this.Environment);

        _dependencyContext = dependencies.GetContext();

        _taskQueueGrain = _superGrain
            .CreateGrain(grainId => new TaskQueueGrain.GrainActivationEvent(grainId))
            .Get(); 
    }

    public Task ExecuteReadyWorkItems()
    {
        return _taskQueueGrain.ExecuteReadyWorkItems();
    }

    public string SiloId { get; }
    public ISiloGrains Grains => _superGrain;
    public ISiloEventDispatch Dispatch { get; }
    public ISiloTaskQueue TaskQueue => _taskQueueGrain;
    public ISiloTimeTravel TimeTravel => _superGrain;
    public ISiloTelemetry Telemetry { get; }
    public ISiloEnvironment Environment { get; }
    public DateTime NextWorkItemAtUtc => _taskQueueGrain.NextWorkItemAtUtc;
}
