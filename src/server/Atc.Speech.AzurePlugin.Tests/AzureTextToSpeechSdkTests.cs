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

        [Test]
        public void SpeakPhoneticAlphabet()
        {
            var alphabet = new[] {
                "אלפא",
                "בראבו",
                "צ'ארלי",
                "דלתה",
                "אקו",
                "פוקסטרוט",
                "גולף",
                "הוטל",
                "אינדיה",
                "ג'ולייט",
                "קילו",
                "לימה",
                "מאיק",
                "נובמבר",
                "אוסקאר",
                "פאפא",
                "קובק",
                "רומאו",
                "סיירה",
                "טאנגו",
                "יוניפורם",
                "ויקטור",
                "וויסקי",
                "אקסראי",
                "יאנקי",
                "זולו",
            };
            
            TestUsingVoice("he-IL-HilaNeural");
            TestUsingVoice("he-IL-AvriNeural");
            
            void TestUsingVoice(string voiceName)
            {
                var config = SpeechConfigFactory.SubscriptionFromEnvironment();
                config.SpeechSynthesisLanguage = "he-IL";
                config.SpeechSynthesisVoiceName = voiceName;
                config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm);
            
                using var synthesizer = new SpeechSynthesizer(config);

                synthesizer.SpeakTextAsync(string.Join(" ", alphabet)).Wait();
            }
        }
    }
}