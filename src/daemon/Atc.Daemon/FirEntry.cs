using Atc.Grains;

namespace Atc.Daemon;

public record FirEntry(
    string Icao,
    ISilo Silo,
    AtcdOfflineSiloEventStream EventStream,
    Thread RunLoopThread
);
