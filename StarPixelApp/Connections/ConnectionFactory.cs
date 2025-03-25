﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Connections
{
    public static class ConnectionFactory
    {
        public static IDeviceConnection CreateConnection(string type)
        {
            return type switch
            {
                "Bluetooth" => new BluetoothConnection(),
                "USB" => new UsbConnection(),
                "Virtual" => new VirtualConnection(),
                "Serial" => new SerialConnection(),
                _ => throw new NotSupportedException($"Connection type {type} is not supported")
            };
        }
    }
}
