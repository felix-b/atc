using Atc.Sound;
using Atc.World;
using Atc.World.AI;
using Atc.World.Comms;
using Zero.Doubt.Logging;
using Zero.Loss.Actors.Impl;

[assembly:GenerateLogger(typeof(StateStore.ILogger))]
[assembly:GenerateLogger(typeof(WorldActor.ILogger))]
[assembly:GenerateLogger(typeof(ICommsLogger))]
[assembly:GenerateLogger(typeof(ISoundSystemLogger))]
[assembly:GenerateLogger(typeof(AIRadioOperatingActor.ILogger))]
