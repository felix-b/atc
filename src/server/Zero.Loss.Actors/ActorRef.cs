using System;
using Zero.Loss.Actors.Impl;

namespace Zero.Loss.Actors
{
    public readonly struct ActorRef<T> : IAnyActorRef, IEquatable<ActorRef<T>> 
        where T : class, IStatefulActor
    {
        private readonly IInternalSupervisorActor _supervisor;
        private readonly string _uniqueId;

        public ActorRef(IInternalSupervisorActor supervisor, string uniqueId)
        {
            _supervisor = supervisor;
            _uniqueId = uniqueId;
        }

        public bool Equals(ActorRef<T> other)
        {
            return _uniqueId == other._uniqueId;
        }

        public override bool Equals(object? obj)
        {
            return obj is ActorRef<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _uniqueId.GetHashCode();
        }

        public override string ToString()
        {
            return UniqueId;
        }

        public T Get() => _supervisor.GetActorObjectByIdOrThrow<T>(_uniqueId);
        public string UniqueId => _uniqueId;
        public bool CanGet => _supervisor != null;

        public static bool operator ==(ActorRef<T> left, ActorRef<T> right)
        {
            return left._uniqueId == right._uniqueId;
        }

        public static bool operator !=(ActorRef<T> left, ActorRef<T> right)
        {
            return !(left == right);
        }

        public static implicit operator ActorRef<IStatefulActor>(ActorRef<T> source)
        {
            return new ActorRef<IStatefulActor>(source._supervisor, source._uniqueId);
        }
    }

    public interface IAnyActorRef
    {
        string UniqueId { get; }
    }
}
