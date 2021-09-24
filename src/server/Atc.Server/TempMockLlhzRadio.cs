using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atc.Sound;
using Atc.Speech.Abstractions;

namespace Atc.Server
{
    public class TempMockLlhzRadio
    {
        public record Atis(string Info, string ActiveRunway, int Qnh, int WindBearing, int WindSpeedKt);

        private const string CallSignFilePath = @"E:\X-Plane 11\Resources\plugins\atc2\callsign.txt";

        private readonly ISpeechSynthesisPlugin _synthesizer;
        private readonly RadioSpeechPlayer _player;
        private readonly SoundFormat _soundFormat;
        private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("he-IL");
        private Task? _currentWorkflow;
        private Atis _currentAtis;
        private string _currentCallsign;
        private Random _random = new Random(DateTime.Now.TimeOfDay.Milliseconds);
        private CancellationTokenSource? _workflowCancellation;
        private TaskCompletionSource? _pilotTransmissionReceived;
        private TaskCompletionSource? _workflowCancellationRequested;

        public TempMockLlhzRadio(ISpeechSynthesisPlugin synthesizer, RadioSpeechPlayer player)
        {
            _synthesizer = synthesizer;
            _player = player;
            _soundFormat = new SoundFormat(bitsPerSample: 16, samplesPerSecond: 11025, channelCount: 1);
            _currentAtis = CreateRandomAtis();
            _currentCallsign = string.Empty;
            
            ResetFlight();
        }

        public void ResetFlight()
        {
            Console.WriteLine("TEMP MOCK RADIO - RESETTING FLIGHT");

            _workflowCancellation?.Cancel();
            _workflowCancellationRequested?.SetResult();
            
            _pilotTransmissionReceived = new TaskCompletionSource();
            _workflowCancellationRequested = new TaskCompletionSource();
            _workflowCancellation = new CancellationTokenSource();

            _currentWorkflow = SafeRunCommunicationWorkflow(_workflowCancellation.Token);
            _currentAtis = CreateRandomAtis();
            _currentCallsign = LoadCallSignOrDefault("CGK");

            var randomSeed = DateTime.Now.TimeOfDay.Milliseconds;
            _random = new Random(randomSeed);
            
            Console.WriteLine(
                $"TEMP MOCK RADIO - ATIS: qnh[{_currentAtis.Qnh}] rwy[{_currentAtis.ActiveRunway}] wnd[{_currentAtis.WindBearing}@{_currentAtis.WindSpeedKt}kt] rnd-seed[{randomSeed}]");
        }

        public void PttPushed(int frequencyKhz)
        {
            Console.WriteLine($"TEMP MOCK RADIO - PTT PUSHED, {frequencyKhz}");
            _player.StartPttStaticNoise();
        }

        public void PttReleased(int frequencyKhz)
        {
            Console.WriteLine($"TEMP MOCK RADIO - PTT RELEASED, {frequencyKhz}");

            if (frequencyKhz <= 0)
            {
                ResetFlight();
                return;
            }
            
            _player.StopPttStaticNoise();
            _pilotTransmissionReceived?.SetResult();
 
            Console.WriteLine($"TEMP MOCK RADIO - RECEIVE COMPLETED");
        }

        public Atis CurrentAtis => _currentAtis;

        public string CurrentCallsign => _currentCallsign;

        private string LoadCallSignOrDefault(string defaultCallsign)
        {
            if (File.Exists(CallSignFilePath))
            {
                var callsign = File.ReadAllText(CallSignFilePath);
                return string.IsNullOrWhiteSpace(callsign) ? defaultCallsign : callsign;
            }
            return defaultCallsign;
        }

        private async Task SafeRunCommunicationWorkflow(CancellationToken cancel)
        {
            try
            {
                await RunCommunicationWorkflow(cancel);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task RunCommunicationWorkflow(CancellationToken cancel)
        {
            Console.WriteLine("TEMP MOCK RADIO - SARTING COMMUNICATION WORKFLOW");

            await NextPilotTransmission(cancel);                          // CLR hello
            await TransmitLo(CreateClearanceGoAheadUtterance(), cancel);  // go ahead
            await NextPilotTransmission(cancel);                          // request start
            await TransmitLo(CreateStartApprovalUtterance(), cancel);     // start approved
            await NextPilotTransmission(cancel);                          // readback
            await TransmitLo(CreateClearanceHandoffUtterance(), cancel);  // hand off to tower
            await NextPilotTransmission(cancel);                          // readback
            
            await NextPilotTransmission(cancel);                          // TWR request taxi
            await TransmitHi(CreateTaxiClearanceUtterance(), cancel);     // taxi clearance
            await NextPilotTransmission(cancel);                          // readback
            
            await NextPilotTransmission(cancel);                          // TWR ready for departure

            int takeoffRandom;
            
            takeoffRandom = _random.Next(3);
            if (takeoffRandom == 2)
            {
                await TransmitHi(CreateHoldShortUtterance(), cancel);     // hold short
                await NextPilotTransmission(cancel);                      // readback
                await Task.Delay(_random.Next(10000, 60000));
                takeoffRandom = _random.Next(2);
            }
            
            if (takeoffRandom == 1)
            {
                await TransmitHi(CreateLineUpAndWaitUtterance(), cancel); // LUAW
                await NextPilotTransmission(cancel);                      // readback
                await Task.Delay(_random.Next(10000, 60000));
            }
            
            await TransmitHi(CreateTakeoffClearanceUtterance(), cancel);  // cleared for takeoff
            await NextPilotTransmission(cancel);                          // readback

            //---- STAY IN PATTERN ---
            // await NextPilotTransmission(cancel);                        // report downwind
            // await TransmitHi(CreatePatternPositionUtterance(), cancel);   // pattern position
            // await NextPilotTransmission(cancel);                        // readback
            
            //---- GO TO TRAINING ZONES ---
            await NextPilotTransmission(cancel);                           // Batzra 800
            await TransmitHi(CreateEnterTrainingZoneUtterance(), cancel);  // cleared to enter a training zone
            await NextPilotTransmission(cancel);                           // readback
            
            await NextPilotTransmission(cancel);                           // request to leave training zone
            int returnToBaseRandom;
            
            returnToBaseRandom = _random.Next(3);
            if (returnToBaseRandom == 2)
            {
                await TransmitHi(CreateStandByInTrainingZoneUtterance(), cancel); // stand by in the training zone
                await NextPilotTransmission(cancel);                              // readback
                await Task.Delay(_random.Next(10000, 60000));
            }

            await TransmitHi(CreateExitTrainingZoneUtterance(), cancel);    // cleared to Batzra 1200
            await NextPilotTransmission(cancel);                            // readback

            await NextPilotTransmission(cancel);                            // Batzra 1200
            await TransmitHi(CreateJoinPatternUtterance(), cancel);         // join pattern at downwind 11 / r.base 29
            await NextPilotTransmission(cancel);                            // readback

            await NextPilotTransmission(cancel);                            // downwind 11/base 29 at 1200
            await TransmitHi(CreateDescendToPatternAltitudeUtterance(), cancel);   // descend to pattern altitude
            await NextPilotTransmission(cancel);                            // readback

            //----- FINAL ----
            
            await NextPilotTransmission(cancel);                            // report final 
            await TransmitHi(CreateLandingClearanceUtterance(), cancel);    // clear to land
            await NextPilotTransmission(cancel);                            // readback
        }

        private async Task NextPilotTransmission(CancellationToken cancel)
        {
            Console.WriteLine("TEMP MOCK RADIO - AWAITING PILOT TRANSMISSION");
            cancel.ThrowIfCancellationRequested();
            if (_pilotTransmissionReceived != null && _workflowCancellationRequested != null)
            {
                await Task.WhenAny(_pilotTransmissionReceived.Task, _workflowCancellationRequested.Task);
            }
            cancel.ThrowIfCancellationRequested();
            _pilotTransmissionReceived = new TaskCompletionSource();
            Console.WriteLine("TEMP MOCK RADIO - PILOT TRANSMISSION RECEIVED");
        }

        private Task TransmitLo(UtteranceDescription utterance, CancellationToken cancel)
        {
            return Transmit(utterance, 1.0f, cancel);
        }

        private Task TransmitHi(UtteranceDescription utterance, CancellationToken cancel)
        {
            return Transmit(utterance, 1.7f, cancel);
        }

        private async Task Transmit(UtteranceDescription utterance, float volume, CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();
            await Task.Delay(1000, cancel);

            Console.WriteLine("TEMP MOCK RADIO - PREPARING TRANSMISSION");

            var voice = new VoiceDescription(
                utterance.Culture, 
                VoiceGender.Male, 
                VoiceType.Bass, 
                VoiceRate.Medium, 
                VoiceLinkQuality.Medium, 
                volume,
                null);

            var speech = await _synthesizer.SynthesizeUtteranceWave(utterance, voice);

            Console.WriteLine("TEMP MOCK RADIO - TRANSMISSION SYNTHESIZED! PLAYING NOW...");
            
            await _player.Play(speech.Wave, _soundFormat, volume);

            Console.WriteLine("TEMP MOCK RADIO - TRANSMISSION PLAYED TO END");
        }

        private Atis CreateRandomAtis()
        {
            char info = (char)_random.Next('A', 'Z' + 1);
            int windSpeedKt = _random.Next(11);
            int windBearing = _random.Next(360) + 1;
            int qnh = _random.Next(2980, 3005);
            
            var activeRunway = windSpeedKt > 3
                ? (windBearing >= 200 || windBearing <= 20 ? "29" : "11")
                : (_random.Next(33) > 22 ? "11" : "29");

            return new Atis(
                Info: $"{info}",
                activeRunway,
                qnh,
                windBearing,
                windSpeedKt);
        }
        
        private UtteranceDescription CreateClearanceGoAheadUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Affirmation, "שלום, "),
                    new (UtteranceDescription.PartType.Affirmation, "<phoneme alphabet='sapi' ph='h aa m ch eh k k'>המשך</phoneme>."),
                }
            );
        }

        private UtteranceDescription CreateStartApprovalUtterance()
        {
            var qnh = _random.Next(2980, 3000).ToString();
            
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Affirmation, "ההתנעה מאושרת"),
                    new (UtteranceDescription.PartType.Text, "מסלול בשימוש"),
                    new (UtteranceDescription.PartType.Data, SpellPhoneticString(_currentAtis.ActiveRunway)),
                    new (UtteranceDescription.PartType.Text, "הלחץ"),
                    new (UtteranceDescription.PartType.Data, $"<prosody rate='0.8'>{SpellPhoneticString(_currentAtis.Qnh.ToString())}</prosody>"),
                    new (UtteranceDescription.PartType.Farewell, "בצרה 800"),
                }
            );
        }

        private UtteranceDescription CreateClearanceHandoffUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Text, "לאחר התנעה "),
                    new (UtteranceDescription.PartType.Text, "<phoneme alphabet='sapi' ph='h aa v o r'>עבור</phoneme>"),
                    new (UtteranceDescription.PartType.Text, " למגדל"),
                    new (UtteranceDescription.PartType.Data, "אחד-שתיים-שתיים שתיים,"),
                    new (UtteranceDescription.PartType.Farewell, "להישמע."),
                }
            );
        }

        private UtteranceDescription CreateTaxiClearanceUtterance()
        {
            UtteranceDescription.Part[] parts = TossADice()
                ? new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Affirmation, "רשאי להסיע"),
                    new (UtteranceDescription.PartType.Text, "מסלול בשימוש"),
                    new (UtteranceDescription.PartType.Data, SpellPhoneticString(_currentAtis.ActiveRunway)),
                }
                : new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Affirmation, "רשאי להסיע, "),
                    new (UtteranceDescription.PartType.Data, SpellPhoneticString(_currentAtis.ActiveRunway)),
                    new (UtteranceDescription.PartType.Text, "בשימוש."),
                };
            
            return new UtteranceDescription(_culture, parts);
        }

        private UtteranceDescription CreateHoldShortUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Punctuation, ","),
                    new (UtteranceDescription.PartType.Negation, "<phoneme alphabet='sapi' ph='h aa m t e n'>המתן</phoneme>"),
                    new (UtteranceDescription.PartType.Punctuation, "."),
                }
            );
        }

        private UtteranceDescription CreateLineUpAndWaitUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Text, "מסלול"),
                    new (UtteranceDescription.PartType.Data, SpellPhoneticString(_currentAtis.ActiveRunway)),
                    new (UtteranceDescription.PartType.Affirmation, "תתיישר בלבד"),
                }
            );
        }

        private UtteranceDescription CreateTakeoffClearanceUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Data, "רוח"),
                    new (UtteranceDescription.PartType.Data, SpellWind()),
                    new (UtteranceDescription.PartType.Text, "מסלול"),
                    new (UtteranceDescription.PartType.Data, SpellPhoneticString(_currentAtis.ActiveRunway)),
                    new (UtteranceDescription.PartType.Affirmation, "<prosody rate='0.8'>" + "רשאי להמריא" + "</prosody>"),
                }
            );
        }

        private UtteranceDescription CreatePatternPositionUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Text, "מספר"),
                    new (UtteranceDescription.PartType.Data, SpellPhoneticString(TossADice() ? "1" : "2")),
                }
            );
        }

        private UtteranceDescription CreateLandingClearanceUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Data, "רוח"),
                    new (UtteranceDescription.PartType.Data, SpellWind()),
                    new (UtteranceDescription.PartType.Text, "מסלול"),
                    new (UtteranceDescription.PartType.Data, SpellPhoneticString(_currentAtis.ActiveRunway)),
                    new (UtteranceDescription.PartType.Affirmation, "<prosody rate='0.8'>" + "רשאי לנחות" + "</prosody>"),
                }
            );
        }

        private UtteranceDescription CreateEnterTrainingZoneUtterance()
        {
            var zoneNames = new[] {"3", "8", "9", "11", "12", "13"};
            var zoneSquawks = new[] {"5103", "5102", "5105", "5111", "5112", "5113"};
            var zoneIndex = _random.Next(zoneNames.Length);
            
            return new UtteranceDescription(
                _culture,
                TossADice()
                    ? new UtteranceDescription.Part[] {
                        new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                        new (UtteranceDescription.PartType.Text, "כנס לאזור"),
                        new (UtteranceDescription.PartType.Data, SpellPhoneticString(zoneNames[zoneIndex])),
                        new (UtteranceDescription.PartType.Text, "עם"),
                        //new (UtteranceDescription.PartType.Data, SpellPhoneticString(zoneSquawks[zoneIndex])),
                        new (UtteranceDescription.PartType.Data, $"<prosody rate='0.8'>{SpellPhoneticString(zoneSquawks[zoneIndex])}</prosody>"),
                        
                        new (UtteranceDescription.PartType.Affirmation, "על"),
                        new (UtteranceDescription.PartType.Affirmation, "<phoneme alphabet='sapi' ph='h a t r a n s p o o n d e r'>הטרנספונדר</phoneme>"),
                    }
                    : new UtteranceDescription.Part[] {
                        new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                        new (UtteranceDescription.PartType.Text, "אזור"),
                        new (UtteranceDescription.PartType.Data, SpellPhoneticString(zoneNames[zoneIndex])),
                        new (UtteranceDescription.PartType.Text, "<phoneme alphabet='sapi' ph='t r a n s p o o n d e r'>טרנספונדר</phoneme>"),
                        new (UtteranceDescription.PartType.Data, SpellPhoneticString(zoneSquawks[zoneIndex])),
                    }
            );
        }

        private UtteranceDescription CreateExitTrainingZoneUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Text, "צא לבצרה אלף מאתיים"),
                }
            );
        }

        private UtteranceDescription CreateStandByInTrainingZoneUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                    new (UtteranceDescription.PartType.Text, "<phoneme alphabet='sapi' ph='h aa m t e n'>המתן</phoneme>"),
                    new (UtteranceDescription.PartType.Text, "באזור"),
                }
            );
        }

        private UtteranceDescription CreateJoinPatternUtterance()
        {
            var parts = new List<UtteranceDescription.Part> {
                new(UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                new(UtteranceDescription.PartType.Instruction, "שמור אלף מאתיים, "),
            };

            if (_currentAtis.ActiveRunway == "11")
            {
                parts.Add(new (UtteranceDescription.PartType.Text, "<phoneme alphabet='sapi' ph='l e t s e e l a'>לצלע</phoneme>"));
                parts.Add(new(UtteranceDescription.PartType.Text, "עם הרוח שמאלית אחד-אחד"));
            }
            else
            {
                parts.Add(new (UtteranceDescription.PartType.Text, "לבסיס ימנית שתיים-תשע"));
            }
            
            return new UtteranceDescription(
                _culture,
                parts.ToArray()
            );
        }

        private UtteranceDescription CreateDescendToPatternAltitudeUtterance()
        {
            var parts = new List<UtteranceDescription.Part> {
                new(UtteranceDescription.PartType.Greeting, SpellPhoneticString(_currentCallsign)),
                new(UtteranceDescription.PartType.Text, "<phoneme alphabet='sapi' ph='h aa n m eh k k'>הנמך</phoneme>")
            };
            
            if (TossADice())
            {
                parts.Add(new (UtteranceDescription.PartType.Text, "לשמונה מאות"));
            }
            else
            {
                parts.AddRange(new UtteranceDescription.Part[] {
                    new(UtteranceDescription.PartType.Text, "לגובה"),
                    new(UtteranceDescription.PartType.Text, "<phoneme alphabet='sapi' ph='h a k a f a a'>הקפה</phoneme>"),
                });
            }

            return new UtteranceDescription(
                _culture,
                parts.ToArray()
            );
        }
        
        private string SpellPhoneticString(string phonetic)
        {
            var ssml = new StringBuilder();
            var separator = "-";

            for (int i = 0; i < phonetic.Length; i++)
            {
                var c = char.ToUpper(phonetic[i]);

                if (i > 0)
                {
                    if (char.IsDigit(c) && c == phonetic[i - 1])
                    {
                        separator = " ";
                    }
                    ssml.Append(separator);
                }

                if (char.IsDigit(c))
                {
                    ssml.Append(_phoneticDigitSsml[c - '0']);
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    ssml.Append(_phoneticLetterSsml[c]);
                }
                else
                {
                    ssml.Append(c);
                }
            }
            
            return ssml.ToString();
        }

        private string SpellWind()
        {
            var speedKt = _currentAtis.WindSpeedKt;
            var bearing = _currentAtis.WindBearing;
            var direction = SpellDirection(out var spelledNamedDirection);
            var calm = (speedKt <= 3);
            
            return calm && !spelledNamedDirection
                ? SpellSpeed()
                : $"{direction} {SpellSpeed()}";

            string SpellSpeed()
            {
                if (calm)
                {
                    return "קלה";
                }

                if (speedKt < 10)
                {
                    return _windKnotsSingleDigitWord[speedKt] + " " + "קשרים";
                }
                
                return SpellPhoneticString(speedKt.ToString()) + " " + "קשר";
            }
            
            string SpellDirection(out bool named)
            {
                var useNamedDirection = (_random.Next(33) % 2) == 0;
                if ((bearing >= 355 || bearing < 5) && useNamedDirection)
                {
                    named = true;
                    return "צפונית";
                }
                if ((bearing >= 85 && bearing <= 95) && useNamedDirection)
                {
                    named = true;
                    return "מזרחית";
                }
                if ((bearing >= 175 && bearing <= 185) && useNamedDirection)
                {
                    named = true;
                    return "דרומית";
                }
                if ((bearing >= 265 && bearing <= 275) && useNamedDirection)
                {
                    named = true;
                    return "מערבית";
                }

                named = false;
                return SpellPhoneticString(bearing.ToString().PadLeft(3, '0'));
            }
        }

        private bool TossADice()
        {
            return (_random.Next(33) % 2) == 0;
        }

        private readonly static IReadOnlyList<string> _phoneticDigitSsml = new[] {
            "אפס",
            "אחד",
            "שתיים",
            "שלוש",
            "ארבע",
            "חמש",
            "שש",
            "שבע", 
            "שמונה", 
            "תשע"
        };

        private readonly static IReadOnlyList<string> _windKnotsSingleDigitWord = new[] {
            "",
            "",
            "",
            "שלושה",
            "ארבעה",
            "חמישה",
            "שישה",
            "שבעה", 
            "שמונה", 
            "תשעה"
        };

        private readonly static IReadOnlyDictionary<char, string> _phoneticLetterSsml = new Dictionary<char, string>() {
            { 'A', "<phoneme alphabet='sapi' ph='aa aa l f a'>Alpha</phoneme>" },
            { 'B', "<phoneme alphabet='sapi' ph='b r aa aa v o'>Bravo</phoneme>" },
            { 'C', "<phoneme alphabet='sapi' ph='ch aa aa r l ih'>Charlie</phoneme>" },
            { 'D', "<phoneme alphabet='sapi' ph='d eh eh l t a'>Delta</phoneme>" },
            { 'E', "<phoneme alphabet='sapi' ph='eh eh k o'>Echo</phoneme>" },
            { 'F', "<phoneme alphabet='sapi' ph='f ao ao k s t r o t'>Foxtrot</phoneme>" },
            { 'G', "Golf" },
            { 'H', "Hotel" },
            { 'I', "<phoneme alphabet='sapi' ph='ih ih n d i a'>India</phoneme>" },
            { 'J', "Juliet" },
            { 'K', "<phoneme alphabet='sapi' ph='k ih ih l o'>Kilo</phoneme>" },
            { 'L', "<phoneme alphabet='sapi' ph='l ih ih m a'>Lima</phoneme>" },
            { 'M', "Mike" },
            { 'N', "November" },
            { 'O', "<phoneme alphabet='sapi' ph='ao ao s k a r'>Oscar</phoneme>" },
            { 'P', "<phoneme alphabet='sapi' ph='p aa p a'>Papa</phoneme>" },
            { 'Q', "<phoneme alphabet='sapi' ph='k eh b eh eh k'>Quebec</phoneme>" },
            { 'R', "<phoneme alphabet='sapi' ph='r o o m eh o'>Romeo</phoneme>" },
            { 'S', "<phoneme alphabet='sapi' ph='s i eh eh r a'>Sierra</phoneme>" },
            { 'T', "<phoneme alphabet='sapi' ph='t aa aa n g o'>Tango</phoneme>" },
            { 'U', "Uniform" },
            { 'V', "Victor" },
            { 'W', "Whiskey" },
            { 'X', "Xray" },
            { 'Y', "Yankee" },
            { 'Z', "Zulu" },
        };
    }
}