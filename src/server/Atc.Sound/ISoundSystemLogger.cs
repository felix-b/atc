using Zero.Doubt.Logging;

namespace Atc.Sound
{
    public interface ISoundSystemLogger
    {
        LogWriter.LogSpan InitializingSoundContext();
        void OpenALInfo(string version, string vendor, string renderer);
        void ListAlcDevices(string deviceList);
        void DestroyingSoundContext();
    }
}
