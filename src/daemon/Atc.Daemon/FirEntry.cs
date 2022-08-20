using Atc.Grains;

namespace Atc.Daemon;

public record FirEntry(
    ISilo Silo,
    AtcdOfflineSiloEventStream EventStream
);

