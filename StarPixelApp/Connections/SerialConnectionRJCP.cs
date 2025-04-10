using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
//using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RJCP.IO.Ports;

namespace StarPixelApp.Connections
{

    public class SerialConnectionRJCP : IDeviceConnection, IDeviceDiscovery
    {
        public bool IsConnected { get; private set; }
        public event Action<byte[]>? DataReceived;

        public long LastRXTimeUs { get; private set; }

        private SerialPortStream _serialPort;

        private CancellationTokenSource? _cts;

        private readonly byte[] _buffer = new byte[1024]; // Фиксированный буфер
        private int _bufferIndex = 0;

        public async Task<bool> ConnectAsync(string portName)
        {
            _serialPort = new SerialPortStream(portName, 500000, 8, Parity.None, StopBits.One)
            {
                DtrEnable = false,
                RtsEnable = false,
                Handshake = Handshake.None,
                ReadBufferSize = 124000,
                WriteBufferSize = 1,
                ReadTimeout = 1,
                WriteTimeout = 1
            };


            try
            {
                _serialPort.Open();
                IsConnected = true;

                _cts = new CancellationTokenSource();
                _ = Task.Run(() => ReadLoopAsync(_cts.Token)); // запустить чтение

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening serial port: {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            //Console.WriteLine("Disconnecting COM port...");
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _cts?.Cancel();
                _serialPort.Close();
            }
            IsConnected = false;
        }

        // Создаем объект Stopwatch для измерения времени рендера
        Stopwatch stopwatcSerial = new Stopwatch();
        /*
        public async Task SendDataAsync(byte[] data)
        {
            if (!IsConnected) throw new InvalidOperationException("COM port not connected.");
            stopwatcSerial.Restart();
            _serialPort.Write(data, 0, data.Length);

            stopwatcSerial.Stop();
            long microseconds = stopwatcSerial.ElapsedTicks * 1000000 / Stopwatch.Frequency;
            microseconds = 0;
            //Console.WriteLine($"Sending via COM port: {BitConverter.ToString(data)}");
        }
        */
        public async Task SendDataAsync(byte[] data)
        {
            if (!IsConnected || !_serialPort.IsOpen)
                throw new InvalidOperationException("COM port not connected.");

            //var stopwatch = Stopwatch.StartNew();
            _serialPort.Write(data, 0, data.Length);
            _serialPort.Flush(); // Убедиться, что всё ушло
            //stopwatch.Stop();

            //long microseconds = stopwatch.ElapsedTicks * 1_000_000 / Stopwatch.Frequency;
            //Console.WriteLine($"Send took: {microseconds} µs");
        }

        //byte[] nPacket = new byte[1]; 

        private async Task ReadLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _serialPort.IsOpen)
                {
                    if (_serialPort.BytesToRead > 0)
                    {
                        //SendDataAsync(nPacket);
                        //nPacket[0]++;

                        long timestamp = Stopwatch.GetTimestamp();
                        int bytesToRead = Math.Min(_serialPort.BytesToRead, _buffer.Length - _bufferIndex);
                        int readBytes = await Task.Run(() => _serialPort.Read(_buffer, _bufferIndex, bytesToRead));
                        _bufferIndex += readBytes;

                        // Переводим в микросекунды
                        LastRXTimeUs = timestamp * 1_000_000 / Stopwatch.Frequency;

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
                        //await Task.Delay(5, token); // Уменьшаем нагрузку на процессор
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //Console.WriteLine("Serial read task cancelled.");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Serial read error: {ex.Message}");
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
            string[] portNames = System.IO.Ports.SerialPort.GetPortNames();

            foreach (var portName in portNames)
            {
                devices.Add(new ConnectionDeviceInfo($"COM Port: {portName}", portName));
            }

            return devices;
        }
    }
}