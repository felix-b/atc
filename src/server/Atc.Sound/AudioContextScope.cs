using System;
using System.Diagnostics;
using OpenTK.Audio.OpenAL;

namespace Atc.Sound
{
    public unsafe class AudioContextScope : IDisposable
    {
        private readonly ALDevice _device; 
        private readonly ALContext _context;
        private bool _disposed = false;
        
        public AudioContextScope()
        {
            var version = AL.Get(ALGetString.Version);
            var vendor = AL.Get(ALGetString.Vendor);
            var renderer = AL.Get(ALGetString.Renderer);
            Console.WriteLine($"OpenAL: version[{version}] vendor[{vendor}] renderer[{renderer}] pid[{Process.GetCurrentProcess().Id}");

            var devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
            Console.WriteLine($"ALC Devices: {string.Join(", ", devices)}");

            _device = ALC.OpenDevice(null);
            _context = ALC.CreateContext(_device, (int*)null);

            ALC.MakeContextCurrent(_context);
            Console.WriteLine($"OpenAL: initialized context");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Console.WriteLine($"OpenAL: deleting context");
            
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