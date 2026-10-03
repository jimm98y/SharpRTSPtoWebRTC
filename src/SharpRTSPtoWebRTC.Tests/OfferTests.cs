using Microsoft.Extensions.Logging.Abstractions;
using SharpRTSPClient;
using SharpRTSPtoWebRTC.WebRTCProxy;
using SIPSorcery.Net;

namespace SharpRTSPtoWebRTC.Tests
{
    /// <summary>
    /// What a browser is offered for each codec the camera may send: the profile, level and the
    /// like the camera's SDP gave, since a decoder set up for one cannot be relied on for another.
    /// </summary>
    [TestClass]
    public class OfferTests
    {
        private static string Offer(
            string videoCodec, IStreamConfigurationData videoStream,
            string audioCodec = "", IStreamConfigurationData audioStream = null,
            int videoType = 96, int audioType = 97)
        {
            var proxy = new RTSPtoWebRTCProxy(
                NullLogger<RTSPtoWebRTCProxyService>.Instance, new RTSPClient(),
                videoType, videoCodec, videoStream,
                audioCodec == "" ? -1 : audioType, audioCodec, audioStream);

            var peerConnection = new RTCPeerConnection();
            try
            {
                RTSPtoWebRTCProxyService.AddTracks(peerConnection, proxy);
                return peerConnection.createOffer().sdp;
            }
            finally
            {
                peerConnection.close();
            }
        }

        // an H264 SPS: Main profile (77), no constraint flags, level 4.0 (40)
        private static readonly byte[] MainProfileSps = { 0x67, 0x4D, 0x00, 0x28, 0x95, 0xA0 };

        [TestMethod]
        public void H264IsOfferedWithTheProfileOfItsSps()
        {
            // The SPS wins over what the SDP claims: it is what the stream really is.
            var configuration = new H264StreamConfigurationData(MainProfileSps, new byte[] { 0x68, 0xCE }) { ProfileLevelId = "42E01F" };

            StringAssert.Contains(Offer("H264", configuration), "a=fmtp:96 profile-level-id=4d0028;level-asymmetry-allowed=1;packetization-mode=1");
        }

        [TestMethod]
        public void H264WithoutAnSpsIsOfferedWithTheProfileOfItsSdp()
        {
            var configuration = new H264StreamConfigurationData() { ProfileLevelId = "64001E", PacketizationMode = 1 };

            StringAssert.Contains(Offer("H264", configuration), "a=fmtp:96 profile-level-id=64001e;level-asymmetry-allowed=1;packetization-mode=1");
        }

        [TestMethod]
        public void H264ThatSaysNothingIsOfferedAsConstrainedBaseline()
        {
            // An fmtp has to be there whatever happens, or Firefox answers with VP8.
            StringAssert.Contains(Offer("H264", null), "a=fmtp:96 profile-level-id=42e01f;level-asymmetry-allowed=1;packetization-mode=1");
        }

        [TestMethod]
        [DataRow(new byte[] { 0x67, 0x4D, 0x00, 0x28 }, "4d0028")]
        [DataRow(new byte[] { 0x27, 0x64, 0x00, 0x1E, 0xAC }, "64001e")] // nal_ref_idc does not matter
        [DataRow(new byte[] { 0x68, 0xCE, 0x3C, 0x80 }, null)]           // a PPS
        [DataRow(new byte[] { 0x67, 0x4D }, null)]                         // too short
        [DataRow(null, null)]
        public void TheProfileLevelIdIsReadFromTheSps(byte[] sps, string expected)
        {
            Assert.AreEqual(expected, RTSPtoWebRTCProxy.GetProfileLevelId(sps));
        }

        [TestMethod]
        public void H265IsOfferedWithTheProfileTierAndLevelOfItsSdp()
        {
            var configuration = new H265StreamConfigurationData() { ProfileId = 2, TierFlag = 1, LevelId = 153 };

            StringAssert.Contains(Offer("H265", configuration), "a=fmtp:96 profile-id=2;tier-flag=1;level-id=153;tx-mode=SRST");
        }

        [TestMethod]
        public void H266IsOfferedWithTheProfileTierAndLevelOfItsSdp()
        {
            var configuration = new H266StreamConfigurationData() { ProfileId = 17, TierFlag = 0, LevelId = 83 };

            StringAssert.Contains(Offer("H266", configuration), "a=fmtp:96 profile-id=17;tier-flag=0;level-id=83");
        }

        [TestMethod]
        public void AV1IsOfferedWithTheProfileLevelAndTierOfItsSdp()
        {
            StringAssert.Contains(Offer("AV1", new AV1StreamConfigurationData(1, 8, 1)), "a=fmtp:96 profile=1;level-idx=8;tier=1");
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(2)]
        public void VP9IsOfferedWithTheProfileOfItsSdp(int profileId)
        {
            string sdp = Offer("VP9", new VP9StreamConfigurationData(profileId));

            StringAssert.Contains(sdp, "a=rtpmap:96 VP9/90000");
            StringAssert.Contains(sdp, $"a=fmtp:96 profile-id={profileId}");
        }

        [TestMethod]
        [DataRow("H265")]
        [DataRow("AV1")]
        [DataRow("VP9")]
        public void AConfigurationTheClientCouldNotReadLeavesTheDefaults(string codec)
        {
            // no fmtp at all: the browser takes the payload format's defaults
            Assert.IsFalse(Offer(codec, null).Contains("a=fmtp:96"));
            Assert.IsFalse(Offer(codec, new UnparsedStreamConfigurationData("profile-id=x")).Contains("a=fmtp:96"));
        }

        [TestMethod]
        public void H264WithAnUnparsedFmtpIsOfferedAsConstrainedBaseline()
        {
            string sdp = Offer("H264", new UnparsedStreamConfigurationData("profile-level-id=xyz123"));

            StringAssert.Contains(sdp, "a=fmtp:96 profile-level-id=42e01f;level-asymmetry-allowed=1;packetization-mode=1");
        }

        [TestMethod]
        public void StereoOpusIsOfferedAsStereo()
        {
            string sdp = Offer("H264", null, "OPUS", new OpusStreamConfigurationData() { SpropStereo = true });

            StringAssert.Contains(sdp, "a=rtpmap:97 opus/48000/2");
            StringAssert.Contains(sdp, "a=fmtp:97 stereo=1;sprop-stereo=1");
        }

        [TestMethod]
        public void MonoOpusSaysNothing()
        {
            Assert.IsFalse(Offer("H264", null, "OPUS", new OpusStreamConfigurationData()).Contains("a=fmtp:97"));
        }

        [TestMethod]
        [DataRow(2, "a=fmtp:111 minptime=10;useinbandfec=1;stereo=1;sprop-stereo=1")]
        [DataRow(1, "a=fmtp:111 minptime=10;useinbandfec=1\r\n")]
        public void AacIsOfferedAsOpusWithTheChannelsOfTheSource(int channelConfiguration, string expected)
        {
            var aac = new AACStreamConfigurationData() { ObjectType = 2, FrequencyIndex = 4, SamplingFrequency = 44100, ChannelConfiguration = channelConfiguration };

            StringAssert.Contains(Offer("H264", null, "AAC", aac), expected);
        }

        [TestMethod]
        public void TranscodedOpusKeepsOutOfTheWayOfTheVideoPayloadType()
        {
            var aac = new AACStreamConfigurationData() { ObjectType = 2, FrequencyIndex = 4, SamplingFrequency = 44100, ChannelConfiguration = 1 };

            string sdp = Offer("H264", null, "AAC", aac, videoType: 111);

            StringAssert.Contains(sdp, "a=rtpmap:111 H264/90000");
            StringAssert.Contains(sdp, "a=rtpmap:110 opus/48000/2");
        }

        [TestMethod]
        public void NoTelephoneEventIsOffered()
        {
            // it took payload type 101, which a camera may have given its video
            string sdp = Offer("H264", null, "PCMU", null, videoType: 101, audioType: 0);

            Assert.IsFalse(sdp.Contains("telephone-event"));
            StringAssert.Contains(sdp, "a=rtpmap:101 H264/90000");
        }
    }
}
