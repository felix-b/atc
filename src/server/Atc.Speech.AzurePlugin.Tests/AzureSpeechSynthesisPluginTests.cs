using System.Threading;
using System.Threading.Tasks;
using Atc.Data;
using Atc.Data.Primitives;
using Atc.Sound;
using Atc.World.Abstractions;
using Atc.World.Testability;
using Atc.World.Tests;
using NUnit.Framework;

namespace Atc.Speech.AzurePlugin.Tests
{
    [TestFixture, Category("e2e")]
    public class AzureSpeechSynthesisPluginTests
    {
        [Test]
        public async Task SayHello()
        {
            var setup = new WorldSetup();
            using var audioContext = new AudioContextScope(setup.DependencyContext.Resolve<ISoundSystemLogger>());
            var player = new RadioSpeechPlayer(setup.Environment);
            
            var plugin = new AzureSpeechSynthesisPlugin();
            var utterance = new UtteranceDescription(
                new LanguageCode("he-IL"),
                new UtteranceDescription.Part[] {
                    new UtteranceDescription.Part(UtteranceDescription.PartType.Text, "Hello, world!")
                }
            );
                
            var wave = await plugin.SynthesizeUtteranceWave(
                utterance,    
                VoiceDescription.Default
            );

            await player.Play(
                wave.Wave, 
                AzureSpeechSynthesisPlugin.SoundFormat,
                volume: 1.0f,  
                CancellationToken.None);
        }
    }
}