using StarPixelApp.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Services
{
    public class DeviceService
    {
        private readonly IDeviceConnection _connection;
        private readonly string _deviceId;

        public DeviceService(string connectionType, string deviceId)
        {
            _connection = ConnectionFactory.CreateConnection(connectionType);
            _deviceId = deviceId;

            _connection.DataReceived += OnDataReceived;
        }

        public async Task<bool> ConnectAsync()
        {
            //return await _connection.ConnectAsync();
            return await _connection.ConnectAsync(_deviceId);
        }

        public async Task DisconnectAsync()
        {
            await _connection.DisconnectAsync();
        }

        public async Task SendDataAsync(byte[] data)
        {
            await _connection.SendDataAsync(data);
        }
        /*
        public async Task<byte[]> ReceiveDataAsync()
        {
            return await _connection.ReceiveDataAsync();
        }
        */
        private void OnDataReceived(byte[] data)
        {
            AsyncEventBus.Publish("deviceDataReceived", data);
        }
        public bool IsConnected => _connection.IsConnected;
    }
}
