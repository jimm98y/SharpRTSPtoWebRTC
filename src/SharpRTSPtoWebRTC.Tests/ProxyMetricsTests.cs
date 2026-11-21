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

using SharpRTSPtoWebRTC.WebRTCProxy;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

namespace SharpRTSPtoWebRTC.Tests
{
    [TestClass]
    public class ProxyMetricsTests
    {
        [TestMethod]
        public void CountsOfTheMeterOfTheProxy()
        {
            var measured = new List<(string Name, long Value)>();
            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == "SharpRTSPtoWebRTC")
                    l.EnableMeasurementEvents(instrument);
            };
            listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) => { lock (measured) measured.Add((instrument.Name, value)); });
            listener.SetMeasurementEventCallback<int>((instrument, value, tags, state) => { lock (measured) measured.Add((instrument.Name, value)); });
            listener.Start();

            ProxyMetrics.Received(true);
            ProxyMetrics.Sent(true, true);
            ProxyMetrics.Sent(false, false);
            ProxyMetrics.Observe(() => Enumerable.Empty<WebRtcViewer>());
            listener.RecordObservableInstruments();

            lock (measured)
            {
                CollectionAssert.IsSubsetOf(new[] { ("rtsp.video.packets", 1L), ("webrtc.video.packets", 1L), ("webrtc.audio.refused", 1L), ("webrtc.viewers", 0L) }, measured);
            }
        }
    }
}
