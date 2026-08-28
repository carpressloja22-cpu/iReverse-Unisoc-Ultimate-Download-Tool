using System;
using System.Collections.Generic;
using System.Linq;

namespace iReverse_Unisoc_Ultimate.MyUI
{
    internal static class UsbDeviceCache
    {
        private static List<USBFastConnect.comInfo> _cachedDevices;
        private static DateTime _cacheTimestamp;
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(2);
        private static readonly object _lock = new object();

        public static List<USBFastConnect.comInfo> GetDevices()
        {
            lock (_lock)
            {
                if (_cachedDevices != null &&
                    DateTime.Now - _cacheTimestamp < CacheLifetime)
                {
                    return _cachedDevices;
                }

                _cachedDevices = USBFastConnect.listDevices.ToList();
                _cacheTimestamp = DateTime.Now;
                return _cachedDevices;
            }
        }

        public static void Invalidate()
        {
            lock (_lock)
            {
                _cachedDevices = null;
                _cacheTimestamp = DateTime.MinValue;
            }
        }

        public static bool IsFresh()
        {
            lock (_lock)
            {
                return _cachedDevices != null &&
                       DateTime.Now - _cacheTimestamp < CacheLifetime;
            }
        }

        public static void Refresh()
        {
            lock (_lock)
            {
                _cachedDevices = USBFastConnect.listDevices.ToList();
                _cacheTimestamp = DateTime.Now;
            }
        }
    }
}
