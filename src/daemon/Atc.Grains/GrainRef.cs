namespace Atc.Grains;

public readonly struct GrainRef<T> : IAnyGrainRef, IEquatable<GrainRef<T>> 
    where T : class, IGrainId
{
    private readonly ISiloGrains _grains;
    private readonly string _grainId;

    public GrainRef(ISiloGrains grains, string grainId)
    {
        _grains = grains;
        _grainId = grainId;
    }

    public bool Equals(GrainRef<T> other)
    {
        return _grainId == other._grainId;
    }

    public override bool Equals(object? obj)
    {
        return obj is GrainRef<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _grainId.GetHashCode();
    }

    public override string ToString()
    {
        return GrainId;
    }

    public T Get() => _grains.GetInstanceById<T>(_grainId);
    public string GrainId => _grainId;
    public bool CanGet => _grains != null;

    public static bool operator ==(GrainRef<T> left, GrainRef<T> right)
    {
        return left._grainId == right._grainId;
    }

    public static bool operator !=(GrainRef<T> left, GrainRef<T> right)
    {
        return !(left == right);
    }

    public static implicit operator GrainRef<IGrain>(GrainRef<T> source)
    {
        return new GrainRef<IGrain>(source._grains, source._grainId);
    }
}

public interface IAnyGrainRef
{
    string GrainId { get; }
}
