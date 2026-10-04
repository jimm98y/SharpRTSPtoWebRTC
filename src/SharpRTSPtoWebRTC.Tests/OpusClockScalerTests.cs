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

using SharpRTSPtoWebRTC.Codecs;

namespace SharpRTSPtoWebRTC.Tests
{
    /// <summary>
    /// Carrying the AAC timestamp over to the 48kHz clock OPUS is sent on.
    /// </summary>
    [TestClass]
    public class OpusClockScalerTests
    {
        private const int AacClock = 44100;
        private const int OpusClock = 48000;
        private const int FrameSamples = 1024; // one AAC frame

        [TestMethod]
        public void TheFirstTimestampIsTheScaledSourceTimestamp()
        {
            var scaler = new OpusClockScaler(AacClock, OpusClock);

            Assert.AreEqual((uint)(44100UL * OpusClock / AacClock), scaler.Next(44100));
        }

        [TestMethod]
        public void OneAacFrameCostsAboutOneThousandOneHundredAndFifteenTicks()
        {
            var scaler = new OpusClockScaler(AacClock, OpusClock);
            uint first = scaler.Next(0);
            uint second = scaler.Next(FrameSamples);

            // 1024 * 48000 / 44100 = 1114.6
            Assert.AreEqual(1114u, second - first);
        }

        /// <summary>
        /// The bug this class exists for: scaling the absolute timestamp did not survive the wrap,
        /// stepping the output clock back about two hours instead of forward by one frame.
        /// </summary>
        [TestMethod]
        public void TheOutputStepsForwardAcrossASourceWrap()
        {
            var scaler = new OpusClockScaler(AacClock, OpusClock);

            // three frames up to 2^32, then three past it
            uint start = unchecked(0u - 3u * FrameSamples);
            uint previous = scaler.Next(start);

            for (int i = 1; i < 6; i++)
            {
                uint source = unchecked(start + (uint)i * FrameSamples);
                uint current = scaler.Next(source);
                uint step = unchecked(current - previous);

                Assert.IsTrue(step == 1114u || step == 1115u,
                    $"step {i} across the wrap was {step} ticks, expected one frame (1114 or 1115)");

                previous = current;
            }
        }

        [TestMethod]
        public void TheRemainderIsCarriedSoADayDoesNotDrift()
        {
            var scaler = new OpusClockScaler(AacClock, OpusClock);

            long frames = 24L * 60 * 60 * AacClock / FrameSamples;
            uint previous = scaler.Next(0);
            long total = 0;

            for (long i = 1; i < frames; i++)
            {
                uint current = scaler.Next(unchecked((uint)(i * FrameSamples)));
                total += unchecked((int)(current - previous));
                previous = current;
            }

            // truncating 1114.6 to 1114 every frame would be about 43 seconds short over a day
            long exact = (long)((double)(frames - 1) * FrameSamples * OpusClock / AacClock);
            Assert.AreEqual(exact, total, "a day of frames drifted");
        }

        /// <summary>
        /// After a reconnect the camera picks a fresh random start, which is not a wrap.
        /// </summary>
        [TestMethod]
        public void ANewStreamCarriesTheOutputClockOnRatherThanLeaping()
        {
            var scaler = new OpusClockScaler(AacClock, OpusClock);
            scaler.Next(0);
            uint before = scaler.Next(FrameSamples);

            // a fresh random start, hours away from where the old stream was
            uint after = scaler.Next(3_000_000_000);

            Assert.AreEqual(before, after, "the output clock leapt on a reconnect");

            // and it carries on stepping normally from there
            Assert.AreEqual(1114u, scaler.Next(3_000_000_000 + FrameSamples) - after);
        }

        [TestMethod]
        public void TheOutputNeverStepsBackwardsOverAFullSourceCycle()
        {
            var scaler = new OpusClockScaler(AacClock, OpusClock);

            // walk the whole 2^32 source range in frame sized steps, through the wrap
            uint source = 0;
            uint previous = scaler.Next(source);

            for (long i = 0; i < (1L << 32) / FrameSamples; i++)
            {
                source = unchecked(source + FrameSamples);
                uint current = scaler.Next(source);
                uint step = unchecked(current - previous);

                Assert.IsTrue(step == 1114u || step == 1115u, $"step was {step} ticks at source {source}");
                previous = current;
            }
        }
    }
}
