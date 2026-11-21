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

using System;
using System.Collections.Generic;
#if NET
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
#endif

namespace SharpRTSPtoWebRTC.WebRTCProxy
{
    /// <summary>
    /// The proxy's metrics, of the meter "SharpRTSPtoWebRTC" (of .NET 10 and later; of the others, none): what came of the
    /// cameras, what went to the viewers and what was refused, and of the viewers connected, their estimates, round trips and
    /// pacers - of where a stream's packets were lost, of a server of many viewers (dotnet-counters, or OpenTelemetry).
    /// </summary>
    internal static class ProxyMetrics
    {
#if NET
        private static readonly Meter Meter = new Meter("SharpRTSPtoWebRTC");
        private static readonly Counter<long> VideoReceived = Meter.CreateCounter<long>("rtsp.video.packets", "{packet}", "RTP packets of video of the cameras");
        private static readonly Counter<long> AudioReceived = Meter.CreateCounter<long>("rtsp.audio.packets", "{packet}", "RTP packets of audio of the cameras");
        private static readonly Counter<long> VideoSent = Meter.CreateCounter<long>("webrtc.video.packets", "{packet}", "Packets of video sent to the viewers");
        private static readonly Counter<long> VideoRefused = Meter.CreateCounter<long>("webrtc.video.refused", "{packet}", "Packets of video a viewer's connection did not send");
        private static readonly Counter<long> AudioSent = Meter.CreateCounter<long>("webrtc.audio.packets", "{packet}", "Packets of audio sent to the viewers");
        private static readonly Counter<long> AudioRefused = Meter.CreateCounter<long>("webrtc.audio.refused", "{packet}", "Packets of audio a viewer's connection did not send");
        private static readonly Counter<long> AudioDropped = Meter.CreateCounter<long>("audio.frames.dropped", "{frame}", "AAC frames dropped, of the transcoding behind");

        // the viewers of the service last made; of a snapshot of their stats of each collection, not of each gauge
        private static Func<IEnumerable<WebRtcViewer>> _viewers;
        private static readonly object _sync = new object();
        private static long _snapshotAt;
        private static Snapshot _snapshot = new Snapshot();

        private sealed class Snapshot
        {
            public int Viewers, PacedTotal, PacedMax;
            public long EstimateP10, EstimateP50;
            public double RttP50, RttP90;
        }

        static ProxyMetrics()
        {
            Meter.CreateObservableGauge("webrtc.viewers", () => Take().Viewers, "{viewer}", "Viewers connected");
            Meter.CreateObservableGauge("webrtc.estimate.p10", () => Take().EstimateP10 / 1000, "kbit/s", "The estimates of the viewers' paths: of the tenth of them lowest");
            Meter.CreateObservableGauge("webrtc.estimate.p50", () => Take().EstimateP50 / 1000, "kbit/s", "The estimates of the viewers' paths: the median");
            Meter.CreateObservableGauge("webrtc.rtt.p50", () => Take().RttP50, "ms", "The viewers' round trips: the median");
            Meter.CreateObservableGauge("webrtc.rtt.p90", () => Take().RttP90, "ms", "The viewers' round trips: of the tenth of them longest");
            Meter.CreateObservableGauge("webrtc.paced.total", () => Take().PacedTotal, "{packet}", "Packets of video the viewers' pacers hold");
            Meter.CreateObservableGauge("webrtc.paced.max", () => Take().PacedMax, "{packet}", "Packets of video the pacer of the most holds");
        }

        public static void Observe(Func<IEnumerable<WebRtcViewer>> viewers) => _viewers = viewers;

        private static Snapshot Take()
        {
            lock (_sync)
            {
                long now = Stopwatch.GetTimestamp();
                if (now - _snapshotAt < Stopwatch.Frequency)
                    return _snapshot;
                _snapshotAt = now;
                var estimates = new List<long>();
                var rtts = new List<double>();
                var snapshot = new Snapshot();
                foreach (var viewer in _viewers?.Invoke() ?? Enumerable.Empty<WebRtcViewer>())
                {
                    if (!viewer.IsConnected)
                        continue;
                    var stats = viewer.Connection.GetStats();
                    snapshot.Viewers++;
                    snapshot.PacedTotal += stats.PacedPackets;
                    snapshot.PacedMax = Math.Max(snapshot.PacedMax, stats.PacedPackets);
                    estimates.Add(stats.EstimatedBitrate);
                    if (stats.RoundTripTime.HasValue)
                        rtts.Add(stats.RoundTripTime.Value.TotalMilliseconds);
                }
                estimates.Sort();
                rtts.Sort();
                snapshot.EstimateP10 = estimates.Count > 0 ? estimates[estimates.Count / 10] : 0;
                snapshot.EstimateP50 = estimates.Count > 0 ? estimates[estimates.Count / 2] : 0;
                snapshot.RttP50 = rtts.Count > 0 ? rtts[rtts.Count / 2] : 0;
                snapshot.RttP90 = rtts.Count > 0 ? rtts[rtts.Count * 9 / 10] : 0;
                return _snapshot = snapshot;
            }
        }

        public static void Received(bool video) => (video ? VideoReceived : AudioReceived).Add(1);

        public static void Sent(bool video, bool sent) => (video ? (sent ? VideoSent : VideoRefused) : (sent ? AudioSent : AudioRefused)).Add(1);

        public static void Dropped() => AudioDropped.Add(1);
#else
        public static void Observe(Func<IEnumerable<WebRtcViewer>> viewers) { }

        public static void Received(bool video) { }

        public static void Sent(bool video, bool sent) { }

        public static void Dropped() { }
#endif
    }
}
