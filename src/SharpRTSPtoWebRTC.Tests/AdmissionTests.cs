// SharpRTSPtoWebRTC
// Copyright (C) 2026 Lukas Volf
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SharpRTSPClient;
using SharpRTSPtoWebRTC.WebRTCProxy;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharpRTSPtoWebRTC.Tests
{
    [TestClass]
    [DoNotParallelize] // of ConnectTimeout, which ConnectTimeoutTests sets too
    public class AdmissionTests
    {
        [TestMethod]
        public async Task OfAViewerBeingSetUpOthersWaitOrAreRefused()
        {
            // a camera that takes the connection and says nothing: an offer of it is being set up for the connect timeout
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var timeout = RTSPtoWebRTCProxyService.ConnectTimeout;
            RTSPtoWebRTCProxyService.ConnectTimeout = TimeSpan.FromSeconds(1);
            try
            {
                var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["MaxPendingViewers"] = "1",
                    ["MaxPendingViewersPerAddress"] = "1",
                    ["PendingWaitSeconds"] = "0.1",
                }).Build();
                var service = new RTSPtoWebRTCProxyService(NullLoggerFactory.Instance, config);
                string url = $"rtsp://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/stream";
                IPAddress a = IPAddress.Parse("192.0.2.1"), b = IPAddress.Parse("192.0.2.2");
                Task<SessionDescriptionInit> Offer(string id, IPAddress client) => service.GetOfferAsync(id, url, null, null, 0, 0, RTPTransport.TCP, 0, 0, null, client);

                var first = Offer("first", a);
                await Assert.ThrowsExactlyAsync<TooManyPendingException>(() => Offer("again", a), "of the same address, at once");
                await Assert.ThrowsExactlyAsync<ServerBusyException>(() => Offer("other", b), "of another address, of no place in the wait");
                await Assert.ThrowsExactlyAsync<TimeoutException>(() => first);
                // its place given back once it failed: the next gets as far as the camera
                await Assert.ThrowsExactlyAsync<TimeoutException>(() => Offer("later", b));
            }
            finally
            {
                RTSPtoWebRTCProxyService.ConnectTimeout = timeout;
                listener.Stop();
            }
        }
    }
}
