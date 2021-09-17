using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Atc.Sound;
using Atc.Speech.Abstractions;

namespace Atc.Server
{
    public class TempMockLlhzRadio
    {
        private readonly ISpeechSynthesisPlugin _synthesizer;
        private readonly RadioSpeechPlayer _player;
        private readonly SoundFormat _soundFormat;
        private readonly Random _random = new Random(DateTime.Now.TimeOfDay.Milliseconds);
        private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("he-IL");
        private Task? _currentWorkflow;
        private CancellationTokenSource? _workflowCancellation;
        private TaskCompletionSource? _pilotTransmissionReceived;
        private TaskCompletionSource? _workflowCancellationRequested;

        public TempMockLlhzRadio(ISpeechSynthesisPlugin synthesizer, RadioSpeechPlayer player)
        {
            _synthesizer = synthesizer;
            _player = player;
            _soundFormat = new SoundFormat(bitsPerSample: 16, samplesPerSecond: 11025, channelCount: 1);
            
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

            await NextPilotTransmission(cancel);                        // CLR hello
            await TransmitLo(CreateClearanceGoAheadUtterance(), cancel);  // go ahead
            await NextPilotTransmission(cancel);                        // request start
            await TransmitLo(CreateStartApprovalUtterance(), cancel);     // start approved
            await NextPilotTransmission(cancel);                        // readback
            await TransmitLo(CreateClearanceHandoffUtterance(), cancel);  // hand off to tower
            await NextPilotTransmission(cancel);                        // readback
            
            await NextPilotTransmission(cancel);                        // TWR request taxi
            await TransmitHi(CreateTaxiClearanceUtterance(), cancel);     // taxi clearance
            await NextPilotTransmission(cancel);                        // readback
            
            await NextPilotTransmission(cancel);                        // TWR ready for departure

            int takeoffRandom;
            
            takeoffRandom = _random.Next(3);
            if (takeoffRandom == 2)
            {
                await TransmitHi(CreateHoldShortUtterance(), cancel);     // hold short
                await NextPilotTransmission(cancel);                    // readback
                await Task.Delay(_random.Next(10000, 60000));
                takeoffRandom = _random.Next(2);
            }
            
            if (takeoffRandom == 1)
            {
                await TransmitHi(CreateLineUpAndWaitUtterance(), cancel); // LUAW
                await NextPilotTransmission(cancel);                    // readback
                await Task.Delay(_random.Next(10000, 60000));
            }
            
            await TransmitHi(CreateTakeoffClearanceUtterance(), cancel);  // cleared for takeoff
            await NextPilotTransmission(cancel);                        // readback
            
            await NextPilotTransmission(cancel);                        // report downwind
            await TransmitHi(CreatePatternPositionUtterance(), cancel);   // pattern position
            await NextPilotTransmission(cancel);                        // readback

            await NextPilotTransmission(cancel);                        // report final
            await TransmitHi(CreateLandingClearanceUtterance(), cancel);  // clear to land
            await NextPilotTransmission(cancel);                        // readback
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

        private UtteranceDescription CreateClearanceGoAheadUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, "Charlie Delta Charlie"),
                    new (UtteranceDescription.PartType.Affirmation, "שלום, המשך"),
                }
            );
        }

        private UtteranceDescription CreateStartApprovalUtterance()
        {
            var qnh = _random.Next(2980, 3000).ToString();
            
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, "Charlie Delta Charlie"),
                    new (UtteranceDescription.PartType.Affirmation, "ההתנעה מאושרת"),
                    new (UtteranceDescription.PartType.Text, "מסלול בשימוש"),
                    new (UtteranceDescription.PartType.Data, "שתיים תשע"),
                    new (UtteranceDescription.PartType.Text, "הלחץ"),
                    new (UtteranceDescription.PartType.Data, $"{qnh[0]}-{qnh[1]}-{qnh[2]}-{qnh[3]}"),
                    new (UtteranceDescription.PartType.Data, "בצרה 800"),
                }
            );
        }

        private UtteranceDescription CreateClearanceHandoffUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, "Charlie Delta Charlie"),
                    new (UtteranceDescription.PartType.Text, "לאחר התנעה עבור למגדל"),
                    new (UtteranceDescription.PartType.Data, "אחד-שתיים-שתיים שתיים,"),
                    new (UtteranceDescription.PartType.Farewell, "להישמע."),
                }
            );
        }

        private UtteranceDescription CreateTaxiClearanceUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, "Charlie Delta Charlie"),
                    new (UtteranceDescription.PartType.Affirmation, "רשאי להסיע"),
                    new (UtteranceDescription.PartType.Text, "מסלול בשימוש"),
                    new (UtteranceDescription.PartType.Data, "שתיים תשע"),
                }
            );
        }

        private UtteranceDescription CreateHoldShortUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, "Charlie Delta Charlie"),
                    new (UtteranceDescription.PartType.Negation, ", המתן."),
                }
            );
        }

        private UtteranceDescription CreateLineUpAndWaitUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, "Charlie Delta Charlie"),
                    new (UtteranceDescription.PartType.Text, "מסלול שתיים תשע"),
                    new (UtteranceDescription.PartType.Data, "תתיישר בלבד"),
                }
            );
        }

        private UtteranceDescription CreateTakeoffClearanceUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, "Charlie Delta Charlie"),
                    new (UtteranceDescription.PartType.Data, "רוח קלה"),
                    new (UtteranceDescription.PartType.Data, "מסלול שתיים תשע"),
                    new (UtteranceDescription.PartType.Affirmation, "רשאי להמריא"),
                }
            );
        }

        private UtteranceDescription CreatePatternPositionUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, "Charlie Delta Charlie"),
                    new (UtteranceDescription.PartType.Text, "מספר"),
                    new (UtteranceDescription.PartType.Data, "שתיים"),
                }
            );
        }

        private UtteranceDescription CreateLandingClearanceUtterance()
        {
            return new UtteranceDescription(
                _culture,
                new UtteranceDescription.Part[] {
                    new (UtteranceDescription.PartType.Greeting, "Charlie Delta Charlie"),
                    new (UtteranceDescription.PartType.Affirmation, "הרוח קלה"),
                    new (UtteranceDescription.PartType.Text, "מסלול"),
                    new (UtteranceDescription.PartType.Data, "שתיים תשע"),
                    new (UtteranceDescription.PartType.Data, "רשאי לנחות"),
                }
            );
        }
    }
}