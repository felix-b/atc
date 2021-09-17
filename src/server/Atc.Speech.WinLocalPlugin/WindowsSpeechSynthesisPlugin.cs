using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Speech.AudioFormat;
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
        private MemoryStream? _activeOutput;
        private TaskCompletionSource<SynthesizeUtteranceWaveResult>? _activeCompletion;

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

            _synthesizer.SpeakCompleted += SynthesizerOnSpeakCompleted;
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
            _synthesizer.SelectVoice(effectiveVoice.Name);

            var ssml = BuildSsml(utterance, voice);

            _activeOutput = new MemoryStream();
            
            var format = new SpeechAudioFormatInfo(samplesPerSecond: 11025, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
            _synthesizer.SetOutputToAudioStream(_activeOutput, format);

            _activeCompletion = new TaskCompletionSource<SynthesizeUtteranceWaveResult>();
            
            Console.WriteLine("SYNTHESIZING SPEECH SSML: " + ssml);

            _synthesizer.SpeakSsmlAsync(ssml);

            return _activeCompletion.Task;
        }

        private void SynthesizerOnSpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            if (_activeOutput != null && _activeCompletion != null)
            {
                _synthesizer.SetOutputToNull();
                _activeCompletion.SetResult(new(_activeOutput.ToArray(), null));
                Console.WriteLine($"SYNTHESIZER COMPLETED! wave buffer size = {_activeOutput.Length}");
                _activeOutput.Dispose();
            }
            else
            {
                Console.WriteLine($"SYNTHESIZER WARNING! completed but no active output/completion!");
            }

            _activeCompletion = null;
            _activeOutput = null;
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
            ssml.Append($"<prosody rate='1.1' pitch='medium'>");

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