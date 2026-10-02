using SharpRTSPtoWebRTC.Codecs;
using System;

namespace SharpRTSPtoWebRTC.Tests
{
    /// <summary>
    /// The buffer decoded PCM waits in until there is a whole OPUS frame of it.
    /// </summary>
    [TestClass]
    public class PcmSampleBufferTests
    {
        private static short[] Ramp(int count, int from = 0)
        {
            var data = new short[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = (short)(from + i);
            }

            return data;
        }

        [TestMethod]
        public void AnEmptyBufferHasNothingInIt()
        {
            Assert.AreEqual(0, new PcmSampleBuffer().Count);
        }

        [TestMethod]
        public void SamplesComeBackInTheOrderTheyWentIn()
        {
            var buffer = new PcmSampleBuffer();
            buffer.Append(Ramp(4), 4);
            buffer.Append(Ramp(4, 4), 4);

            Assert.AreEqual(8, buffer.Count);
            CollectionAssert.AreEqual(Ramp(8), buffer.Peek(8).ToArray());
        }

        [TestMethod]
        public void AppendTakesOnlyTheCountAskedFor()
        {
            var buffer = new PcmSampleBuffer();

            // the resampler writes into a buffer bigger than the samples it produced
            buffer.Append(Ramp(100), 3);

            CollectionAssert.AreEqual(new short[] { 0, 1, 2 }, buffer.Peek(3).ToArray());
            Assert.AreEqual(3, buffer.Count);
        }

        [TestMethod]
        public void PeekLeavesTheSamplesWhereTheyAre()
        {
            var buffer = new PcmSampleBuffer();
            buffer.Append(Ramp(8), 8);

            buffer.Peek(4);

            Assert.AreEqual(8, buffer.Count);
        }

        [TestMethod]
        public void AdvanceConsumesFromTheFront()
        {
            var buffer = new PcmSampleBuffer();
            buffer.Append(Ramp(8), 8);

            buffer.Advance(3);

            Assert.AreEqual(5, buffer.Count);
            CollectionAssert.AreEqual(new short[] { 3, 4, 5, 6, 7 }, buffer.Peek(5).ToArray());
        }

        [TestMethod]
        public void WhatIsLeftOverSurvivesTheNextAppend()
        {
            var buffer = new PcmSampleBuffer();
            buffer.Append(Ramp(8), 8);
            buffer.Advance(6);          // two left over, as at the end of an encode loop
            buffer.Append(Ramp(4, 100), 4);

            CollectionAssert.AreEqual(new short[] { 6, 7, 100, 101, 102, 103 }, buffer.Peek(6).ToArray());
        }

        /// <summary>
        /// The reason for the read cursor: a steady stream must not grow the buffer for ever as the
        /// cursor walks forward.
        /// </summary>
        [TestMethod]
        public void CapacityStopsGrowingOnASteadyStream()
        {
            var buffer = new PcmSampleBuffer();
            const int Frame = 1920; // one 960 sample OPUS frame, stereo

            // prime it, then run a thousand frames through
            buffer.Append(Ramp(Frame + 100), Frame + 100);
            buffer.Advance(Frame);

            int settled = buffer.Capacity;

            for (int i = 0; i < 1000; i++)
            {
                buffer.Append(Ramp(Frame), Frame);
                buffer.Advance(Frame);
            }

            Assert.AreEqual(settled, buffer.Capacity, "the buffer kept growing as the cursor moved");
            Assert.IsTrue(buffer.Count < Frame, "more than a frame was left unencoded");
        }

        [TestMethod]
        public void AppendingNothingIsHarmless()
        {
            var buffer = new PcmSampleBuffer();
            buffer.Append(Ramp(4), 4);
            buffer.Append(new short[0], 0);

            Assert.AreEqual(4, buffer.Count);
        }

        [TestMethod]
        public void ReadingPastWhatIsThereIsRefused()
        {
            var buffer = new PcmSampleBuffer();
            buffer.Append(Ramp(4), 4);

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => buffer.Peek(5));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => buffer.Advance(5));
        }

        [TestMethod]
        public void AppendingMoreThanTheSourceHoldsIsRefused()
        {
            var buffer = new PcmSampleBuffer();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => buffer.Append(Ramp(4), 5));
            Assert.ThrowsExactly<ArgumentNullException>(() => buffer.Append(null, 1));
        }
    }
}
