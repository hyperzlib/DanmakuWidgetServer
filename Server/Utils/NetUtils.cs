using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DanmakuWidgetServer.Server.Utils
{
    internal static class NetUtils
    {
        public static string[] GetAllIPv4Addresses()
        {
            var addresses = new List<string>();

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                var ipProperties = networkInterface.GetIPProperties();
                foreach (var unicastAddress in ipProperties.UnicastAddresses)
                {
                    if (unicastAddress?.Address == null)
                    {
                        continue;
                    }

                    if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        addresses.Add(unicastAddress.Address.ToString());
                    }
                }
            }

            return addresses.Distinct().ToArray();
        }

        public static string[] SortByLocalAddressPriority(IEnumerable<string> addresses)
        {
            if (addresses == null)
            {
                return new string[0];
            }

            return addresses
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .Select(x =>
                {
                    if (TryGetIPv4Bytes(x, out var bytes))
                    {
                        return new
                        {
                            Address = x,
                            Priority = GetPriority(bytes),
                            B0 = (int)bytes[0],
                            B1 = (int)bytes[1],
                            B2 = (int)bytes[2],
                            B3 = (int)bytes[3],
                        };
                    }

                    return new
                    {
                        Address = x,
                        Priority = 4,
                        B0 = 999,
                        B1 = 999,
                        B2 = 999,
                        B3 = 999,
                    };
                })
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Priority == 1 ? x.B2 : x.Priority == 2 ? x.B1 : x.Priority == 3 ? x.B2 : x.B0)
                .ThenBy(x => x.Priority == 1 ? x.B3 : x.Priority == 2 ? x.B2 : x.Priority == 3 ? x.B3 : x.B1)
                .ThenBy(x => x.Priority == 2 ? x.B3 : x.B2)
                .ThenBy(x => x.B3)
                .ThenBy(x => x.Address)
                .Select(x => x.Address)
                .ToArray();
        }

        private static int GetPriority(byte[] bytes)
        {
            if (bytes[0] == 127 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 1)
            {
                return 0;
            }

            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return 1;
            }

            if (bytes[0] == 10)
            {
                return 2;
            }

            if (bytes[0] == 172 && bytes[1] == 17)
            {
                return 3;
            }

            return 4;
        }

        private static bool TryGetIPv4Bytes(string address, out byte[] bytes)
        {
            bytes = null;
            if (!IPAddress.TryParse(address, out var ipAddress))
            {
                return false;
            }

            if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            bytes = ipAddress.GetAddressBytes();
            return bytes.Length == 4;
        }
    }
}
