using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Connections
{
    public class BluetoothConnection : IDeviceConnection, IDeviceDiscovery
    {
        public bool IsConnected { get; private set; }
        public event Action<byte[]>? DataReceived;
        public long LastRXTimeUs { get; private set; }

        private string _deviceId;

        public async Task<bool> ConnectAsync(string deviceId)
        {
            Console.WriteLine("Connecting via Bluetooth...");
            _deviceId = deviceId;
            Console.WriteLine($"Подключение к Bluetooth-устройству {_deviceId}");
            await Task.Delay(500);
            IsConnected = true;
            return IsConnected;
        }

        public async Task DisconnectAsync()
        {
            Console.WriteLine("Disconnecting Bluetooth...");
            IsConnected = false;
        }

        public async Task SendDataAsync(byte[] data)
        {
            if (!IsConnected) throw new InvalidOperationException("Bluetooth not connected.");
            Console.WriteLine($"Sending via Bluetooth: {BitConverter.ToString(data)}");
        }

        public async Task<byte[]> ReceiveDataAsync()
        {
            if (!IsConnected) throw new InvalidOperationException("Bluetooth not connected.");
            return new byte[] { 0x01, 0x02 }; // Пример данных
        }

        // Теперь напрямую вызываем DataReceived, когда приходят данные
        private void SimulateDataReceiving()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(2000); // Симуляция получения данных
                    //string receivedData = $"DATA_FROM_{_deviceId}";
                    byte[] receivedData = { 0x00, 0x01};
                    Console.WriteLine($"Получены данные: {receivedData}");
                    DataReceived?.Invoke(receivedData); // Вызываем событие
                }
            });
        }

        public async Task<List<ConnectionDeviceInfo>> DiscoverDevicesAsync()
        {
            await Task.Delay(1000); // Имитация поиска
            return new List<ConnectionDeviceInfo>
            {
                new ConnectionDeviceInfo("Bluetooth Device 1", "00:1A:7D:DA:71:13"),
                new ConnectionDeviceInfo("Bluetooth Device 2", "00:1A:7D:DA:71:14")
            };
        }
    }
}
