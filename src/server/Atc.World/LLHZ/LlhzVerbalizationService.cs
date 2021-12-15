using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atc.Data.Primitives;
using Atc.World.Abstractions;
using Atc.World.Comms;
using static Atc.World.Abstractions.UtteranceDescription.PartFactory;
using IntonationType = Atc.World.Abstractions.UtteranceDescription.IntonationType;

namespace Atc.World.LLHZ
{
    public class LlhzVerbalizationService : IVerbalizationService
    {
        private readonly HebrewVerbalizer _hebrewVerbalizer;
        private readonly ISystemEnvironment _environment;

        public LlhzVerbalizationService(ISystemEnvironment environment)
        {
            _environment = environment;
            _hebrewVerbalizer = new HebrewVerbalizer(environment);
        }

        public IVerbalizer GetVerbalizer(PartyDescription speaker, LanguageCode? language = null)
        {
            var effectiveLanguage = language ?? speaker.Voice.Language;

            switch (effectiveLanguage.Code)
            {
                case HebrewVerbalizer.LanguageCodeString:
                    return _hebrewVerbalizer;
                default:
                    throw new NotSupportedException();
            }
        }

        private delegate IEnumerable<UtteranceDescription.Part> IntentVerbalizerCallback(
            PartyDescription speaker,
            Intent intent);

        public class HebrewVerbalizer : IVerbalizer
        {
            public const string LanguageCodeString = "he-IL";
            
            private readonly LanguageCode _language = LanguageCodeString;
            private readonly Dictionary<Type, IntentVerbalizerCallback> _callbackByIntentType = new();
            private readonly ISystemEnvironment _environment;

            public HebrewVerbalizer(ISystemEnvironment environment)
            {
                _environment = environment;
                BuildIntentCallbackMap();
            }

            public UtteranceDescription VerbalizeIntent(PartyDescription speaker, Intent intent)
            {
                var intentType = intent.GetType();
                
                if (_callbackByIntentType.TryGetValue(intentType, out var callback))
                {
                    var parts = callback(speaker, intent);
                    return new UtteranceDescription(Language, parts, TimeSpan.FromSeconds(5));
                }

                throw new NotSupportedException(
                    $"Intent of type '{intentType}' is not supported by current verbalizer");
            }

            public LanguageCode Language => _language;

            private void BuildIntentCallbackMap()
            {
                _callbackByIntentType.Add(typeof(GreetingIntent), OnGreeting);
                _callbackByIntentType.Add(typeof(GoAheadIntent), OnGoAhead);
                _callbackByIntentType.Add(typeof(StartupRequestIntent), OnRequestStartup);
                _callbackByIntentType.Add(typeof(StartupApprovalIntent), OnApproveStartup);
                _callbackByIntentType.Add(typeof(StartupApprovalReadbackIntent), OnReadbackStartupApproval);
            }
            
            private IEnumerable<UtteranceDescription.Part> OnGreeting(PartyDescription speaker, Intent intent)
            {
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);
                SpellCallsign(parts, intent.CallsignCalling, IntonationType.Greeting);
                SpellGreeting(parts, intent);

                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnGoAhead(PartyDescription speaker, Intent intent)
            {
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);
                SpellCallsign(parts, intent.CallsignCalling, IntonationType.Greeting);
                SpellGreeting(parts, intent);
                
                parts.Add(InstructionPart("המשך", IntonationType.Greeting));

                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnRequestStartup(PartyDescription speaker, Intent intent)
            {
                StartupRequestIntent request = (StartupRequestIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, request.CallsignReceivingOrThrow(), IntonationType.Greeting);
                SpellText(parts, string.Join(" ", new[] {
                    SelectRandom("רשות", "מבקש"),
                    SelectRandom("התנעה", "להתניע"),
                    GetStartupText(request.DepartureType)                    
                }));

                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnApproveStartup(PartyDescription speaker, Intent intent)
            {
                StartupApprovalIntent approval = (StartupApprovalIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, approval.CallsignReceivingOrThrow(), IntonationType.Greeting);
                
                if (TossADice(intent))
                {
                    SpellText(parts, string.Join(" ", new[] {
                        "ההתנעה",
                        GetStartupText(approval.VfrClearance!.DepartureType),                    
                        "מאושרת",
                    }));
                }
                else
                {
                    SpellText(parts, string.Join(" ", new[] {
                        "רשאי להתניע",
                        GetStartupText(approval.VfrClearance!.DepartureType)                    
                    }));
                }
                
                SpellAtis(parts, approval, approval.Atis!);

                if (approval.VfrClearance!.InitialNavaid != null && approval.VfrClearance!.InitialAltitude != null)
                {
                    parts.Add(DataPart(string.Join(" ", new[] {
                        approval.VfrClearance.InitialNavaid,
                        SpellAltitude(approval.VfrClearance.InitialAltitude.Value)
                    })));                  
                }
                
                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnReadbackStartupApproval(PartyDescription speaker, Intent intent)
            {
                StartupApprovalReadbackIntent readback = (StartupApprovalReadbackIntent) intent;
                var approval = readback.OriginalIntent;
                var parts = new List<UtteranceDescription.Part>();

                if (TossADice(intent, probability: 30))
                {
                    SpellText(parts, "ההתנעה מאושרת");
                }
                else if (TossADice(intent, probability: 60))
                {
                    SpellText(parts, "רשאי להתניע");
                }
                
                SpellAtisReadback(parts, readback, approval.Atis!);

                if (approval.VfrClearance!.InitialNavaid != null && approval.VfrClearance!.InitialAltitude != null)
                {
                    parts.Add(DataPart(string.Join(" ", new[] {
                        approval.VfrClearance.InitialNavaid,
                        SpellAltitude(approval.VfrClearance.InitialAltitude.Value)
                    })));                  
                }
                
                SpellCallsign(parts, readback.CallsignCalling, IntonationType.Farewell);

                return parts;
            }

            private void SpellText(IList<UtteranceDescription.Part> parts, string text, IntonationType intonation = IntonationType.Neutral)
            {
                parts.Add(TextPart(text, intonation));
            }

            private void SpellCallsign(IList<UtteranceDescription.Part> parts, string callsign, IntonationType intonation)
            {
                var textToSpell = callsign.Substring(callsign.Length - 3, 3);//TODO: fine-tune
                var phoneticTextToSpell = PhoneticAlphabet.SpellString(textToSpell);
                parts.Add(CallsignPart(phoneticTextToSpell, intonation));
            }

            private void SpellAtis(IList<UtteranceDescription.Part> parts, Intent intent, TerminalInformation atis)
            {
                if (TossADice(intent))
                {
                    AddRunwayParts();
                    AddQnhParts();
                }
                else
                {
                    AddQnhParts();
                    AddRunwayParts();
                }
                
                void AddQnhParts() 
                {
                    parts.Add(TextPart("לחץ"));
                    parts.Add(DataPart(PhoneticAlphabet.SpellString(atis.Qnh.InHgX100.ToString())));
                }

                void AddRunwayParts()
                {
                    parts.Add(TextPart(TossADice(intent, probability: 40) ? "מסלול" : "מסלול בשימוש"));
                    parts.Add(DataPart(PhoneticAlphabet.SpellString(atis.ActiveRunwaysDeparture)));
                }
            }

            private void SpellAtisReadback(IList<UtteranceDescription.Part> parts, Intent intent, TerminalInformation atis)
            {
                if (TossADice(intent))
                {
                    AddRunwayParts();
                    AddQnhParts();
                }
                else
                {
                    AddQnhParts();
                    AddRunwayParts();
                }
                
                void AddQnhParts() 
                {
                    if (TossADice(intent))
                    {
                        parts.Add(TextPart("לחץ"));
                    }
                    parts.Add(DataPart(PhoneticAlphabet.SpellString(atis.Qnh.InHgX100.ToString())));
                }

                void AddRunwayParts()
                {
                    if (TossADice(intent))
                    {
                        parts.Add(TextPart(TossADice(intent, probability: 40) ? "מסלול" : "מסלול בשימוש"));
                    }
                    parts.Add(DataPart(PhoneticAlphabet.SpellString(atis.ActiveRunwaysDeparture)));
                }
            }

            private void SpellGreeting(IList<UtteranceDescription.Part> parts, Intent intent)
            {
                parts.Add(TextPart(GetGreetingText(), IntonationType.Greeting));
                
                string GetGreetingText()
                {
                    var hour = intent.Header.CreatedAtUtc.ToLocalTimeAt(intent.Header.OriginatorPosition).Hour;
                    if (hour >= 6 && hour < 11 && TossADice(intent, probability: 70))
                    {
                        return "בוקר טוב";
                    }
                    else if (hour < 16 && TossADice(intent, probability: 50))
                    {
                        return "צהריים טובים";
                    }
                    else if (hour < 23 && TossADice(intent, probability: 70))
                    {
                        return "ערב טוב";
                    }
                    else
                    {
                        return "שלום";
                    }
                }
            }

            private string SpellAltitude(Altitude altitude)
            {
                //TODO
                return ((int) altitude.Feet).ToString();
            }

            private string GetStartupText(DepartureIntentType departureType)
            {
                switch (departureType)
                {
                    case DepartureIntentType.ToTrainingZones:
                        return "לאזורים";
                    case DepartureIntentType.ToStayInPattern:
                        return "להקפות";
                    default:
                        throw new NotSupportedException($"'{nameof(DepartureIntentType)}.{departureType}' is not supported");
                }
            }

            private string SelectRandom(params string[] choices)
            {
                if (choices.Length == 0)
                {
                    throw new ArgumentException("Array of choices cannot be empty", paramName: nameof(choices));
                }

                var selectedIndex = _environment.Random(0, choices.Length);
                return choices[selectedIndex];
            }
            
            private bool TossADice(Intent intent, int probability = 50)
            {
                var dice = (intent.Header.CreatedAtUtc.Millisecond % 100); 
                return (dice < probability);
            }
        }

        public static class PhoneticAlphabet
        {
            private static readonly string[] _digits = {
                "אפס",
                "אחד",
                "שתיים",
                "שלוש",
                "ארבע",
                "חמש",
                "שש",
                "שבע",
                "שמונה",
                "תשע",
            };
            
            private static readonly string[] _letters = {
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
            
            public static string SpellString(string s)
            {
                var builder = new StringBuilder();

                for (int i = 0; i < s.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(' ');
                    }

                    AppendPhoneticCharacter(s[i]);
                }

                return builder.ToString();
                
                void AppendPhoneticCharacter(char c)
                {
                    if (c >= '0' && c <= '9')
                    {
                        var digitIndex = c - '0';
                        builder.Append(_digits[digitIndex]);
                    }
                    else if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z')
                    {
                        var letterIndex = char.ToUpper(c) - 'A';
                        builder.Append(_letters[letterIndex]);
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }
            }
        }
    }
}
