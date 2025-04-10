using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if ANDROID
using Android.Content;
using Android.Hardware.Usb;
using Microsoft.Maui.ApplicationModel;
#elif WINDOWS
using Windows.Devices.Enumeration;
#endif

namespace StarPixelApp.Connections
{
    public class UsbConnection : IDeviceConnection, IDeviceDiscovery
    {
        public bool IsConnected { get; private set; }
        public event Action<byte[]>? DataReceived;

        public long LastRXTimeUs { get; private set; }

        private string _deviceId;

        public async Task<bool> ConnectAsync(string deviceId)
        {
            Console.WriteLine("Connecting via USB...");
            _deviceId = deviceId;
            Console.WriteLine($"Подключение к Bluetooth-устройству {_deviceId}");
            await Task.Delay(500);
            IsConnected = true;
            return IsConnected;
        }

        public async Task DisconnectAsync()
        {
            Console.WriteLine("Disconnecting USB...");
            IsConnected = false;
        }

        public async Task SendDataAsync(byte[] data)
        {
            if (!IsConnected) throw new InvalidOperationException("USB not connected.");
            Console.WriteLine($"Sending via USB: {BitConverter.ToString(data)}");
        }
        /*
        public async Task<byte[]> ReceiveDataAsync()
        {
            if (!IsConnected) throw new InvalidOperationException("USB not connected.");
            return new byte[] { 0x03, 0x04 }; // Пример данных
        }
        */
        /*
        private void OnDataReceived(byte[] data)
        {
            DataReceived?.Invoke(data);
        }
        */
        // Теперь напрямую вызываем DataReceived, когда приходят данные
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
        public async Task<List<ConnectionDeviceInfo>> DiscoverDevicesAsync()
        {
            var devices = new List<ConnectionDeviceInfo>();
            /*
            await Task.Delay(1000);
            return new List<ConnectionDeviceInfo>
        {
            new ConnectionDeviceInfo("USB Device 1", "COM3"),
            new ConnectionDeviceInfo("USB Device 2", "COM4")
        };
            */

#if ANDROID
            var usbManager = (UsbManager)Platform.CurrentActivity.GetSystemService(Context.UsbService);
            var deviceList = usbManager.DeviceList.Values;

            foreach (var device in deviceList)
            {
                if (usbManager.HasPermission(device)) // Только устройства с разрешением
                {
                    string deviceInfo = $"VendorId: {device.VendorId}, ProductId: {device.ProductId}";
                    devices.Add(new ConnectionDeviceInfo(device.DeviceName, deviceInfo));
                }
            }

#elif WINDOWS
            string selector = "System.Devices.InterfaceClassGuid:=\"{A5DCBF10-6530-11D2-901F-00C04FB951ED}\""; // USB-устройства
            var usbDevices = await DeviceInformation.FindAllAsync(selector);

            foreach (var device in usbDevices)
            {
                if (device.IsEnabled) // Фильтрация только включенных USB-устройств
                {
                    devices.Add(new ConnectionDeviceInfo(device.Name, device.Id));
                }
            }
#endif

            return devices;
        }
    }
}
