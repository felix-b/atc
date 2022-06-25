using System.Collections.Immutable;

namespace Atc.Grains.Impl;

public record SiloSnapshotOpaqueData(
    ulong NextDispatchSequenceNo,
    ImmutableList<GrainSnapshotOpaqueData> Grains,
    ImmutableDictionary<string, ulong> LastInstanceIdPerTypeString
);

public record GrainSnapshotOpaqueData(
    string GrainType,
    string GrainId,
    IGrainActivationEvent ActivationEvent,
    ulong ActivationEventSequenceNo,
    object State
);
