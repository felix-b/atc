#if false

using Atc.Sound.OpenAL;

namespace Atc.Telemetry.Impl;

public static class AudioContextScope_Telemetry
{
    public class Noop : AudioContextScope.IMyTelemetry
    {
        public ITraceSpan InitializingSoundContext()
        {
            return new NoopTraceSpan();
        }

        public void InfoOpenALInit(string version, string vendor, string renderer)
        {
        }

        public void VerboseListAlcDevices(string deviceList)
        {
        }

        public void VerboseDestroyingSoundContext()
        {
        }

    }
}

#endif