namespace Atc.Grains;

public readonly struct GrainRef<T> : IAnyGrainRef, IEquatable<GrainRef<T>>, IEqualityComparer<GrainRef<T>>
    where T : class, IGrainId
{
    private readonly ISiloGrains _grains;
    private readonly string _grainId;

    public GrainRef(ISiloGrains grains, string grainId)
    {
        _grains = grains;
        _grainId = grainId;
    }

    public GrainRef<S> As<S>() where S : class, IGrainId
    {
        if (CanGet)
        {
            // validate type compatibility
            var unused = (S)(object)Get();
        }

        return new GrainRef<S>(_grains, _grainId);
    }
    
    public T Get()
    {
        if (_grains == null)
        {
            throw new InvalidOperationException("GrainRef was not initialized with a reference");
        }

        return _grains.GetInstanceById<T>(_grainId);
    }

    public override string ToString()
    {
        return GrainId;
    }

    public override int GetHashCode()
    {
        return _grainId.GetHashCode();
    }
    
    public bool Equals(GrainRef<T> other)
    {
        return other._grainId == this._grainId;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is GrainRef<T> other)
        {
            return Equals(other);
        }
        if (obj is IAnyGrainRef anyOther)
        {
            return Equals(anyOther.As<T>());
        }
        return false;
    }

    bool IEqualityComparer<GrainRef<T>>.Equals(GrainRef<T> x, GrainRef<T> y)
    {
        return x == y;
    }

    int IEqualityComparer<GrainRef<T>>.GetHashCode(GrainRef<T> value)
    {
        return value.GetHashCode();
    }

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
    GrainRef<S> As<S>() where S : class, IGrainId;
    string GrainId { get; }
}
