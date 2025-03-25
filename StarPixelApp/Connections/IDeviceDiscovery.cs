using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Connections
{
    public interface IDeviceDiscovery
    {
        Task<List<ConnectionDeviceInfo>> DiscoverDevicesAsync();
    }
}
