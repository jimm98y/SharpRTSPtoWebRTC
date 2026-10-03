using SharpRTSPtoWebRTC.Codecs;

namespace SharpRTSPtoWebRTC.Tests
{
    /// <summary>
    /// Taking the decoding order numbers out of H265 and H266 payloads, which no browser reads.
    /// </summary>
    [TestClass]
    public class DonlRemoverTests
    {
        // H265 payload headers: the type is bits 1-6 of the first byte
        private const byte H265_IDR = 19 << 1;
        private const byte H265_AP = 48 << 1;
        private const byte H265_FU = 49 << 1;
        private const byte H265_PACI = 50 << 1;

        [TestMethod]
        public void TheDonlIsTakenOutOfASingleNalUnit()
        {
            byte[] payload = { H265_IDR, 0x01, 0x12, 0x34, 0xAA, 0xBB };

            CollectionAssert.AreEqual(new byte[] { H265_IDR, 0x01, 0xAA, 0xBB }, DonlRemover.StripH265(payload));
        }

        [TestMethod]
        public void TheDonlIsTakenOutOfTheFirstFragmentOnly()
        {
            byte[] first = { H265_FU, 0x01, 0x80 | 19, 0x12, 0x34, 0xAA };
            byte[] middle = { H265_FU, 0x01, 19, 0xBB, 0xCC };

            CollectionAssert.AreEqual(new byte[] { H265_FU, 0x01, 0x80 | 19, 0xAA }, DonlRemover.StripH265(first));
            CollectionAssert.AreEqual(middle, DonlRemover.StripH265(middle));
        }

        [TestMethod]
        public void TheDonlAndDondAreTakenOutOfAnAggregationPacket()
        {
            byte[] payload =
            {
                H265_AP, 0x01,
                0x12, 0x34, 0x00, 0x03, 0x40, 0x01, 0xAA, // DONL, size, a VPS
                0x01,       0x00, 0x02, 0x42, 0x01,       // DOND, size, an SPS
            };

            byte[] expected =
            {
                H265_AP, 0x01,
                0x00, 0x03, 0x40, 0x01, 0xAA,
                0x00, 0x02, 0x42, 0x01,
            };

            CollectionAssert.AreEqual(expected, DonlRemover.StripH265(payload));
        }

        [TestMethod]
        public void AnAggregationPacketThatRunsShortIsDropped()
        {
            byte[] payload = { H265_AP, 0x01, 0x12, 0x34, 0x00, 0x09, 0x40, 0x01 };

            Assert.IsNull(DonlRemover.StripH265(payload));
        }

        [TestMethod]
        public void APaciPacketIsDropped()
        {
            Assert.IsNull(DonlRemover.StripH265(new byte[] { H265_PACI, 0x01, 0x00, 0x00, 0x00 }));
        }

        [TestMethod]
        public void APayloadTooShortForItsDonlIsDropped()
        {
            Assert.IsNull(DonlRemover.StripH265(new byte[] { H265_IDR, 0x01, 0x12 }));
        }

        [TestMethod]
        public void TheDonlIsTakenOutOfAnH266Fragment()
        {
            // H266 payload headers: the type is bits 3-7 of the second byte; 29 is a fragmentation unit
            byte[] payload = { 0x00, 29 << 3, 0x80 | 8, 0x12, 0x34, 0xAA };

            CollectionAssert.AreEqual(new byte[] { 0x00, 29 << 3, 0x80 | 8, 0xAA }, DonlRemover.StripH266(payload));
        }
    }
}
