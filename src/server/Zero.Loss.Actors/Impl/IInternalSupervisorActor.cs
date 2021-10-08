using System.Collections.Generic;

namespace Zero.Loss.Actors.Impl
{
    public interface IInternalSupervisorActor
    {
        bool TryGetActorObjectById<TActor>(string uniqueId, out TActor? actor) where TActor : class, IStatefulActor;

        public TActor GetActorObjectByIdOrThrow<TActor>(string uniqueId) where TActor : class, IStatefulActor
        {
            if (TryGetActorObjectById<TActor>(uniqueId, out var actor))
            {
                return actor!;
            }

            throw new ActorNotFoundException($"Actor '{uniqueId}' could not be found");
        }
    }
}
