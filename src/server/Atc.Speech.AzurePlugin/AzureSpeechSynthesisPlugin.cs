using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atc.World.Abstractions;
using Just.Utility;
using Microsoft.CognitiveServices.Speech;

namespace Atc.Speech.AzurePlugin
{
    public class AzureSpeechSynthesisPlugin : ISpeechSynthesisPlugin, IDisposable
    {
        private readonly SpeechConfig _speechConfig;
        private readonly SpeechSynthesizer _synthesizer;
        private readonly ExclusiveLock _synthesizerLock;

        public AzureSpeechSynthesisPlugin()
        {
            _speechConfig = CreateSpeechConfig();
            _synthesizer = new SpeechSynthesizer(_speechConfig, audioConfig: null);
            _synthesizerLock = new ExclusiveLock("AzureSpeechSynthesizer");
        }
        
        public void Dispose()
        {
            _synthesizer.Dispose();
        }

        public async Task<SynthesizeUtteranceWaveResult> SynthesizeUtteranceWave(UtteranceDescription utterance, VoiceDescription voice)
        {
            using var acquiredLock = _synthesizerLock.Acquire(TimeSpan.FromSeconds(5));
            
            var platformVoiceId = voice.AssignedPlatformVoiceId ?? FindPlatformVoiceId(voice);
            var ssml = BuildSsml(utterance, voice, platformVoiceId);
            
            //using var synthesizer = new SpeechSynthesizer(_speechConfig, audioConfig: null);
            var result = await _synthesizer.SpeakSsmlAsync(ssml);

            return new SynthesizeUtteranceWaveResult(
                Wave: result.AudioData, 
                AssignedPlatformVoiceId: platformVoiceId,
                Format: SoundFormat);
        }

        private string FindPlatformVoiceId(VoiceDescription voice)
        {
            //TODO
            
            switch (voice.Language.Code)
            {
                case "he-IL":
                    return voice.Gender == GenderType.Male 
                        ? "he-IL-AvriNeural" 
                        : "he-IL-HilaNeural";
                default:
                    return voice.Gender == GenderType.Male 
                        ? "en-US-GuyNeural" 
                        : "en-US-JennyNeural";
            }
        }
        
        private string BuildSsml(UtteranceDescription utterance, VoiceDescription voice, string platformVoiceId)
        {
            var ssml = new StringBuilder();
                
            ssml.Append(
                "<speak " + 
                "xmlns=\"http://www.w3.org/2001/10/synthesis\" " + 
                "xmlns:mstts=\"http://www.w3.org/2001/mstts\" " + 
                "xmlns:emo=\"http://www.w3.org/2009/10/emotionml\" " + 
                "version=\"1.0\" " +
                "xml:lang=\"he-IL\" >");
            ssml.Append(
                $"<voice name=\"{platformVoiceId}\">");

            ssml.Append($"<prosody volume='100' rate='1.2' pitch='low'>");

            foreach (var part in utterance.Parts)
            {
                ssml.Append(part.Type != UtteranceDescription.PartType.Punctuation 
                    ? part.Text 
                    : ",");
                ssml.Append(' ');
            }

            ssml.Append("</prosody>");
            ssml.Append("</voice>");
            ssml.Append("</speak>");

            return ssml.ToString();
        }

        public static readonly SoundFormat SoundFormat = 
            new SoundFormat(bitsPerSample: 16, samplesPerSecond: 16000, channelCount: 1);
        
        public static readonly SpeechSynthesisOutputFormat SynthesisOutputFormat = 
            SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm;
        
        private static SpeechConfig CreateSpeechConfig()
        {
            var config = SpeechConfigFactory.SubscriptionFromEnvironment();
            config.SpeechSynthesisLanguage = "he-IL";
            config.SetSpeechSynthesisOutputFormat(SynthesisOutputFormat);
            return config;
        }

    }
}
