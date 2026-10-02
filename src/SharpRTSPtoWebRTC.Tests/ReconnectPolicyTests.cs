using SharpRTSPClient;
using SharpRTSPtoWebRTC.WebRTCProxy;
using System;

namespace SharpRTSPtoWebRTC.Tests
{
    /// <summary>
    /// Which dropped streams are worth reconnecting to, and how hard.
    /// </summary>
    [TestClass]
    public class ReconnectPolicyTests
    {
        [TestMethod]
        [DataRow(StoppedReason.ConnectionFailed)]
        [DataRow(StoppedReason.RtcpBye)]
        [DataRow(StoppedReason.ProtocolError)]
        [DataRow(StoppedReason.ServerError)]
        [DataRow(StoppedReason.Unknown)]
        public void ATransientFailureIsRetried(StoppedReason reason)
        {
            Assert.IsTrue(ReconnectPolicy.IsWorthRetrying(reason));
        }

        /// <summary>
        /// Repeating the same exchange cannot change the answer, and against a camera that locks an
        /// account out after so many failures it does real harm.
        /// </summary>
        [TestMethod]
        [DataRow(StoppedReason.Unauthorized)]
        [DataRow(StoppedReason.NotFound)]
        [DataRow(StoppedReason.UnsupportedMedia)]
        [DataRow(StoppedReason.EncryptionUnavailable)]
        public void AFailureThatWillNotChangeIsNotRetried(StoppedReason reason)
        {
            Assert.IsFalse(ReconnectPolicy.IsWorthRetrying(reason));
        }

        [TestMethod]
        public void TheDelayBacksOff()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(1), ReconnectPolicy.Delay(1));
            Assert.AreEqual(TimeSpan.FromSeconds(2), ReconnectPolicy.Delay(2));
            Assert.AreEqual(TimeSpan.FromSeconds(4), ReconnectPolicy.Delay(3));
            Assert.AreEqual(TimeSpan.FromSeconds(8), ReconnectPolicy.Delay(4));
            Assert.AreEqual(TimeSpan.FromSeconds(16), ReconnectPolicy.Delay(5));
        }

        [TestMethod]
        public void TheDelayIsCappedHoweverLongTheOutage()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(30), ReconnectPolicy.Delay(6));
            Assert.AreEqual(TimeSpan.FromSeconds(30), ReconnectPolicy.Delay(100));

            // the cap is applied before anything is shifted, so a long outage cannot overflow it
            Assert.AreEqual(TimeSpan.FromSeconds(30), ReconnectPolicy.Delay(int.MaxValue));
        }

        [TestMethod]
        public void TheFirstAttemptStillWaits()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(1), ReconnectPolicy.Delay(0));
            Assert.AreEqual(TimeSpan.FromSeconds(1), ReconnectPolicy.Delay(-5));
        }
    }
}
