﻿using System;
using System.Collections.Generic;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Comms.Verbalizers;

namespace Atc.World.Testability.Comms
{
    public class TestVerbalizationService : IVerbalizationService
    {
        public IVerbalizer GetVerbalizer(PartyDescription speaker, LanguageCode? language = null)
        {
            return new TestVerbalizer();
        }
                
        public class TestVerbalizer : IVerbalizer
        {
            public UtteranceDescription VerbalizeIntent(PartyDescription speaker, Intent intent)
            {
                if (intent is TestGreetingIntent greeting)
                {
                    return new UtteranceDescription(Language, new[] {
                        new UtteranceDescription.Part(
                            UtteranceDescription.PartType.Text, 
                            $"This is a test greeting number {greeting.RepeatCount} from {intent.Header.OriginatorCallsign}",
                            UtteranceDescription.IntonationType.Greeting
                        )
                    }, estimatedDuration: TimeSpan.FromSeconds(5));
                }

                return new UtteranceDescription(Language, new[] {
                    new UtteranceDescription.Part(
                        UtteranceDescription.PartType.Text, 
                        intent.ToString()
                    )
                }, estimatedDuration: TimeSpan.FromSeconds(5));
            }

            public LanguageCode Language { get; } = "en-US";
        }
    }
}