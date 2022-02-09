using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atc.Data.Control;
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
                _callbackByIntentType.Add(typeof(LandingClearanceReadbackIntent), OnLandingClearanceReadbackIntent);
                _callbackByIntentType.Add(typeof(GoAheadIntent), OnGoAhead);
                _callbackByIntentType.Add(typeof(StartupRequestIntent), OnRequestStartup);
                _callbackByIntentType.Add(typeof(StartupApprovalIntent), OnApproveStartup);
                _callbackByIntentType.Add(typeof(StartupApprovalReadbackIntent), OnReadbackStartupApproval);
                _callbackByIntentType.Add(typeof(MonitorFrequencyIntent), OnMonitorFrequencyIntent);
                _callbackByIntentType.Add(typeof(MonitorFrequencyReadbackIntent), OnMonitorFrequencyReadbackIntent);
                _callbackByIntentType.Add(typeof(DepartureTaxiRequestIntent), OnDepartureTaxiRequestIntent);
                _callbackByIntentType.Add(typeof(DepartureTaxiClearanceIntent), OnDepartureTaxiClearanceIntent);
                _callbackByIntentType.Add(typeof(DepartureTaxiClearanceReadbackIntent), OnDepartureTaxiClearanceReadbackIntent);
                _callbackByIntentType.Add(typeof(ReportReadyForDepartureIntent), OnReportReadyForDepartureIntent);
                _callbackByIntentType.Add(typeof(TakeoffClearanceIntent), OnTakeoffClearanceIntent);
                _callbackByIntentType.Add(typeof(TakeoffClearanceReadbackIntent), OnTakeoffClearanceReadbackIntent);
                _callbackByIntentType.Add(typeof(ReportDownwindIntent), OnReportDownwindIntent);
                _callbackByIntentType.Add(typeof(LandingSequenceAssignmentIntent), OnLandingSequenceAssignmentIntent);
                _callbackByIntentType.Add(typeof(LandingSequenceAssignmentReadbackIntent), OnLandingSequenceAssignmentReadbackIntent);
                _callbackByIntentType.Add(typeof(FinalApproachReportIntent), OnReportFinalApproachIntent);
                _callbackByIntentType.Add(typeof(LandingClearanceIntent), OnLandingClearanceIntent);
                _callbackByIntentType.Add(typeof(FarewellIntent), OnFarewell);
                _callbackByIntentType.Add(typeof(ReportRemainingCircuitCountIntent), OnReportRemainingCircuitCountIntent);
                _callbackByIntentType.Add(typeof(ReadbackRemainingCircuitCountIntent), OnReadbackRemainingCircuitCountIntent);
            }
            
            private IEnumerable<UtteranceDescription.Part> OnGreeting(PartyDescription speaker, Intent intent)
            {
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);
                SpellCallsign(parts, intent.CallsignCalling, IntonationType.Greeting);
                SpellGreeting(parts, intent);

                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnFarewell(PartyDescription speaker, Intent intent)
            {
                var parts = new List<UtteranceDescription.Part>();
                var isThanks = (intent.Options.Flags & IntentOptionFlags.HasThanks) != 0;

                if (isThanks)
                {
                    SpellCallsign(parts, intent.CallsignCalling, IntonationType.Farewell);
                    parts.Add(TextPart("תודה רבה"));
                }
                else
                {
                    parts.Add(TextPart(TossADice(intent)
                        ? "bye" + " " + "יום טוב"
                        : "בבקשה יום טוב"));
                }

                return parts;
            } 
            
            private IEnumerable<UtteranceDescription.Part> OnGoAhead(PartyDescription speaker, Intent intent)
            {
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);
                SpellCallsign(parts, intent.CallsignCalling, IntonationType.Greeting);

                if (TossADice(intent))
                {
                    SpellGreeting(parts, intent);
                }

                parts.Add(PunctuationPart());
                parts.Add(InstructionPart("המשך", IntonationType.Greeting));

                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnRequestStartup(PartyDescription speaker, Intent intent)
            {
                StartupRequestIntent request = (StartupRequestIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, request.CallsignCalling, IntonationType.Greeting);
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
                
                SpellTerminalInfo(parts, approval, approval.Information!);

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
                
                SpellTerminalInfoReadback(parts, readback, approval.Information!);

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

            private IEnumerable<UtteranceDescription.Part> OnMonitorFrequencyIntent(PartyDescription speaker, Intent intent)
            {
                MonitorFrequencyIntent instruction = (MonitorFrequencyIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);

                string controllerText = (instruction.ControllerType.HasValue
                    ? SpellControllerPositionType(instruction.ControllerType.Value)
                    : instruction.ControllerCallsign) 
                    ?? throw new ArgumentException(
                        "OnMonitorFrequencyIntent: either controller position type or callsign must be specified", 
                        nameof(intent));

                var frequencyText = SpellFrequency(instruction.Frequency);

                if (instruction.Options.Condition != null)
                {
                    parts.Add(TextPart(SpellIntentConditionText(instruction.Options.Condition)));
                }
                
                parts.Add(DataPart("עבור ל" + controllerText + " " + "ב" + frequencyText));
                parts.Add(FarewellPart("להישמע"));

                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnMonitorFrequencyReadbackIntent(PartyDescription speaker, Intent intent)
            {
                MonitorFrequencyReadbackIntent readback = (MonitorFrequencyReadbackIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                string controllerText = (readback.OriginalIntent.ControllerType.HasValue
                        ? SpellControllerPositionType(readback.OriginalIntent.ControllerType.Value)
                        : readback.OriginalIntent.ControllerCallsign) 
                    ?? throw new ArgumentException(
                        "OnMonitorFrequencyReadbackIntent: either controller position type or callsign must be specified", 
                        nameof(intent));

                var frequencyText = SpellFrequency(readback.OriginalIntent.Frequency);
                
                parts.Add(DataPart(controllerText + " " + "ב" + frequencyText));
                parts.Add(FarewellPart("להישמע תודה"));

                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnDepartureTaxiRequestIntent(PartyDescription speaker, Intent intent)
            {
                DepartureTaxiRequestIntent request = (DepartureTaxiRequestIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                if (request.ParkingStand != null)
                {
                    SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);
                    SpellGreeting(parts, intent);
                    SpellCallsign(parts, intent.CallsignCalling, IntonationType.Greeting);
                    parts.Add(DataPart("ב" + PhoneticAlphabet.SpellString(request.ParkingStand)));
                }
                else
                {
                    var heads = TossADice(request);
                    SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);
                    if (heads)
                    {
                        SpellGreeting(parts, intent);
                    }
                    SpellCallsign(parts, intent.CallsignCalling, IntonationType.Greeting);
                    if (!heads)
                    {
                        SpellGreeting(parts, intent);
                    }
                }

                parts.Add(TossADice(intent) 
                    ? TextPart("מוכן להסיעה")
                    : TextPart("רשות הסעה"));

                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnDepartureTaxiClearanceIntent(PartyDescription speaker, Intent intent)
            {
                DepartureTaxiClearanceIntent clearance = (DepartureTaxiClearanceIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);
                
                if (clearance.HoldingPoint != null)
                { 
                    parts.Add(DataPart("תסיע ל" + clearance.HoldingPoint));
                }
                else
                {
                    parts.Add(
                        TossADice(intent)
                        ? TextPart("רשאי להסיע")
                        : TextPart("תסיע"));
                }
                
                parts.Add(
                    TossADice(intent) 
                    ? DataPart(SpellRunway(clearance.ActiveRunway) + " " + "בשימוש")
                    : DataPart("מסלול בשימוש" + " " + SpellRunway(clearance.ActiveRunway)));

                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnDepartureTaxiClearanceReadbackIntent(PartyDescription speaker, Intent intent)
            {
                DepartureTaxiClearanceReadbackIntent readback = (DepartureTaxiClearanceReadbackIntent) intent;
                var parts = new List<UtteranceDescription.Part>();
                var clearance = readback.OriginalIntent;
                
                if (clearance.HoldingPoint != null)
                { 
                    parts.Add(DataPart("מסיע ל" + clearance.HoldingPoint));
                    parts.Add(TossADice(intent)
                        ? DataPart(SpellRunway(clearance.ActiveRunway) + " " + "בשימוש")
                        : DataPart("מסלול בשימוש" + " " + SpellRunway(clearance.ActiveRunway)));
                }
                else
                {
                    parts.Add(TossADice(intent)
                        ? TextPart("רשאי להסיע")
                        : TextPart("מסיע"));

                    parts.Add(DataPart(SpellRunway(clearance.ActiveRunway)));
                }
                
                SpellCallsign(parts, intent.CallsignCalling, IntonationType.Farewell);
                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnReportReadyForDepartureIntent(PartyDescription speaker, Intent intent)
            {
                ReportReadyForDepartureIntent report = (ReportReadyForDepartureIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, intent.CallsignCalling, IntonationType.Greeting);
                parts.Add(TossADice(intent)
                    ? TextPart("מוכן לעזיבה")
                    : TextPart("מוכן להתיישר"));
                
                return parts;
            }
            
            private IEnumerable<UtteranceDescription.Part> OnTakeoffClearanceIntent(PartyDescription speaker, Intent intent)
            {
                TakeoffClearanceIntent clearance = (TakeoffClearanceIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);
                parts.Add(DataPart(SpellWind(clearance.Wind)));
                parts.Add(TossADice(clearance)
                    ? DataPart("מסלול" + " " + SpellRunway(clearance.Runway))
                    : DataPart(SpellRunway(clearance.Runway)));

                parts.Add(InstructionPart("רשאי להמריא"));
                
                if ((clearance.Options.Flags & IntentOptionFlags.Expedite) != 0)
                {
                    parts.Add(TossADice(clearance)
                        ? InstructionPart("המראה מיידית")
                        : InstructionPart("ללא עיכוב"));
                }

                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnTakeoffClearanceReadbackIntent(PartyDescription speaker, Intent intent)
            {
                TakeoffClearanceReadbackIntent readback = (TakeoffClearanceReadbackIntent) intent;
                var parts = new List<UtteranceDescription.Part>();
                var runwayPart = DataPart(SpellRunway(readback.OriginalIntent.Runway)); 
                var heads = TossADice(readback);

                if (heads)
                {
                    parts.Add(runwayPart);
                }

                parts.Add(InstructionPart("רשאי להמריא"));
                
                if ((readback.OriginalIntent.Options.Flags & IntentOptionFlags.Expedite) != 0)
                {
                    parts.Add(TossADice(readback)
                        ? InstructionPart("המראה מיידית")
                        : InstructionPart("ללא עיכוב"));
                }

                if (!heads)
                {
                    parts.Add(runwayPart);
                }

                SpellCallsign(parts, intent.CallsignCalling, IntonationType.Farewell);
                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnReportDownwindIntent(PartyDescription speaker, Intent intent)
            {
                ReportDownwindIntent report = (ReportDownwindIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, report.CallsignCalling, IntonationType.Greeting);
                parts.Add(TextPart("עם הרוח"));
                
                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnLandingSequenceAssignmentIntent(PartyDescription speaker, Intent intent)
            {
                LandingSequenceAssignmentIntent assignment = (LandingSequenceAssignmentIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);
                parts.Add(DataPart(
                    "מספר" + " " + 
                    PhoneticAlphabet.SpellString(assignment.LandingSequenceNumber.ToString())));
                
                if (assignment.Options.Traffic != null)
                {
                    SpellTraffic(parts, assignment, assignment.Options.Traffic);
                }
                
                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnLandingSequenceAssignmentReadbackIntent(PartyDescription speaker, Intent intent)
            {
                LandingSequenceAssignmentReadbackIntent readback = (LandingSequenceAssignmentReadbackIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                parts.Add(DataPart(
                    (TossADice(readback) ? "מספר" + " " : "") + 
                    PhoneticAlphabet.SpellString(readback.OriginalIntent.LandingSequenceNumber.ToString())));
                
                SpellCallsign(parts, intent.CallsignCalling, IntonationType.Farewell);
                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnReportFinalApproachIntent(PartyDescription speaker, Intent intent)
            {
                FinalApproachReportIntent report = (FinalApproachReportIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, report.CallsignCalling, IntonationType.Greeting);

                parts.Add(TossADice(report)
                    ? TextPart("final")
                    : TextPart("final" + " " + SpellRunway(report.Runway)));
                
                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnLandingClearanceIntent(PartyDescription speaker, Intent intent)
            {
                LandingClearanceIntent clearance = (LandingClearanceIntent) intent;
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);
                parts.Add(DataPart(SpellWind(clearance.Wind)));
                parts.Add(TossADice(clearance)
                    ? DataPart("מסלול" + " " + SpellRunway(clearance.Runway))
                    : DataPart(SpellRunway(clearance.Runway)));

                var landingTypeText = clearance.LandingType == LandingType.FullStop
                    ? "לנחות"
                    : (TossADice(clearance) ? "לגעת" : "touch and go");
                parts.Add(InstructionPart("רשאי" + " " + landingTypeText));

                return parts;
            }

            private IEnumerable<UtteranceDescription.Part> OnLandingClearanceReadbackIntent(PartyDescription speaker, Intent intent)
            {
                LandingClearanceReadbackIntent readback = (LandingClearanceReadbackIntent) intent;
                var parts = new List<UtteranceDescription.Part>();
                var landingTypeText = readback.OriginalIntent.LandingType == LandingType.FullStop
                    ? "לנחות"
                    : (TossADice(readback.OriginalIntent) ? "לגעת" : "touch and go");
                var heads = TossADice(readback);

                if (heads)
                {
                    parts.Add(DataPart(SpellRunway(readback.OriginalIntent.Runway)));
                }

                if (TossADice(readback, probability: 30))
                {
                    parts.Add(InstructionPart("רשאי"));
                    parts.Add(PunctuationPart());
                }
                else
                {
                    parts.Add(InstructionPart("רשאי" + " " + landingTypeText));
                    if (TossADice(readback, probability: 60))
                    {
                        parts.Add(DataPart(SpellRunway(readback.OriginalIntent.Runway)));
                    }
                }

                SpellCallsign(parts, intent.CallsignCalling, IntonationType.Farewell);

                return parts;
            }
            
            private IEnumerable<UtteranceDescription.Part> OnReportRemainingCircuitCountIntent(PartyDescription speaker, Intent intent)
            {
                var parts = new List<UtteranceDescription.Part>();

                SpellCallsign(parts, intent.CallsignCalling, IntonationType.Greeting);

                parts.Add(DataPart(TossADice(intent)
                    ? "שתיים לסיום"
                    : "אלה יהיו שתי הקפות אחרונות"
                ));

                return parts;
            }
            
            private IEnumerable<UtteranceDescription.Part> OnReadbackRemainingCircuitCountIntent(PartyDescription speaker, Intent intent)
            {
                var parts = new List<UtteranceDescription.Part>();
                var heads = TossADice(intent);

                if (heads)
                {
                    SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting);
                }

                parts.Add(DataPart(TossADice2(intent)
                    ? "שתיים לסיום"
                    : "שתי הקפות אחרונות"
                ));

                if (!heads)
                {
                    SpellCallsign(parts, intent.CallsignReceivingOrThrow(), IntonationType.Greeting, useForPreposition: true);
                }
                
                return parts;
            }
            
            private void SpellText(IList<UtteranceDescription.Part> parts, string text, IntonationType intonation = IntonationType.Neutral)
            {
                parts.Add(TextPart(text, intonation));
            }

            private void SpellCallsign(
                IList<UtteranceDescription.Part> parts, 
                string callsign, 
                IntonationType intonation, 
                bool useForPreposition = false)
            {
                var isPhonetic = callsign.Any(c => char.IsDigit(c));
                var text = isPhonetic
                    ? PhoneticAlphabet.SpellString(callsign.Substring(callsign.Length - 3, 3)) //TODO: fine-tune
                    : callsign;
                var preposition = useForPreposition
                    ? "ל"
                    : string.Empty;
                parts.Add(CallsignPart(preposition + text, intonation));
            }

            private void SpellTerminalInfo(IList<UtteranceDescription.Part> parts, Intent intent, TerminalInformation info)
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
                    parts.Add(TextPart("הלחץ"));
                    parts.Add(DataPart(PhoneticAlphabet.SpellString(info.Qnh.InHgX100.ToString())));
                }

                void AddRunwayParts()
                {
                    //parts.Add(TextPart(TossADice(intent, probability: 40) ? "מסלול" : "מסלול בשימוש"));
                    parts.Add(TextPart("מסלול בשימוש"));
                    parts.Add(DataPart(PhoneticAlphabet.SpellString(info.ActiveRunwaysDepartureCommaSeparated)));
                }
            }

            private void SpellTerminalInfoReadback(IList<UtteranceDescription.Part> parts, Intent intent, TerminalInformation info)
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
                    parts.Add(DataPart(PhoneticAlphabet.SpellString(info.Qnh.InHgX100.ToString())));
                }

                void AddRunwayParts()
                {
                    if (TossADice(intent))
                    {
                        parts.Add(TextPart(TossADice(intent, probability: 40) ? "מסלול" : "מסלול בשימוש"));
                    }
                    parts.Add(DataPart(PhoneticAlphabet.SpellString(info.ActiveRunwaysDepartureCommaSeparated)));
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

            private string SpellFrequency(Frequency frequency)
            {
                var digitsToSpell = frequency.Khz.ToString().TrimEnd('0');
                return PhoneticAlphabet.SpellString(digitsToSpell);
            }

            private string SpellWind(Wind wind)
            {
                if (wind.Speed == null || wind.Speed.Value.Max.Knots <= 3)
                {
                    return "רוח קלה";
                }

                var text = new StringBuilder();
                text.Append("רוח");
                text.Append(' ');
                text.Append(GetDirectionText());
                text.Append(' ');
                text.Append(GetSpeedText());

                var gustsText = GetGustsText();
                if (gustsText != null)
                {
                    text.Append(' ');
                    text.Append(gustsText);
                }

                return text.ToString(); 

                
                string GetDirectionText()
                {
                    if (wind.Direction == null)
                    {
                        return "משתנית";
                    }

                    var toDegrees = PhoneticAlphabet.SpellString(wind.Direction.Value.Max.Degrees.RoundToInt32().ToString()); 
                    
                    if (wind.Direction.Value.IsRange)
                    {
                        var fromDegrees = PhoneticAlphabet.SpellString(wind.Direction.Value.Min.Degrees.RoundToInt32().ToString());
                        return fromDegrees  + " " + "עד" + " " + toDegrees;
                    }

                    return toDegrees;
                }

                string SpellKnots(Speed speed)
                {
                    var intValue = speed.Knots.RoundToInt32();
                    switch (intValue)
                    {
                        case 1:
                            return "אחד קשר";
                        case 2:
                            return "שני קשרים";
                        default:
                            return PhoneticAlphabet.SpellString(intValue.ToString(), useMasculineDigits: true) + " " + "קשרים";
                    }
                }
                
                string GetSpeedText()
                {
                    if (wind.Speed.Value.IsRange)
                    {
                        return
                            SpellKnots(wind.Speed.Value.Min) + 
                            " " + "עד" + " " +
                            SpellKnots(wind.Speed.Value.Max);
                    }

                    return SpellKnots(wind.Speed.Value.Max);
                }

                string? GetGustsText()
                {
                    if (wind.Gust == null)
                    {
                        return null;
                    }
                    
                    return "משובים עד" + " " + SpellKnots(wind.Gust.Value);
                }
            }

            private string SpellRunway(string name)
            {
                return PhoneticAlphabet.SpellString(name);
            }

            private string SpellIntentConditionText(IntentCondition condition)
            {
                if (condition.SubjectType == ConditionSubjectType.Startup && condition.Timing == ConditionTimingType.After)
                {
                    return "לאחר התנעה";
                }

                throw new NotSupportedException("Specified intent condition is not supported");
            }

            private void SpellTraffic(IList<UtteranceDescription.Part> parts, Intent intent, TrafficAdvisory traffic)
            {
                if (traffic != null && 
                    traffic.Location.Pattern.HasValue && 
                    traffic.Location.RelativeOrdering == TrafficAdvisoryLocationOrdering.InFront)
                {
                    parts.Add(DataPart("לפניך" + " " + "ב" + GetPatternLocationText()));
                }

                string GetPatternLocationText()
                {
                    var patternEdgeText = GetPatternEdgeText();
                    if (traffic.Location.Refinement != null && traffic.Location.Refinement != TrafficAdvisoryLocationRefinement.None)
                    {
                        switch (traffic.Location.Refinement.Value)
                        {
                            case TrafficAdvisoryLocationRefinement.BeginningOf:
                                return "תחילת " + patternEdgeText;
                            case TrafficAdvisoryLocationRefinement.MiddleOf:
                                return "אמצע " + patternEdgeText;
                            case TrafficAdvisoryLocationRefinement.EndOf:
                                return (traffic.Location.Pattern.Value == TrafficAdvisoryLocationPattern.Final
                                    ? "פאינל קצר"
                                    : "סוף " + patternEdgeText);
                        }
                    }

                    return patternEdgeText;
                }

                string GetPatternEdgeText()
                {
                    switch (traffic.Location.Pattern.Value)
                    {
                        case TrafficAdvisoryLocationPattern.Upwind: return "צלע מתה";
                        case TrafficAdvisoryLocationPattern.Crosswind: return "צולבת";
                        case TrafficAdvisoryLocationPattern.Downwind: return "עם הרוח";
                        case TrafficAdvisoryLocationPattern.Base: return "בסיס";
                        case TrafficAdvisoryLocationPattern.Final: return "פאינל";
                        default: return string.Empty;
                    }
                }
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

            private string SpellControllerPositionType(ControllerPositionType positionType)
            {
                switch (positionType)
                {
                    case ControllerPositionType.Local:
                        return "מגדל";
                    case ControllerPositionType.ClearanceDelivery:
                        return "קליראנס";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(positionType), "SpellControllerPositionType only supports TWR and CLRDEL");
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

            private bool TossADice2(Intent intent, int probability = 50)
            {
                var dice = (intent.Header.OriginatorCallsign.GetHashCode() % 100); 
                return (dice < probability);
            }
        }

        public static class PhoneticAlphabet
        {
            private static readonly string[] __digitsFeminine = {
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
            
            private static readonly string[] __digitsMasculine = {
                "אפס",
                "אחד",
                "שניים",
                "שלושה",
                "ארבעה",
                "חמישה",
                "שישה",
                "שבעה",
                "שמונה",
                "תשעה",
            };

            private static readonly string[] __letters = {
                "אלפא",
                "בראבו",
                "צ'ארלי",
                "דלתה",
                "Echo",
                "פוקסטרוט",
                "גולף",
                "Hotel",
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
                "Zulu",

                /*
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
                */
            };
            
            public static string SpellString(string s, bool useMasculineDigits = false)
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
                        var digitText = useMasculineDigits ? __digitsMasculine[digitIndex] : __digitsFeminine[digitIndex]; 
                        builder.Append(digitText);
                    }
                    else if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z')
                    {
                        var letterIndex = char.ToUpper(c) - 'A';
                        builder.Append(__letters[letterIndex]);
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
