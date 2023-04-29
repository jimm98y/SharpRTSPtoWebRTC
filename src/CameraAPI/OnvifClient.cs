using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Xml.XPath;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace CameraAPI
{
    public class OnvifClient : IDisposable
    {
        private class UdpState
        {
            public UdpClient Client { get; set; }
            public IPEndPoint Endpoint { get; set; }
            public IList<string> Result { get; set; }
        }

        public const int ONVIF_BROADCAST_TIMEOUT = 4000; // 4s timeout
        private const int ONVIF_BROADCAST_PORT = 54567;
        public const string WS_DISCOVERY_ADDRESS_IPv4 = "239.255.255.250";
        public const int WS_DISCOVERY_PORT = 3702;

        private static SemaphoreSlim _discoverySlim = new SemaphoreSlim(1);
        private bool disposedValue;
        private readonly string _userName;
        private readonly string _password;
        private readonly string _onvifUrl;
        private readonly HttpClient _client;

        private TimeSpan _cameraTimeOffset = TimeSpan.Zero;

        public OnvifClient(string onvifUrl, string userName = null, string password = null)
        {
            this._onvifUrl = onvifUrl ?? throw new ArgumentNullException(nameof(onvifUrl));
            this._userName = userName;
            this._password = password;
            this._client = new HttpClient();
        }

        public async Task<Dictionary<string, string>> GetCapabilitiesAsync()
        {
            const string ONVIF_GETCAPABILITIES =
                @"<s:Body xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                    <GetCapabilities xmlns=""http://www.onvif.org/ver10/device/wsdl"">
                        <Category>All</Category>
                    </GetCapabilities>
                </s:Body>";

            string message = Envelope(ONVIF_GETCAPABILITIES);
            string response = await PostOnvifMessage(this._onvifUrl, message);

            Dictionary<string, string> ret = new Dictionary<string, string>();

            XNamespace nsCap = "http://www.onvif.org/ver10/device/wsdl";
            XNamespace nsAddr = "http://www.onvif.org/ver10/schema";

            using (var textReader = new StringReader(response))
            {
                var doc =  XDocument.Load(textReader);

                var capabilities = 
                    (from node in doc.Descendants(nsCap + "Capabilities").Elements()
                    select node.Name).ToArray();

                foreach(var capability in capabilities)
                {
                    var url =
                        (from node in doc.Descendants(capability).Descendants(nsAddr + "XAddr")
                         select node.Value).FirstOrDefault();

                    ret.Add(capability.LocalName, url);
                }
            }

            return ret;
        }

        public async Task<string[]> GetProfilesAsync(Dictionary<string, string> capabilities)
        {
            string mediaEndpoint = capabilities["Media"];

            const string ONVIF_GETPROFILES =
                @"<s:Body xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                   <GetProfiles xmlns=""http://www.onvif.org/ver10/media/wsdl""/>
                </s:Body>";

            string message = Envelope(ONVIF_GETPROFILES);
            string response = await PostOnvifMessage(mediaEndpoint, message);

            XNamespace ns = "http://www.onvif.org/ver10/media/wsdl";
            using (var textReader = new StringReader(response))
            {
                var doc = XDocument.Load(textReader);

                var profiles =
                    (from node in doc.Descendants(ns + "GetProfilesResponse").Elements()
                     select node.Attribute("token").Value).ToArray();

                return profiles;
            }
        }

        /// <summary>
        /// Get date and time from the camera.
        /// </summary>
        /// <returns><see cref="DateTime"/> in UTC.</returns>
        public async Task<DateTime> GetDateAndTimeAsync()
        {
            const string ONVIF_GETDATEANDTIME_MESSAGE =
                @"<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"">
                    <s:Body xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                        <GetSystemDateAndTime xmlns=""http://www.onvif.org/ver10/device/wsdl""/>
                    </s:Body>
                </s:Envelope>";

            string response = await PostOnvifMessage(this._onvifUrl, ONVIF_GETDATEANDTIME_MESSAGE);

            using (var textReader = new StringReader(response))
            {
                var document = new XPathDocument(textReader);
                var navigator = document.CreateNavigator();

                // TODO: use local time and the timezone

                // UTC is optional according to the specification
                int hour = int.Parse(ReadXPathValue(navigator, "//*[local-name()='UTCDateTime']/*[local-name()='Time']/*[local-name()='Hour']/text()"));
                int minute = int.Parse(ReadXPathValue(navigator, "//*[local-name()='UTCDateTime']/*[local-name()='Time']/*[local-name()='Minute']/text()"));
                int second = int.Parse(ReadXPathValue(navigator, "//*[local-name()='UTCDateTime']/*[local-name()='Time']/*[local-name()='Second']/text()"));

                int year = int.Parse(ReadXPathValue(navigator, "//*[local-name()='UTCDateTime']/*[local-name()='Date']/*[local-name()='Year']/text()"));
                int month = int.Parse(ReadXPathValue(navigator, "//*[local-name()='UTCDateTime']/*[local-name()='Date']/*[local-name()='Month']/text()"));
                int day = int.Parse(ReadXPathValue(navigator, "//*[local-name()='UTCDateTime']/*[local-name()='Date']/*[local-name()='Day']/text()"));

                var cameraTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

                // set the current time offset to sync time in between the camera and the client
                this._cameraTimeOffset = DateTime.UtcNow.Subtract(cameraTime);

                return cameraTime;
            }
        }

        public async Task<string> GetStreamUriAsync(Dictionary<string, string> capabilities, string profile)
        {
            string mediaEndpoint = capabilities["Media"];

            const string ONVIF_GETSTREAMURI =
                @"<s:Body xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                        <GetStreamUri xmlns=""http://www.onvif.org/ver10/media/wsdl"">
                            <StreamSetup>
                                <Stream xmlns=""http://www.onvif.org/ver10/schema"">RTP-Unicast</Stream>
                                <Transport xmlns=""http://www.onvif.org/ver10/schema"">
                                    <Protocol>RTSP</Protocol>
                                </Transport>
                            </StreamSetup>
                            <ProfileToken></ProfileToken>
                        </GetStreamUri>
                    </s:Body>";

            string message = Envelope(ONVIF_GETSTREAMURI)
              .Replace("<ProfileToken></ProfileToken>", $"<ProfileToken>{profile}</ProfileToken>"); // profile token

            string response = await PostOnvifMessage(mediaEndpoint, message);

            using (var textReader = new StringReader(response))
            {
                var document = new XPathDocument(textReader);
                var navigator = document.CreateNavigator();

                string uri = ReadXPathValue(navigator, "//*[local-name()='GetStreamUriResponse']/*[local-name()='MediaUri']/*[local-name()='Uri']/text()");
                return uri;
            }
        }

        private async Task<string> PostOnvifMessage(string url, string message)
        {
            using (var response = await _client.PostAsync(url, new StringContent(message)))
            {
                string responseMessage = await response.Content.ReadAsStringAsync();
                return responseMessage;
            }
        }

        private static string ReadXPathValue(XPathNavigator navigator, string xpath)
        {
            var node = navigator.SelectSingleNode(xpath);
            if (node != null)
            {
                return node.Value;
            }
            return null;
        }

        private string Envelope(string body)
        {
            GetPasswordDigest(this._password, this._cameraTimeOffset, out string nonce, out string timestamp, out string digest);
            return
                $@"<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"">
                    <s:Header>
                        <Security s:mustUnderstand=""1"" xmlns=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
                            <UsernameToken>
                                <Username>{this._userName}</Username>
                                <Password Type=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest"">{digest}</Password>
                                <Nonce EncodingType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"">{nonce}</Nonce>
                                <Created xmlns=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"">{timestamp}</Created>
                            </UsernameToken>
                        </Security>
                    </s:Header>
                    {body}
                </s:Envelope>";
        }

        private static void GetPasswordDigest(string password, TimeSpan cameraTimeOffset, out string nonce, out string timestamp, out string digest)
        {
            // Get nonce
            Random rnd = new Random();
            byte[] nonceBytes = new byte[16];
            rnd.NextBytes(nonceBytes);
            string nonce64 = Convert.ToBase64String(nonceBytes);
            nonce = nonce64;

            // Get timestamp
            DateTime created = DateTime.UtcNow - cameraTimeOffset;
            string creationtime = created.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            byte[] creationtimeBytes = Encoding.UTF8.GetBytes(creationtime);
            timestamp = creationtime;

            // Convert the plain password to bytes
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Concatenate nonce + creationtime + password
            byte[] concatenationBytes = new byte[nonceBytes.Length + creationtimeBytes.Length + passwordBytes.Length];
            Buffer.BlockCopy(nonceBytes, 0, concatenationBytes, 0, nonceBytes.Length);
            Buffer.BlockCopy(creationtimeBytes, 0, concatenationBytes, nonceBytes.Length, creationtimeBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, concatenationBytes, nonceBytes.Length + creationtimeBytes.Length, passwordBytes.Length);

            // Apply SHA1 on the concatenation
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] pdresult = sha.ComputeHash(concatenationBytes);
            string passwordDigest = Convert.ToBase64String(pdresult);
            digest = passwordDigest;
        }

        #region Discovery

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

                    client.BeginReceive(DiscoveryMessageReceived, s);

                    // Give the probe a unique urn:uuid (we must do this for each probe!)
                    string uuid = Guid.NewGuid().ToString();

                    // Composes and sends a Probe to discover devices on the network. uuid is the urn:uuid to put in the probe.
                    string onvifDiscoveryProbe = WS_DISCOVERY_PROBE_MESSAGE.Replace("e1245346-bee7-4ef0-82f2-c02a69b54d9c", uuid.ToLowerInvariant());

                    byte[] message = Encoding.UTF8.GetBytes(onvifDiscoveryProbe);
                    await client.SendAsync(message, message.Count(), multicastEndpoint);

                    // make sure we do not wait forever
                    await Task.Delay(broadcastTimeout);

                    return s.Result.OrderBy(x => x).ToArray();
                }
            }
            finally
            {
                _discoverySlim.Release();
            }
        }

        private static void DiscoveryMessageReceived(IAsyncResult result)
        {
            try
            {
                UdpClient client = ((UdpState)result.AsyncState).Client;
                IPEndPoint endpoint = ((UdpState)result.AsyncState).Endpoint;
                byte[] receiveBytes = client.EndReceive(result, ref endpoint);
                string message = Encoding.UTF8.GetString(receiveBytes);
                string host = endpoint.Address.ToString();
                var devices = ((UdpState)result.AsyncState).Result;
                var deviceEndpoint = ReadOnvifEndpoint(message);
                if (deviceEndpoint != null)
                {
                    devices.Add(deviceEndpoint);
                }
                client.BeginReceive(DiscoveryMessageReceived, result.AsyncState);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static string ReadOnvifEndpoint(string message)
        {
            using (var textReader = new StringReader(message))
            {
                var document = new XPathDocument(textReader);
                var navigator = document.CreateNavigator();

                // local-name is used to ignore the namespace
                var node = navigator.SelectSingleNode("//*[local-name()='XAddrs']/text()");
                if (node != null)
                {
                    string[] addresses = node.Value.Split(' ');
                    return addresses.First();
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion // Discovery

        #region IDisposable implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _client.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion // IDisposable implementation
    }
}
