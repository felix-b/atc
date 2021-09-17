using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using Atc.Speech.Abstractions;

namespace Atc.Speech.WinLocalPlugin
{
    [SupportedOSPlatform("windows")]
    public class WindowsSpeechSynthesisPlugin : ISpeechSynthesisPlugin, IDisposable
    {
        private readonly SpeechSynthesizer _synthesizer = new();
        private readonly IReadOnlyList<VoiceInfo> _voices;
        private readonly IReadOnlyDictionary<string, VoiceInfo> _voiceByName;

        public WindowsSpeechSynthesisPlugin()
        {
            var voiceListBuilder = new List<VoiceInfo>();
            var voiceMapBuilder = new Dictionary<string, VoiceInfo>();
            
            foreach (var voice in _synthesizer.GetInstalledVoices())
            {
                voiceListBuilder.Add(voice.VoiceInfo);
                voiceMapBuilder.Add(voice.VoiceInfo.Name, voice.VoiceInfo);
            }

            _voices = voiceListBuilder;
            _voiceByName = voiceMapBuilder;
        }

        public void Dispose()
        {
            _synthesizer.Dispose();
        }

        public Task<SynthesizeUtteranceWaveResult> SynthesizeUtteranceWave(UtteranceDescription utterance, VoiceDescription voice)
        {
            Console.WriteLine("TEMP MOCK RADIO - TRANSMISSION STARTED.");

            var effectiveVoice = voice.AssignedPlatformVoiceId != null 
                ? _voiceByName[voice.AssignedPlatformVoiceId]
                : LookupVoice(voice);

            var ssml = BuildSsml(utterance, voice);
            Console.WriteLine("SYNTHESIZING SPEECH SSML: " + ssml);

            using var output = new MemoryStream();

            _synthesizer.SelectVoice(effectiveVoice.Name);
            //_synthesizer.SetOutputToWaveStream(output);
            _synthesizer.SpeakSsmlAsync(ssml);  
            //_synthesizer.SetOutputToNull();

            Console.WriteLine("TEMP MOCK RADIO - TRANSMISSION FINISHED.");
            
            return Task.FromResult(new SynthesizeUtteranceWaveResult(
                new byte[0],
                effectiveVoice.Name
            ));
        }

        private VoiceInfo LookupVoice(VoiceDescription description)
        {
            //TODO implement the logic
            return _voices.First(v => v.Culture.Name == description.Culture.Name);
        }

        public string BuildSsml(UtteranceDescription utterance, VoiceDescription voice)
        {
            var ssml = new StringBuilder();
            ssml.Append($"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"{utterance.Culture.Name}\">");
            ssml.Append($"<prosody volume='{voice.Volume}' rate='1.2' pitch='x-high'>");

            foreach (var part in utterance.Parts)
            {
                ssml.Append(part.Text);
                ssml.Append(' ');
            }

            ssml.Append("</prosody>");
            ssml.Append("</speak>");
            
            return ssml.ToString();
        }
    }
}