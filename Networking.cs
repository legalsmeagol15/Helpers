using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Arguments;
using System.Globalization;

namespace Helpers
{
    /// <summary>Networking helpers.</summary>
    public static class Networking
    {
        /// <summary>Returns the local IP address of this system.</summary>
        /// <remarks>Posted https://stackoverflow.com/questions/6803073/get-local-ip-address </remarks>
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        /// <summary>
        /// Returns an IPAddress-and-port combo for both ipv4 or ipv6.
        /// </summary>
        ///<remarks>From https://stackoverflow.com/questions/2727609/best-way-to-create-ipendpoint-from-string </remarks>
        public static IPEndPoint ParseIPEndPoint(string str)
        {
            string[] split = str.Split(':');
            IPAddress result = null;
            int port = 0;
            if (split.Length < 2)
                throw new FormatException("Invalid endpoint format: " + str.Substring(0, 50));
            else if (split.Length > 2)
            {
                // This must be ipv6
                // 01:02:03:04:05:06 would mean the IPAddress is 01:02:03:04:05, and the port is 06
                // It has become common to represent [01:02:03:04:05]:06 the same way, watch out for this.
                split[0] = split[0].TrimStart('[');
                split[split.Length - 2] = split[split.Length - 2].TrimEnd(']');
                string rejoined = string.Join(":", split, 0, split.Length - 1);

                if (!IPAddress.TryParse(rejoined, out result))
                {
                    throw new FormatException("Invalid ipv6 address: " + str.Substring(0, 50));
                }
            }
            else if (!IPAddress.TryParse(split[0], out result))
                // This must be ipv4
                throw new FormatException("Invalid ipv4 address: " + str.Substring(0, 50));
            else if (!int.TryParse(split[split.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
                throw new FormatException("Invalid port string: " + split[split.Length - 1].Substring(0, 50));
            return new IPEndPoint(result, port);
        }
    }

}
