using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Connections
{
    public class VirtualConnection : IDeviceConnection, IDeviceDiscovery
    {
        private readonly List<byte> _receivedData = new();
        public bool IsConnected { get; private set; }
        public event Action<byte[]>? DataReceived;
        private string _deviceId;

        public async Task<bool> ConnectAsync(string deviceId)
        {
            Console.WriteLine("Connecting to Virtual Device...");
            
            _deviceId = deviceId;
            Console.WriteLine($"Подключение к Bluetooth-устройству {_deviceId}");
            await Task.Delay(500);
            IsConnected = true;
            return IsConnected;
        }

        public async Task DisconnectAsync()
        {
            Console.WriteLine("Disconnecting Virtual Device...");
            await Task.Delay(200); // Имитация задержки отключения
            IsConnected = false;
        }

        public async Task SendDataAsync(byte[] data)
        {
            if (!IsConnected) throw new InvalidOperationException("Virtual device not connected.");
            Console.WriteLine($"[Virtual] Sending Data: {BitConverter.ToString(data)}");

            // Добавляем в список, имитируя получение ответа от устройства
            _receivedData.AddRange(data);
            await Task.Delay(100); // Имитация обработки данных
        }

        private void SimulateDataReceiving()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(2000); // Симуляция получения данных
                    //string receivedData = $"DATA_FROM_{_deviceId}";
                    byte[] receivedData = { 0x00, 0x01 };
                    Console.WriteLine($"Получены данные: {receivedData}");
                    DataReceived?.Invoke(receivedData); // Вызываем событие
                }
            });
        }

        /*
        public async Task<byte[]> ReceiveDataAsync()
        {
            if (!IsConnected) throw new InvalidOperationException("Virtual device not connected.");

            // Возвращаем сохранённые данные или фиктивные байты
            var responseData = _receivedData.Count > 0 ? _receivedData.ToArray() : new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            _receivedData.Clear(); // Очищаем буфер после чтения

            Console.WriteLine($"[Virtual] Receiving Data: {BitConverter.ToString(responseData)}");
            await Task.Delay(100); // Имитация обработки данных

            return responseData;
        }
        */
        /*
        private void OnDataReceived(byte[] data)
        {
            DataReceived?.Invoke(data);
        }
        */
        public async Task<List<ConnectionDeviceInfo>> DiscoverDevicesAsync()
        {
            await Task.Delay(500);
            return new List<ConnectionDeviceInfo>
        {
            new ConnectionDeviceInfo("Virtual Device A", "VIRTUAL_1"),
            new ConnectionDeviceInfo("Virtual Device B", "VIRTUAL_2")
        };
        }
    }

}
