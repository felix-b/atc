using System;
using System.Diagnostics;
using OpenTK.Audio.OpenAL;

namespace Atc.Sound
{
    public unsafe class AudioContextScope : IDisposable
    {
        private readonly ISoundSystemLogger _logger;
        private readonly ALDevice _device; 
        private readonly ALContext _context;
        private bool _disposed = false;
        
        public AudioContextScope(ISoundSystemLogger logger)
        {
            _logger = logger;
            using var logSpan =_logger.InitializingSoundContext();
            
            var version = AL.Get(ALGetString.Version);
            var vendor = AL.Get(ALGetString.Vendor);
            var renderer = AL.Get(ALGetString.Renderer);
            _logger.OpenALInfo(version, vendor, renderer);

            var devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
            _logger.ListAlcDevices(deviceList: string.Join(", ", devices));

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
            _logger.DestroyingSoundContext();
            
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
    }
}