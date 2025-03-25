using StarPixelApp.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarPixelApp.Services
{
    public class DeviceDiscoveryService
    {
        private readonly IDeviceDiscovery _deviceDiscovery;

        public DeviceDiscoveryService(IDeviceDiscovery deviceDiscovery)
        {
            _deviceDiscovery = deviceDiscovery;
        }

        public async Task<List<ConnectionDeviceInfo>> DiscoverDevicesAsync()
        {
            return await _deviceDiscovery.DiscoverDevicesAsync().ConfigureAwait(false); 
        }
    }
}
