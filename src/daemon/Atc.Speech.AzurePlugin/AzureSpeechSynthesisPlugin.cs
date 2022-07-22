using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Atc.Telemetry;
using Atc.World.Contracts.Communications;
using Atc.World.Contracts.Sound;
using Microsoft.CognitiveServices.Speech;
using SpeechSynthesisResult = Atc.World.Contracts.Communications.SpeechSynthesisResult;

namespace Atc.Speech.AzurePlugin;

public class AzureSpeechSynthesisPlugin : ISpeechSynthesisPlugin, IDisposable
{
    private readonly IAudioStreamCache _audioStreamCache;
    private readonly IThisTelemetry _telemetry;
    private readonly ConcurrentDictionary<string, SpeechSynthesizerEntry> _synthesizerByLanguageCode = new();
    private bool _disposed = false;

    public AzureSpeechSynthesisPlugin(IAudioStreamCache audioStreamCache, IThisTelemetry telemetry)
    {
        _audioStreamCache = audioStreamCache;
        _telemetry = telemetry;
        GetOrAddSpeechSynthesizerEntry(LanguageCode.English);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeAllSynthesizers();

        void DisposeAllSynthesizers()
        {
            var snapshotOfSynthesizers = _synthesizerByLanguageCode.ToArray();

            foreach (var kvp in snapshotOfSynthesizers)
            {
                try
                {
                    var entry = kvp.Value;
                    using (entry.SynthesizerLock.Acquire(TimeSpan.FromSeconds(3)))
                    {
                        entry.Synthesizer.Dispose();
                    }
                }
                catch // doesn't matter
                {
                }
            }
        }
    }

    public async Task<SpeechSynthesisResult> SynthesizeSpeech(
        UtteranceDescription utterance, 
        VoiceDescription voice,
        CancellationToken cancellation)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(objectName: nameof(AzureSpeechSynthesisPlugin));
        }

        var entry = GetOrAddSpeechSynthesizerEntry(utterance.Language);
        using var acquiredLock = entry.SynthesizerLock.Acquire(TimeSpan.FromSeconds(5));
            
        var platformVoiceId = voice.AssignedPlatformVoiceId ?? FindPlatformVoiceId(voice);
        var ssml = BuildSsml(utterance, voice, platformVoiceId);

        var clock = Stopwatch.StartNew();
        var result = await entry.Synthesizer.SpeakSsmlAsync(ssml);
        
        Console.WriteLine($"SynthesizeSpeech >>> [{utterance}] -> {result.AudioData.Length} bytes, {clock.ElapsedMilliseconds} ms");
        
        //TODO: use streaming instead of buffering
        
        var stream = _audioStreamCache.CreateStream(SoundFormat, duration: null);
        await stream.AddDataChunk(result.AudioData, cancellation);
        stream.Complete();
        
        return new SpeechSynthesisResult(   
            AudioStreamId: stream.Id,
            AssignedPlatformVoiceId: platformVoiceId,
            Duration: stream.Duration 
        );
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

        ssml.Append($"<prosody volume='{GetVolumeSetting()}' rate='{GetRateSetting()}' pitch='{GetPitchSetting()}'>");

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

        string GetPitchSetting()
        {
            switch (voice.Type)
            {
                case VoiceType.Bass: 
                    return "x-low";
                case VoiceType.Contralto: 
                    return "low";
                case VoiceType.Baritone: 
                    return "low";
                case VoiceType.MezzoSoprano: 
                    return "medium";
                case VoiceType.Tenor: 
                    return "medium";
                case VoiceType.Soprano: 
                    return "high";
                case VoiceType.Countertenor: 
                    return "high";
                case VoiceType.Treble: 
                    return "x-high";
                default:
                    return "default";
            }
        }

        string GetRateSetting()
        {
            switch (voice.Rate)
            {
                case VoiceRate.Fast: return "1.2";
                case VoiceRate.Medium: return "1.1";
                case VoiceRate.Slow: return "1";
                default: return "default";
            }
        }

        string GetVolumeSetting()
        {
            var intVolume = (int)Math.Round(100 * voice.Volume);
            return intVolume.ToString();
        }
    }

    private SpeechSynthesizerEntry GetOrAddSpeechSynthesizerEntry(LanguageCode language)
    {
        return _synthesizerByLanguageCode.GetOrAdd(language.Code, code => {
            var config = CreateSpeechConfig(code);
            var synthesizer = new SpeechSynthesizer(config, audioConfig: null);
            return new SpeechSynthesizerEntry(
                synthesizer,
                new ExclusiveLock($"speech-synthesizer-{language.Code}")
            );
        });
    }

    private SpeechConfig CreateSpeechConfig(string languageCode)
    {
        var config = SpeechConfigFactory.SubscriptionFromEnvironment();
        config.SpeechSynthesisLanguage = languageCode;
        config.SetSpeechSynthesisOutputFormat(SynthesisOutputFormat);
        return config;
    }

    public static readonly SoundFormat SoundFormat = 
        new SoundFormat(bitsPerSample: 16, samplesPerSecond: 8000, channelCount: 1);
        
    public static readonly SpeechSynthesisOutputFormat SynthesisOutputFormat = 
        SpeechSynthesisOutputFormat.Raw8Khz16BitMonoPcm;

    public interface IThisTelemetry : ITelemetry
    {
        
    }
    
    private record SpeechSynthesizerEntry(
        SpeechSynthesizer Synthesizer, 
        ExclusiveLock SynthesizerLock
    );
}
