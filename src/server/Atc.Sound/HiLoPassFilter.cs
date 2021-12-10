using System;
using Atc.World.Abstractions;

namespace Atc.Sound
{
    //reference: https://stackoverflow.com/a/29561548/4544845
    public unsafe class HiLoPassFilter
    {
        /// <summary>
        /// rez amount, from sqrt(2) to ~ 0.1
        /// </summary>
        private float _resonance;
        private float _cutoffFrequency;
        private int _sampleRate;
        private PassType _passType;

        private float _c, _a1, _a2, _a3, _b1, _b2;

        /// <summary>
        /// Array of input values, latest are in front
        /// </summary>
        private float[] _inputHistory = new float[2];

        /// <summary>
        /// Array of output values, latest are in front
        /// </summary>
        private float[] _outputHistory = new float[3];

        public HiLoPassFilter(int sampleRate, PassType passType, float cutoffFrequency, float resonance)
        {
            _sampleRate = sampleRate;
            _passType = passType;
            _cutoffFrequency = cutoffFrequency;
            _resonance = resonance;

            switch (passType)
            {
                case PassType.Lowpass:
                    _c = 1.0f / (float)Math.Tan(Math.PI * cutoffFrequency / sampleRate);
                    _a1 = 1.0f / (1.0f + resonance * _c + _c * _c);
                    _a2 = 2f * _a1;
                    _a3 = _a1;
                    _b1 = 2.0f * (1.0f - _c * _c) * _a1;
                    _b2 = (1.0f - resonance * _c + _c * _c) * _a1;
                    break;
                case PassType.Highpass:
                    _c = (float)Math.Tan(Math.PI * cutoffFrequency / sampleRate);
                    _a1 = 1.0f / (1.0f + resonance * _c + _c * _c);
                    _a2 = -2f * _a1;
                    _a3 = _a1;
                    _b1 = 2.0f * (_c * _c - 1.0f) * _a1;
                    _b2 = (1.0f - resonance * _c + _c * _c) * _a1;
                    break;
            }
        }

        private void Update(float newInput)
        {
            float newOutput = 
                _a1 * newInput + 
                _a2 * _inputHistory[0] + 
                _a3 * _inputHistory[1] - 
                _b1 * _outputHistory[0] - 
                _b2 * _outputHistory[1];

            _inputHistory[1] = _inputHistory[0];
            _inputHistory[0] = newInput;

            _outputHistory[2] = _outputHistory[1];
            _outputHistory[1] = _outputHistory[0];
            _outputHistory[0] = newOutput;
        }

        private float getValue()
        {
            return this._outputHistory[0];
        }

        public static void TransformBuffer(byte[] buffer, SoundFormat format, PassType passType, float cutoffFrequency, float resonance)
        {
            var filter = new HiLoPassFilter(format.SamplesPerSecond, passType, cutoffFrequency, resonance);

            var bytesPerSample = format.BytesPerSample;
            float sampleIn;
            float sampleOut;

            for (int i = 0 ; i < buffer.Length ; i += bytesPerSample)
            { 
                if (bytesPerSample == 1)
                {
                    //TODO: this works really bad for 8-bit samples - find why
                    sampleIn = buffer[i];
                    filter.Update(sampleIn);
                    sampleOut = filter.getValue();
                    buffer[i] = (byte)sampleOut;
                }
                else // bytesPerSample == 2
                {
                    fixed (byte* p1 = buffer) {
                        Int16* p2 = (Int16*)(p1 + i);
                        sampleIn = *p2;
                        filter.Update(sampleIn);
                        sampleOut = filter.getValue();
                        *p2 = (Int16)sampleOut;
                    }
                }
            }
        }

        public enum PassType
        {
            Highpass,
            Lowpass,
        }
    }
}
