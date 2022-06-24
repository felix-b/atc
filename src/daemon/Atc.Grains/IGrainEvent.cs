namespace Atc.Grains;

public interface IGrainEvent
{
}

public interface IGrainActivationEvent : IGrainEvent
{
    string GrainId { get; }
}

public interface IGrainActivationEvent<TGrain> : IGrainActivationEvent where TGrain : class, IGrain
{
    string GrainId { get; }
}
