using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Arguments;

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
    }
    
}
