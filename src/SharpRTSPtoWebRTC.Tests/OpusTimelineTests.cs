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
    /// The timestamps the Opus frames go out with, one frame apart whatever the rounding of the
    /// source clock does.
    /// </summary>
    [TestClass]
    public class OpusTimelineTests
    {
        private const int FRAME = 960;

        [TestMethod]
        public void TheFirstFrameStartsWhereTheSourceSays()
        {
            var timeline = new OpusTimeline(FRAME);
            timeline.Align(123456);

            Assert.AreEqual(123456u, timeline.Take(FRAME));
            Assert.AreEqual(123456u + FRAME, timeline.Take(FRAME));
        }

        [TestMethod]
        public void TheRoundingOfTheSourceClockDoesNotMoveTheFrames()
        {
            // 1024 samples at 44.1kHz are 1114.56 at 48kHz, so where the source puts the next frame
            // wobbles a sample either side of where counting on does
            var timeline = new OpusTimeline(FRAME);
            timeline.Align(0);
            uint previous = timeline.Take(FRAME);

            int[] wobble = { 1, -1, 0, 1, -1, 1, 0, -1 };
            foreach (int w in wobble)
            {
                timeline.Align(unchecked(previous + FRAME + (uint)w));
                uint next = timeline.Take(FRAME);

                Assert.AreEqual(FRAME, (int)(next - previous));
                previous = next;
            }
        }

        [TestMethod]
        public void AGapInTheSourceIsFollowed()
        {
            var timeline = new OpusTimeline(FRAME);
            timeline.Align(0);
            timeline.Take(FRAME);

            // a quarter of a second went missing
            timeline.Align(FRAME + 12000);

            Assert.AreEqual((uint)(FRAME + 12000), timeline.Take(FRAME));
        }

        [TestMethod]
        public void ASourceThatStepsBackIsFollowed()
        {
            var timeline = new OpusTimeline(FRAME);
            timeline.Align(100000);
            timeline.Take(FRAME);

            timeline.Align(50000);

            Assert.AreEqual(50000u, timeline.Take(FRAME));
        }

        [TestMethod]
        public void TheClockWrapsWithoutAJump()
        {
            var timeline = new OpusTimeline(FRAME);
            timeline.Align(uint.MaxValue - 100);
            uint first = timeline.Take(FRAME);

            // the source, a sample off, on the far side of the wrap
            timeline.Align(unchecked(first + FRAME + 1));
            uint second = timeline.Take(FRAME);

            Assert.AreEqual(FRAME, unchecked((int)(second - first)));
            Assert.IsTrue(second < first); // it did wrap
        }
    }
}
