using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
//using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.IO.Ports;

namespace StarPixelApp.Connections
{
    public class Win32SerialConnection : IDeviceConnection, IDeviceDiscovery, IDisposable
    {
        public bool IsConnected { get; private set; }
        public event Action<byte[]>? DataReceived;

        public long LastRXTimeUs { get; private set; }

        private IntPtr _handle = IntPtr.Zero;
        private CancellationTokenSource? _cts;
        private Task? _readTask;
        private readonly byte[] _readBuffer = new byte[1024];

        public async Task<bool> ConnectAsync(string portName)
        {
            string fullPortName = @"\\.\" + portName;
            _handle = CreateFile(fullPortName, GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);

            if (_handle == INVALID_HANDLE_VALUE)
                return false;

            DCB dcb = new DCB();
            if (!GetCommState(_handle, ref dcb))
                return false;

            dcb.BaudRate = 500000;
            dcb.ByteSize = 8;
            dcb.Parity = 0;
            dcb.StopBits = 0;

            if (!SetCommState(_handle, ref dcb))
                return false;

            COMMTIMEOUTS timeouts = new COMMTIMEOUTS
            {
                ReadIntervalTimeout = 0xffffffff,
                ReadTotalTimeoutConstant = 0,
                ReadTotalTimeoutMultiplier = 0,
                WriteTotalTimeoutConstant = 0,
                WriteTotalTimeoutMultiplier = 0
            };

            SetCommTimeouts(_handle, ref timeouts);

            _cts = new CancellationTokenSource();
            _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));
            IsConnected = true;
            return true;
        }

        public async Task DisconnectAsync()
        {
            if (IsConnected)
            {
                _cts?.Cancel();
                if (_readTask != null) await _readTask;
                CloseHandle(_handle);
                IsConnected = false;
            }
        }

        public async Task SendDataAsync(byte[] data)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected.");

            OVERLAPPED overlapped = new OVERLAPPED();
            IntPtr overlappedPtr = Marshal.AllocHGlobal(Marshal.SizeOf(overlapped));
            Marshal.StructureToPtr(overlapped, overlappedPtr, false);

            uint bytesWritten;
            bool result = WriteFile(_handle, data, (uint)data.Length, out bytesWritten, overlappedPtr);
            Marshal.FreeHGlobal(overlappedPtr);

            if (!result)
            {
                // handle errors
            }
        }

        private async Task ReadLoopAsync(CancellationToken token)
        {
            OVERLAPPED overlapped = new OVERLAPPED();
            IntPtr overlappedPtr = Marshal.AllocHGlobal(Marshal.SizeOf(overlapped));
            Marshal.StructureToPtr(overlapped, overlappedPtr, false);

            while (!token.IsCancellationRequested)
            {
                uint bytesRead;
                bool success = ReadFile(_handle, _readBuffer, (uint)_readBuffer.Length, out bytesRead, overlappedPtr);
                if (success && bytesRead > 0)
                {
                    byte[] received = new byte[bytesRead];
                    Array.Copy(_readBuffer, received, bytesRead);
                    LastRXTimeUs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
                    DataReceived?.Invoke(received);
                }
                await Task.Delay(1, token); // минимальная пауза
            }

            Marshal.FreeHGlobal(overlappedPtr);
        }

        public async Task<List<ConnectionDeviceInfo>> DiscoverDevicesAsync()
        {
            var devices = new List<ConnectionDeviceInfo>();
            foreach (var port in SerialPort.GetPortNames())
            {
                devices.Add(new ConnectionDeviceInfo($"COM Port: {port}", port));
            }
            return devices;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            if (_readTask != null) _readTask.Wait();
            if (_handle != IntPtr.Zero) CloseHandle(_handle);
            _cts?.Dispose();
        }

        #region Win32 API

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential)]
        private struct DCB
        {
            public uint DCBlength;
            public uint BaudRate;
            public uint Flags;
            public ushort wReserved;
            public ushort XonLim;
            public ushort XoffLim;
            public byte ByteSize;
            public byte Parity;
            public byte StopBits;
            public char XonChar;
            public char XoffChar;
            public char ErrorChar;
            public char EofChar;
            public char EvtChar;
            public ushort wReserved1;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COMMTIMEOUTS
        {
            public uint ReadIntervalTimeout;
            public uint ReadTotalTimeoutMultiplier;
            public uint ReadTotalTimeoutConstant;
            public uint WriteTotalTimeoutMultiplier;
            public uint WriteTotalTimeoutConstant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct OVERLAPPED
        {
            public IntPtr Internal;
            public IntPtr InternalHigh;
            public uint Offset;
            public uint OffsetHigh;
            public IntPtr hEvent;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName, uint dwDesiredAccess,
            uint dwShareMode, IntPtr lpSecurityAttributes,
            uint dwCreationDisposition, uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll")]
        private static extern bool GetCommState(IntPtr hFile, ref DCB lpDCB);

        [DllImport("kernel32.dll")]
        private static extern bool SetCommState(IntPtr hFile, [In] ref DCB lpDCB);

        [DllImport("kernel32.dll")]
        private static extern bool SetCommTimeouts(IntPtr hFile, [In] ref COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(
            IntPtr hFile, [Out] byte[] lpBuffer,
            uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(
            IntPtr hFile, byte[] lpBuffer,
            uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        #endregion
    }

}