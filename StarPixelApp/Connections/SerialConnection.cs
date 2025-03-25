using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Connections
{
    public class SerialConnection : IDeviceConnection, IDeviceDiscovery
    {
        public bool IsConnected { get; private set; }
        public event Action<byte[]>? DataReceived;

        private SerialPort _serialPort;

        //private Thread? _readThread;
        private CancellationTokenSource? _cts;

        private readonly byte[] _buffer = new byte[1024]; // Фиксированный буфер
        private int _bufferIndex = 0;

        public async Task<bool> ConnectAsync(string portName)
        {
            //Console.WriteLine($"Connecting to COM port {portName}...");
            //_serialPort = new SerialPort(portName, 500000, Parity.None, 8, StopBits.One);

            _serialPort = new SerialPort(portName, 500000, Parity.None, 8, StopBits.One)
            {
                ReadBufferSize = 124000, // Увеличение буфера чтения
                ReceivedBytesThreshold = 1, // Событие вызывается даже при 1 байте
                Handshake = Handshake.None,
                DtrEnable = true,
                RtsEnable = true
            };


            _serialPort.Open();
            IsConnected = true;

            // Запускаем асинхронное чтение
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ReadLoopAsync(_cts.Token));

            return IsConnected;
        }

        public async Task DisconnectAsync()
        {
            Console.WriteLine("Disconnecting COM port...");
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _cts?.Cancel();
                _serialPort.Close();
            }
            IsConnected = false;
        }

        public async Task SendDataAsync(byte[] data)
        {
            if (!IsConnected) throw new InvalidOperationException("COM port not connected.");
            _serialPort.Write(data, 0, data.Length);
            Console.WriteLine($"Sending via COM port: {BitConverter.ToString(data)}");
        }

        private async Task ReadLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_serialPort.BytesToRead > 0)
                    {
                        int bytesToRead = Math.Min(_serialPort.BytesToRead, _buffer.Length - _bufferIndex);
                        int readBytes = await Task.Run(() => _serialPort.Read(_buffer, _bufferIndex, bytesToRead));
                        _bufferIndex += readBytes;

                        // Проверяем, достигли ли конца пакета (например, символ '\n')
                        //if (Array.IndexOf(_buffer, (byte)'\n', 0, _bufferIndex) >= 0)
                        {
                            byte[] packet = new byte[_bufferIndex];
                            Array.Copy(_buffer, packet, _bufferIndex);
                            DataReceived?.Invoke(packet);
                            _bufferIndex = 0; // Обнуляем буфер
                        }
                    }
                    //else
                    {
                        await Task.Delay(5, token); // Уменьшаем нагрузку на процессор
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Serial read task cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serial read error: {ex.Message}");
            }
        }

        int bytesToRead;
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Reset(); // Сбрасываем счетчик
            //stopwatch.Start();
/*
            int bytesToRead = _serialPort.BytesToRead;
            byte[] buffer = new byte[bytesToRead];
            _serialPort.Read(buffer, 0, bytesToRead);
            DataReceived?.Invoke(buffer);
*/
            //bytesToRead = 0;


            //Debug.WriteLine($"Core = Data Received: {bytesToRead}");

            //stopwatch.Stop();
            //long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            //EventBus.Publish("labelTime", elapsedMilliseconds.ToString());
        }

        public async Task<List<ConnectionDeviceInfo>> DiscoverDevicesAsync()
        {
            var devices = new List<ConnectionDeviceInfo>();
            string[] portNames = SerialPort.GetPortNames();

            foreach (var portName in portNames)
            {
                devices.Add(new ConnectionDeviceInfo($"COM Port: {portName}", portName));
            }

            return devices;
        }
    }
}