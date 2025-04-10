using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Connections
{
    public interface IDeviceConnection
    {
        Task<bool> ConnectAsync(string deviceId);
        Task DisconnectAsync();
        Task SendDataAsync(byte[] data);
        
        event Action<byte[]> DataReceived;
        bool IsConnected { get; }

        public long LastRXTimeUs { get; }
    }
}
