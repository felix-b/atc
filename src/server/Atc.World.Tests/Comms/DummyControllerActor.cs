﻿using System;
using Atc.Speech.Abstractions;
using Atc.World.Abstractions;
using Atc.World.Comms;
using Microsoft.AspNetCore.Mvc;
using Zero.Loss.Actors;

namespace Atc.World.Tests.Comms
{
    public class DummyControllerActor : StatefulActor<DummyControllerActor.DummyState>, IHaveParty
    {
        public static readonly string TypeString = "dummy";
        
        public record DummyState(
            int RepeatCount,
            ActorRef<RadioStationActor> Radio
        );
        
        public record DummyActivationEvent(
            string UniqueId, 
            ActorRef<RadioStationActor> Radio
        ) : IActivationStateEvent<DummyControllerActor>;
        
        public record IncrementRepeatCountEvent() : IStateEvent;

        private readonly IStateStore _store;
        private readonly IWorldContext _world;
        private readonly ControllerParty _party;

        public DummyControllerActor(IStateStore store, IWorldContext world, DummyActivationEvent activation) 
            : base(TypeString, activation.UniqueId, new DummyState(RepeatCount: 0, activation.Radio))
        {
            _store = store;
            _world = world;
            _party = new DummyControllerParty(
                activation.UniqueId, 
                State.Radio.Get().Callsign, 
                NatureType.AI, 
                VoiceDescription.Default, 
                GenderType.Male, 
                AgeType.Senior,
                "Bob");
            
            _world.DeferBy(TimeSpan.FromSeconds(1), () => {
                State.Radio.Get().PowerOn();
                TransmitGreeting();
            });
        }

        protected override DummyState Reduce(DummyState stateBefore, IStateEvent @event)
        {
            return @event is IncrementRepeatCountEvent
                ? stateBefore with {RepeatCount = stateBefore.RepeatCount + 1}
                : stateBefore;
        }
        
        private void TransmitGreeting()
        {
            var wave = new RadioTransmissionWave(
                "en-US",
                new byte[0],
                TimeSpan.FromSeconds(5),
                new TestGreetingIntent(_world, State.RepeatCount, State.Radio));
            
            State.Radio.Get().AIEnqueueTransmission(wave);
            _store.Dispatch(this, new IncrementRepeatCountEvent());
        }

        public static void RegisterType(ISupervisorActorInit supervisor)
        {
            supervisor.RegisterActorType<DummyControllerActor, DummyActivationEvent>(
                TypeString,
                (activation, dependencies) => new DummyControllerActor(
                    dependencies.Resolve<IStateStore>(),
                    dependencies.Resolve<IWorldContext>(), 
                    activation
                )
            );
        }

        private class DummyControllerParty : ControllerParty
        {
            public DummyControllerParty(
                string uniqueId, 
                string callsign, 
                NatureType nature, 
                VoiceDescription defaultVoice, 
                GenderType gender, 
                AgeType age, 
                string? firstName) 
                : base(uniqueId, callsign, nature, defaultVoice, gender, age, firstName)
            {
            }

            public override void ReceiveIntent(Intent intent)
            {
            }
        }

        public Party Party => _party;
    }
}
