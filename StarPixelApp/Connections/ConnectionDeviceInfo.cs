using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Connections
{
    public class ConnectionDeviceInfo
    {
        public string Name { get; set; }
        public string Address { get; set; } // MAC-адрес для Bluetooth, COM-порт для USB и т. д.

        public ConnectionDeviceInfo(string name, string address)
        {
            Name = name;
            Address = address;
        }

        public override string ToString() => $"{Name} ({Address})";
    }
}
