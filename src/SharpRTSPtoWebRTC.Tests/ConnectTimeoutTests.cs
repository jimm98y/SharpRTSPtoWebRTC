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
using SharpRTSPtoWebRTC.WebRTCProxy;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharpRTSPtoWebRTC.Tests
{
    [TestClass]
    public class ConnectTimeoutTests
    {
        [TestMethod]
        public async Task AnOfferOfACameraThatNeverAnswersFails()
        {
            // a camera that takes the connection and says nothing
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var timeout = RTSPtoWebRTCProxyService.ConnectTimeout;
            RTSPtoWebRTCProxyService.ConnectTimeout = TimeSpan.FromSeconds(1);
            try
            {
                var service = new RTSPtoWebRTCProxyService(NullLoggerFactory.Instance, new ConfigurationBuilder().Build());
                var watch = Stopwatch.StartNew();
                await Assert.ThrowsExactlyAsync<TimeoutException>(() => service.GetOfferAsync("a", $"rtsp://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/stream"));
                Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(10), $"of the timeout, not of the reconnects: {watch.Elapsed}");
            }
            finally
            {
                RTSPtoWebRTCProxyService.ConnectTimeout = timeout;
                listener.Stop();
            }
        }
    }
}
