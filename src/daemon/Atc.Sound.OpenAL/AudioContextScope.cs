using Atc.Telemetry;
using OpenTK.Audio.OpenAL;

namespace Atc.Sound.OpenAL;

public unsafe class AudioContextScope : IDisposable
{
    private readonly IThisTelemetry _telemetry;
    private readonly ALDevice _device; 
    private readonly ALContext _context;
    private bool _disposed = false;
        
    public AudioContextScope(IThisTelemetry telemetry)
    {
        _telemetry = telemetry;
        using var logSpan =_telemetry.InitializingSoundContext();
            
        var version = AL.Get(ALGetString.Version);
        var vendor = AL.Get(ALGetString.Vendor);
        var renderer = AL.Get(ALGetString.Renderer);
        _telemetry.InfoOpenALInit(version, vendor, renderer);

        var devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
        _telemetry.VerboseListAlcDevices(deviceList: string.Join(", ", devices));

        _device = ALC.OpenDevice(null);
        _context = ALC.CreateContext(_device, (int*)null);

        ALC.MakeContextCurrent(_context);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _telemetry.VerboseDestroyingSoundContext();
            
        if (_context != ALContext.Null) 
        {
            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(_context);
        }

        if (_device != IntPtr.Zero) 
        {
            ALC.CloseDevice(_device);
        }
    }
    
    public interface IThisTelemetry : ITelemetry
    {
        ITraceSpan InitializingSoundContext();
        void InfoOpenALInit(string version, string vendor, string renderer);
        void VerboseListAlcDevices(string deviceList);
        void VerboseDestroyingSoundContext();
    }
}
