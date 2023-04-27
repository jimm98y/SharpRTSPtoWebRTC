using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace CameraAPI
{
    public class OnvifClient
    {
        private class UdpState
        {
            public UdpClient Client { get; set; }
            public IPEndPoint Endpoint { get; set; }
            public IList<string> Result { get; set; }
        }

        public const int ONVIF_BROADCAST_TIMEOUT = 10000;
        private const int ONVIF_BROADCAST_PORT = 54567;
        public const string WS_DISCOVERY_ADDRESS_IPv4 = "239.255.255.250";
        public const int WS_DISCOVERY_PORT = 3702;

        private static SemaphoreSlim _discoverySlim = new SemaphoreSlim(1);

        /// <summary>
        /// Discover ONVIF devices in the local network.
        /// </summary>
        /// <param name="ipAddress">IP address of the network interface to use (IP of the host computer).</param>
        /// <param name="broadcastTimeout"><see cref="ONVIF_BROADCAST_TIMEOUT"/>.</param>
        /// <returns>A list of discovered devices.</returns>
        public static async Task<IList<string>> DiscoverAsync(string ipAddress, int broadcastTimeout = ONVIF_BROADCAST_TIMEOUT)
        {
            await _discoverySlim.WaitAsync();

            const string WS_DISCOVERY_PROBE_MESSAGE =
            "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\">\r\n" +
            "   <s:Header>\r\n" +
            "      <a:Action s:mustUnderstand=\"1\">http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</a:Action>\r\n" +
            "      <a:MessageID>urn:uuid:e1245346-bee7-4ef0-82f2-c02a69b54d9c</a:MessageID>\r\n" + // uuid has to be replaced by a unique one before sending the request
            "      <a:ReplyTo>\r\n" +
            "        <a:Address>http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous</a:Address>\r\n" +
            "      </a:ReplyTo>\r\n" +
            "      <a:To>urn:schemas-xmlsoap-org:ws:2005:04:discovery</a:To>\r\n" +
            "   </s:Header>\r\n" +
            "   <s:Body>\r\n" +
            "      <Probe xmlns=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\">\r\n" +
            "         <d:Types xmlns:d=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\" xmlns:dp0=\"http://www.onvif.org/ver10/network/wsdl\">dp0:NetworkVideoTransmitter</d:Types>\r\n" +
            "      </Probe>\r\n" +
            "   </s:Body>\r\n" +
            "</s:Envelope>\r\n";

            IList<string> devices = new List<string>();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), ONVIF_BROADCAST_PORT + 1);
            IPEndPoint multicastEndpoint = new IPEndPoint(IPAddress.Parse(WS_DISCOVERY_ADDRESS_IPv4), WS_DISCOVERY_PORT);

            try
            {
                using (UdpClient client = new UdpClient(endPoint))
                {
                    UdpState s = new UdpState();
                    s.Endpoint = endPoint;
                    s.Client = client;
                    s.Result = devices;

                    client.BeginReceive(MessageReceived, s);

                    // Give the probe a unique urn:uuid (we must do this for each probe!)
                    string uuid = Guid.NewGuid().ToString();

                    // Composes and sends a Probe to discover devices on the network. uuid is the urn:uuid to put in the probe.
                    string onvifDiscoveryProbe = WS_DISCOVERY_PROBE_MESSAGE.Replace("e1245346-bee7-4ef0-82f2-c02a69b54d9c", uuid.ToLowerInvariant());

                    byte[] message = Encoding.UTF8.GetBytes(onvifDiscoveryProbe);
                    await client.SendAsync(message, message.Count(), multicastEndpoint);

                    // make sure we do not wait forever
                    await Task.Delay(broadcastTimeout);

                    return s.Result;
                }
            }
            finally
            {
                _discoverySlim.Release();
            }
        }

        private static void MessageReceived(IAsyncResult result)
        {
            try
            {
                UdpClient client = ((UdpState)result.AsyncState).Client;
                IPEndPoint endpoint = ((UdpState)result.AsyncState).Endpoint;
                byte[] receiveBytes = client.EndReceive(result, ref endpoint);
                string message = Encoding.UTF8.GetString(receiveBytes);
                string host = endpoint.Address.ToString();
                var devices = ((UdpState)result.AsyncState).Result;
                devices.Add(host);
                client.BeginReceive(MessageReceived, result.AsyncState);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
