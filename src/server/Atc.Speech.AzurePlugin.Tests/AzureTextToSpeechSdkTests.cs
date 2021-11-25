using Microsoft.CognitiveServices.Speech;
using NUnit.Framework;

namespace Atc.Speech.AzurePlugin.Tests
{
    [TestFixture(Category = "e2e")]
    public class AzureTextToSpeechSdkTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TrySpeak()
        {
            var config = SpeechConfigFactory.SubscriptionFromEnvironment();
            config.SpeechSynthesisLanguage = "he-IL";
            config.SpeechSynthesisVoiceName = "he-IL-HilaNeural";
            config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm);
            
            using var synthesizer = new SpeechSynthesizer(config);
            synthesizer.SpeakTextAsync("צ'ארלי גולף קילו שלום, תמתין במקום").Wait();
        }
    }
}