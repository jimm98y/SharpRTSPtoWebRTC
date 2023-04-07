using System.Text;
using Rtsp.Messages;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Logging;

namespace CameraAPI
{
    public class RTSPClient
    {
        static RTSPClient()
        {
            Rtsp.RtspUtils.RegisterUri();
        }

        private readonly ILogger _logger;

        // Events that applications can receive
        public event Received_SPS_PPS_Delegate Received_SPS_PPS;
        public event Received_VPS_SPS_PPS_Delegate Received_VPS_SPS_PPS;
        public event Received_NALs_Delegate Received_NALs;
        public event Received_G711_Delegate Received_G711;
        public event Received_AMR_Delegate Received_AMR;
        public event Received_AAC_Delegate Received_AAC;
        public event Received_RTP_Delegate RtpMessageReceived;
        public event Setup_Completed_Delegate SetupCompleted;

        // Delegated functions (essentially the function prototype)
        public delegate void Received_SPS_PPS_Delegate(byte[] sps, byte[] pps); // H264
        public delegate void Received_VPS_SPS_PPS_Delegate(byte[] vps, byte[] sps, byte[] pps); // H265
        public delegate void Received_NALs_Delegate(List<byte[]> nal_units); // H264 or H265
        public delegate void Received_G711_Delegate(string format, List<byte[]> g711);
        public delegate void Received_AMR_Delegate(string format, List<byte[]> amr);
        public delegate void Received_AAC_Delegate(string format, List<byte[]> aac, uint ObjectType, uint FrequencyIndex, uint ChannelConfiguration);
        public delegate void Received_RTP_Delegate(byte[] rtp, uint timestamp, int markerBit, int payloadType, int skip); // RTP
        public delegate void Setup_Completed_Delegate(string video, int videoType, string audio, int audioType); // current audio and video

        public enum RTP_TRANSPORT { UDP, TCP, MULTICAST, UNKNOWN };
        public enum MEDIA_REQUEST { VIDEO_ONLY, AUDIO_ONLY, VIDEO_AND_AUDIO };
        private enum RTSP_STATUS { WaitingToConnect, Connecting, ConnectFailed, Connected };

        Rtsp.RtspTcpTransport _rtspSocket = null; // RTSP connection
        volatile RTSP_STATUS _rtspSocketStatus = RTSP_STATUS.WaitingToConnect;
        Rtsp.RtspListener _rtspClient = null;   // this wraps around a the RTSP tcp_socket stream
        RTP_TRANSPORT _rtpTransport = RTP_TRANSPORT.UNKNOWN; // Mode, either RTP over UDP or RTP over TCP using the RTSP socket
        Rtsp.UDPSocket _videoUdpPair = null;       // Pair of UDP ports used in RTP over UDP mode or in MULTICAST mode
        Rtsp.UDPSocket _audioUdpPair = null;       // Pair of UDP ports used in RTP over UDP mode or in MULTICAST mode
        string _rtspUrl = "";             // RTSP URL (username & password will be stripped out
        string _userName = "";            // Username
        string _password = "";            // Password
        string _hostname = "";            // RTSP Server hostname or IP address
        int _rtspPort = 0;                // RTSP Server TCP Port number
        string _session = "";             // RTSP Session
        string _authType = null;          // cached from most recent WWW-Authenticate reply
        string _realm = null;             // cached from most recent WWW-Authenticate reply
        string _nonce = null;             // cached from most recent WWW-Authenticate reply
        uint _ssrc = 12345;
        bool _clientWantsVideo = false;  // Client wants to receive Video
        bool _clientWantsAudio = false;  // Client wants to receive Audio
        Uri _videoUri = null;            // URI used for the Video Track
        int _videoPayload = -1;          // Payload Type for the Video. (often 96 which is the first dynamic payload value. Bosch use 35)
        int _videoDataChannel = -1;      // RTP Channel Number used for the video RTP stream or the UDP port number
        int _videoRtcpChannel = -1;      // RTP Channel Number used for the video RTCP status report messages OR the UDP port number
        bool _h264SpsPpsFired = false;   // True if the SDP included a sprop-Parameter-Set for H264 video
        bool _h265VpsSpsPpsFired = false; // True if the SDP included a sprop-vps, sprop-sps and sprop_pps for H265 video
        string _videoCodec = "";         // Codec used with Payload Types 96..127 (eg "H264")

        Uri _audioUri = null;            // URI used for the Audio Track
        int _audioPayload = -1;          // Payload Type for the Video. (often 96 which is the first dynamic payload value)
        int _audioDataChannel = -1;     // RTP Channel Number used for the audio RTP stream or the UDP port number
        int _audioRtcpChannel = -1;     // RTP Channel Number used for the audio RTCP status report messages OR the UDP port number
        string _audioCodec = "";         // Codec used with Payload Types (eg "PCMA" or "AMR")

        bool _serverSupportsGetParameter = false; // Used with RTSP keepalive
#pragma warning disable 0414 // Remove unread private members
        bool _serverSupportsSetParameter = false; // Used with RTSP keepalive
#pragma warning restore 0414 // Remove unread private members
        System.Timers.Timer _keepaliveTimer = null; // Used with RTSP keepalive

        Rtsp.H264Payload _h264Payload = null;
        Rtsp.H265Payload _h265Payload = null;
        Rtsp.G711Payload _g711Payload = new Rtsp.G711Payload();
        Rtsp.AMRPayload _amrPayload = new Rtsp.AMRPayload();
        Rtsp.AACPayload _aacPayload = null;

        private object _syncRoot = new object();

        List<RtspRequestSetup> _setupMessages = new List<RtspRequestSetup>(); // setup messages still to send
                                                                              // RTP packet (or RTCP packet) has been received.
        public RTSPClient(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Connect(string url, RTP_TRANSPORT rtpTransport, string userName = null, string password = null, MEDIA_REQUEST mediaRequest = MEDIA_REQUEST.VIDEO_AND_AUDIO)
        {
            _logger.LogDebug("Connecting to " + url);
            this._rtspUrl = url;

            // Use URI to extract username and password
            // and to make a new URL without the username and password
            try
            {
                Uri uri = new Uri(this._rtspUrl);
                _hostname = uri.Host;
                _rtspPort = uri.Port;

                if (uri.UserInfo.Length > 0)
                {
                    var userParsed = uri.UserInfo.Split(new char[] { ':' });
                    _userName = userParsed[0];
                    _password = userParsed[1];
                    this._rtspUrl = uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.UserInfo, UriFormat.UriEscaped);
                }
            }
            catch
            {
                _userName = null;
                _password = null;
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                _userName = userName;
            }

            if (!string.IsNullOrWhiteSpace(password))
            {
                _password = password;
            }

            // We can ask the RTSP server for Video, Audio or both. If we don't want audio we don't need to SETUP the audio channal or receive it
            _clientWantsVideo = false;
            _clientWantsAudio = false;
            
            if (mediaRequest == MEDIA_REQUEST.VIDEO_ONLY || mediaRequest == MEDIA_REQUEST.VIDEO_AND_AUDIO)
            {
                _clientWantsVideo = true;
            }

            if (mediaRequest == MEDIA_REQUEST.AUDIO_ONLY || mediaRequest == MEDIA_REQUEST.VIDEO_AND_AUDIO)
            {
                _clientWantsAudio = true;
            }

            // Connect to a RTSP Server. The RTSP session is a TCP connection
            _rtspSocketStatus = RTSP_STATUS.Connecting;

            try
            {
                _rtspSocket = new Rtsp.RtspTcpTransport(_hostname, _rtspPort);
            }
            catch
            {
                _rtspSocketStatus = RTSP_STATUS.ConnectFailed;
                _logger.LogWarning("Error - did not connect");
                return;
            }

            if (_rtspSocket.Connected == false)
            {
                _rtspSocketStatus = RTSP_STATUS.ConnectFailed;
                _logger.LogWarning("Error - did not connect");
                return;
            }

            _rtspSocketStatus = RTSP_STATUS.Connected;

            // Connect a RTSP Listener to the RTSP Socket (or other Stream) to send RTSP messages and listen for RTSP replies
            _rtspClient = new Rtsp.RtspListener(_rtspSocket);
            _rtspClient.AutoReconnect = false;
            _rtspClient.MessageReceived += Rtsp_MessageReceived;
            _rtspClient.DataReceived += Rtp_DataReceived;
            _rtspClient.Start(); // start listening for messages from the server (messages fire the MessageReceived event)

            // Check the RTP Transport
            // If the RTP transport is TCP then we interleave the RTP packets in the RTSP stream
            // If the RTP transport is UDP, we initialise two UDP sockets (one for video, one for RTCP status messages)
            // If the RTP transport is MULTICAST, we have to wait for the SETUP message to get the Multicast Address from the RTSP server
            this._rtpTransport = rtpTransport;

            if (rtpTransport == RTP_TRANSPORT.UDP)
            {
                _videoUdpPair = new Rtsp.UDPSocket(50000, 51000); // give a range of 500 pairs (1000 addresses) to try incase some address are in use
                _videoUdpPair.DataReceived += Rtp_DataReceived;
                _videoUdpPair.Start(); // start listening for data on the UDP ports
                _audioUdpPair = new Rtsp.UDPSocket(50000, 51000); // give a range of 500 pairs (1000 addresses) to try incase some address are in use
                _audioUdpPair.DataReceived += Rtp_DataReceived;
                _audioUdpPair.Start(); // start listening for data on the UDP ports
            }

            if (rtpTransport == RTP_TRANSPORT.TCP)
            {
                // Nothing to do. Data will arrive in the RTSP Listener
            }

            if (rtpTransport == RTP_TRANSPORT.MULTICAST)
            {
                // Nothing to do. Will open Multicast UDP sockets after the SETUP command
            }

            // Send OPTIONS
            // In the Received Message handler we will send DESCRIBE, SETUP and PLAY
            RtspRequest options_message = new RtspRequestOptions();
            options_message.RtspUri = new Uri(this._rtspUrl);
            _rtspClient.SendMessage(options_message);
        }

        // return true if this connection failed, or if it connected but is no longer connected.
        public bool StreamingFinished()
        {
            if (_rtspSocketStatus == RTSP_STATUS.ConnectFailed)
            {
                return true;
            }

            if (_rtspSocketStatus == RTSP_STATUS.Connected && _rtspSocket.Connected == false)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Pause()
        {
            if (_rtspClient != null)
            {
                // Send PAUSE
                RtspRequest pause_message = new RtspRequestPause();
                pause_message.RtspUri = new Uri(_rtspUrl);
                pause_message.Session = _session;
                
                if (_authType != null)
                {
                    AddAuthorization(pause_message, _userName, _password, _authType, _realm, _nonce, _rtspUrl);
                }

                _rtspClient.SendMessage(pause_message);
            }
        }

        public void Play()
        {
            if (_rtspClient != null)
            {
                // Send PLAY
                RtspRequest play_message = new RtspRequestPlay();
                play_message.RtspUri = new Uri(_rtspUrl);
                play_message.Session = _session;
                
                if (_authType != null)
                {
                    AddAuthorization(play_message, _userName, _password, _authType, _realm, _nonce, _rtspUrl);
                }

                _rtspClient.SendMessage(play_message);
            }
        }

        public void Stop()
        {
            if (_rtspClient != null)
            {
                // Send TEARDOWN
                RtspRequest teardown_message = new RtspRequestTeardown();
                teardown_message.RtspUri = new Uri(_rtspUrl);
                teardown_message.Session = _session;
                
                if (_authType != null)
                {
                    AddAuthorization(teardown_message, _userName, _password, _authType, _realm, _nonce, _rtspUrl);
                }

                _rtspClient.SendMessage(teardown_message);
            }

            // Stop the keepalive timer
            if (_keepaliveTimer != null)
            {
                _keepaliveTimer.Stop();
            }

            // clear up any UDP sockets
            if (_videoUdpPair != null)
            {
                _videoUdpPair.Stop();
            }

            if (_audioUdpPair != null)
            {
                _audioUdpPair.Stop();
            }

            // Drop the RTSP session
            if (_rtspClient != null)
            {
                _rtspClient.Stop();
            }
        }

        private void Rtp_DataReceived(object sender, Rtsp.RtspChunkEventArgs e)
        {
            RtspData data_received = e.Message as RtspData;

            // Check which channel the Data was received on.
            // eg the Video Channel, the Video Control Channel (RTCP)
            // the Audio Channel or the Audio Control Channel (RTCP)

            if (data_received.Channel == _videoRtcpChannel || data_received.Channel == _audioRtcpChannel)
            {
                _logger.LogDebug("Received a RTCP message on channel " + data_received.Channel);

                // RTCP Packet
                // - Version, Padding and Receiver Report Count
                // - Packet Type
                // - Length
                // - SSRC
                // - payload

                // There can be multiple RTCP packets transmitted together. Loop ever each one
                long packetIndex = 0;

                while (packetIndex < e.Message.Data.Length)
                {
                    int rtcp_version = (e.Message.Data[packetIndex + 0] >> 6);
                    int rtcp_padding = (e.Message.Data[packetIndex + 0] >> 5) & 0x01;
                    int rtcp_reception_report_count = (e.Message.Data[packetIndex + 0] & 0x1F);
                    byte rtcp_packet_type = e.Message.Data[packetIndex + 1]; // Values from 200 to 207
                    uint rtcp_length = (uint)(e.Message.Data[packetIndex + 2] << 8) + (uint)(e.Message.Data[packetIndex + 3]); // number of 32 bit words
                    uint rtcp_ssrc = (uint)(e.Message.Data[packetIndex + 4] << 24) + (uint)(e.Message.Data[packetIndex + 5] << 16)
                        + (uint)(e.Message.Data[packetIndex + 6] << 8) + (uint)(e.Message.Data[packetIndex + 7]);

                    // 200 = SR = Sender Report
                    // 201 = RR = Receiver Report
                    // 202 = SDES = Source Description
                    // 203 = Bye = Goodbye
                    // 204 = APP = Application Specific Method
                    // 207 = XR = Extended Reports

                    _logger.LogDebug("RTCP Data. PacketType=" + rtcp_packet_type + " SSRC=" + rtcp_ssrc);

                    if (rtcp_packet_type == 200)
                    {
                        // We have received a Sender Report
                        // Use it to convert the RTP timestamp into the UTC time
                        uint ntp_msw_seconds = (uint)(e.Message.Data[packetIndex + 8] << 24) + (uint)(e.Message.Data[packetIndex + 9] << 16)
                        + (uint)(e.Message.Data[packetIndex + 10] << 8) + (uint)(e.Message.Data[packetIndex + 11]);

                        uint ntp_lsw_fractions = (uint)(e.Message.Data[packetIndex + 12] << 24) + (uint)(e.Message.Data[packetIndex + 13] << 16)
                        + (uint)(e.Message.Data[packetIndex + 14] << 8) + (uint)(e.Message.Data[packetIndex + 15]);

                        uint rtp_timestamp = (uint)(e.Message.Data[packetIndex + 16] << 24) + (uint)(e.Message.Data[packetIndex + 17] << 16)
                        + (uint)(e.Message.Data[packetIndex + 18] << 8) + (uint)(e.Message.Data[packetIndex + 19]);

                        double ntp = ntp_msw_seconds + (ntp_lsw_fractions / UInt32.MaxValue);

                        // NTP Most Signigicant Word is relative to 0h, 1 Jan 1900
                        // This will wrap around in 2036
                        DateTime time = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                        time = time.AddSeconds((double)ntp_msw_seconds); // adds 'double' (whole&fraction)

                        _logger.LogDebug("RTCP time (UTC) for RTP timestamp " + rtp_timestamp + " is " + time);

                        // Send a Receiver Report
                        try
                        {
                            byte[] rtcp_receiver_report = new byte[8];
                            int version = 2;
                            int paddingBit = 0;
                            int reportCount = 0; // an empty report
                            int packetType = 201; // Receiver Report
                            int length = (rtcp_receiver_report.Length / 4) - 1; // num 32 bit words minus 1
                            rtcp_receiver_report[0] = (byte)((version << 6) + (paddingBit << 5) + reportCount);
                            rtcp_receiver_report[1] = (byte)(packetType);
                            rtcp_receiver_report[2] = (byte)((length >> 8) & 0xFF);
                            rtcp_receiver_report[3] = (byte)((length >> 0) & 0XFF);
                            rtcp_receiver_report[4] = (byte)((_ssrc >> 24) & 0xFF);
                            rtcp_receiver_report[5] = (byte)((_ssrc >> 16) & 0xFF);
                            rtcp_receiver_report[6] = (byte)((_ssrc >> 8) & 0xFF);
                            rtcp_receiver_report[7] = (byte)((_ssrc >> 0) & 0xFF);

                            if (_rtpTransport == RTP_TRANSPORT.TCP)
                            {
                                // Send it over via the RTSP connection
                                _rtspClient.SendData(_videoRtcpChannel, rtcp_receiver_report);
                            }

                            if (_rtpTransport == RTP_TRANSPORT.UDP || _rtpTransport == RTP_TRANSPORT.MULTICAST)
                            {
                                // Send it via a UDP Packet
                                _logger.LogDebug("TODO - Need to implement RTCP over UDP");
                            }
                        }
                        catch
                        {
                            _logger.LogDebug("Error writing RTCP packet");
                        }
                    }

                    packetIndex = packetIndex + ((rtcp_length + 1) * 4);
                }

                return;
            }

            if (data_received.Channel == _videoDataChannel || data_received.Channel == _audioDataChannel)
            {
                // Received some Video or Audio Data on the correct channel.

                // RTP Packet Header
                // 0 - Version, P, X, CC, M, PT and Sequence Number
                //32 - Timestamp
                //64 - SSRC
                //96 - CSRCs (optional)
                //nn - Extension ID and Length
                //nn - Extension header

                int rtp_version = (e.Message.Data[0] >> 6);
                int rtp_padding = (e.Message.Data[0] >> 5) & 0x01;
                int rtp_extension = (e.Message.Data[0] >> 4) & 0x01;
                int rtp_csrc_count = (e.Message.Data[0] >> 0) & 0x0F;
                int rtp_marker = (e.Message.Data[1] >> 7) & 0x01;
                int rtp_payload_type = (e.Message.Data[1] >> 0) & 0x7F;
                uint rtp_sequence_number = ((uint)e.Message.Data[2] << 8) + (uint)(e.Message.Data[3]);
                uint rtp_timestamp = ((uint)e.Message.Data[4] << 24) + (uint)(e.Message.Data[5] << 16) + (uint)(e.Message.Data[6] << 8) + (uint)(e.Message.Data[7]);
                uint rtp_ssrc = ((uint)e.Message.Data[8] << 24) + (uint)(e.Message.Data[9] << 16) + (uint)(e.Message.Data[10] << 8) + (uint)(e.Message.Data[11]);

                int rtp_payload_start = 4 // V,P,M,SEQ
                                    + 4 // time stamp
                                    + 4 // ssrc
                                    + (4 * rtp_csrc_count); // zero or more csrcs

                uint rtp_extension_id = 0;
                uint rtp_extension_size = 0;

                if (rtp_extension == 1)
                {
                    rtp_extension_id = ((uint)e.Message.Data[rtp_payload_start + 0] << 8) + (uint)(e.Message.Data[rtp_payload_start + 1] << 0);
                    rtp_extension_size = ((uint)e.Message.Data[rtp_payload_start + 2] << 8) + (uint)(e.Message.Data[rtp_payload_start + 3] << 0) * 4; // units of extension_size is 4-bytes
                    rtp_payload_start += 4 + (int)rtp_extension_size;  // extension header and extension payload
                }

                _logger.LogDebug("RTP Data"
                                    + " V=" + rtp_version
                                    + " P=" + rtp_padding
                                    + " X=" + rtp_extension
                                    + " CC=" + rtp_csrc_count
                                    + " M=" + rtp_marker
                                    + " PT=" + rtp_payload_type
                                    + " Seq=" + rtp_sequence_number
                                    + " Time (MS)=" + rtp_timestamp / 90 // convert from 90kHZ clock to ms
                                    + " SSRC=" + rtp_ssrc
                                    + " Size=" + e.Message.Data.Length);

                RtpMessageReceived?.Invoke(e.Message.Data, rtp_timestamp, rtp_marker, rtp_payload_type, rtp_payload_start);

                // Check the payload type in the RTP packet matches the Payload Type value from the SDP
                if (data_received.Channel == _videoDataChannel && rtp_payload_type != _videoPayload)
                {
                    _logger.LogDebug("Ignoring this Video RTP payload");
                    return; // ignore this data
                }

                // Check the payload type in the RTP packet matches the Payload Type value from the SDP
                else if (data_received.Channel == _audioDataChannel && rtp_payload_type != _audioPayload)
                {
                    _logger.LogDebug("Ignoring this Audio RTP payload");
                    return; // ignore this data
                }
                else if (data_received.Channel == _videoDataChannel && rtp_payload_type == _videoPayload && _videoCodec.Equals("H264"))
                {
                    // H264 RTP Packet

                    // If rtp_marker is '1' then this is the final transmission for this packet.
                    // If rtp_marker is '0' we need to accumulate data with the same timestamp

                    // ToDo - Check Timestamp
                    // Add the RTP packet to the tempoary_rtp list until we have a complete 'Frame'
                    byte[] rtp_payload = new byte[e.Message.Data.Length - rtp_payload_start]; // payload with RTP header removed
                    Array.Copy(e.Message.Data, rtp_payload_start, rtp_payload, 0, rtp_payload.Length); // copy payload

                    List<byte[]> nal_units = _h264Payload.ProcessRTPPacket(rtp_payload, rtp_marker); // this will cache the Packets until there is a Frame

                    if (nal_units == null)
                    {
                        // we have not passed in enough RTP packets to make a Frame of video
                    }
                    else
                    {
                        // If we did not have a SPS and PPS in the SDP then search for the SPS and PPS
                        // in the NALs and fire the Received_SPS_PPS event.
                        // We assume the SPS and PPS are in the same Frame.
                        if (_h264SpsPpsFired == false)
                        {
                            // Check this frame for SPS and PPS
                            byte[] sps = null;
                            byte[] pps = null;

                            foreach (byte[] nal_unit in nal_units)
                            {
                                if (nal_unit.Length > 0)
                                {
                                    int nal_ref_idc = (nal_unit[0] >> 5) & 0x03;
                                    int nal_unit_type = nal_unit[0] & 0x1F;

                                    if (nal_unit_type == 7)
                                    {
                                        sps = nal_unit; // SPS
                                    }

                                    if (nal_unit_type == 8)
                                    {
                                        pps = nal_unit; // PPS
                                    }
                                }
                            }
                            if (sps != null && pps != null)
                            {
                                if (Received_SPS_PPS != null)
                                {
                                    Received_SPS_PPS(sps, pps);
                                }

                                _h264SpsPpsFired = true;
                            }
                        }

                        // we have a frame of NAL Units. Write them to the file
                        if (Received_NALs != null)
                        {
                            Received_NALs(nal_units);
                        }
                    }
                }
                else if (data_received.Channel == _videoDataChannel
                            && rtp_payload_type == _videoPayload
                            && _videoCodec.Equals("H265"))
                {
                    // H265 RTP Packet

                    // If rtp_marker is '1' then this is the final transmission for this packet.
                    // If rtp_marker is '0' we need to accumulate data with the same timestamp

                    // Add the RTP packet to the tempoary_rtp list until we have a complete 'Frame'

                    byte[] rtp_payload = new byte[e.Message.Data.Length - rtp_payload_start]; // payload with RTP header removed
                    Array.Copy(e.Message.Data, rtp_payload_start, rtp_payload, 0, rtp_payload.Length); // copy payload

                    List<byte[]> nal_units = _h265Payload.ProcessRTPPacket(rtp_payload, rtp_marker); // this will cache the Packets until there is a Frame

                    if (nal_units == null)
                    {
                        // we have not passed in enough RTP packets to make a Frame of video
                    }
                    else
                    {
                        // If we did not have a VPS, SPS and PPS in the SDP then search for the VPS SPS and PPS
                        // in the NALs and fire the Received_VPS_SPS_PPS event.
                        // We assume the VPS, SPS and PPS are in the same Frame.
                        if (_h265VpsSpsPpsFired == false)
                        {
                            // Check this frame for VPS, SPS and PPS
                            byte[] vps = null;
                            byte[] sps = null;
                            byte[] pps = null;

                            foreach (byte[] nal_unit in nal_units)
                            {
                                if (nal_unit.Length > 0)
                                {
                                    int nal_unit_type = (nal_unit[0] >> 1) & 0x3F;

                                    if (nal_unit_type == 32)
                                    {
                                        vps = nal_unit; // VPS
                                    }

                                    if (nal_unit_type == 33)
                                    {
                                        sps = nal_unit; // SPS
                                    }

                                    if (nal_unit_type == 34)
                                    {
                                        pps = nal_unit; // PPS
                                    }
                                }
                            }
                            if (vps != null && sps != null && pps != null)
                            {
                                // Fire the Event
                                if (Received_VPS_SPS_PPS != null)
                                {
                                    Received_VPS_SPS_PPS(vps, sps, pps);
                                }

                                _h265VpsSpsPpsFired = true;
                            }
                        }

                        // we have a frame of NAL Units. Write them to the file
                        if (Received_NALs != null)
                        {
                            Received_NALs(nal_units);
                        }
                    }
                }
                else if (data_received.Channel == _audioDataChannel && (rtp_payload_type == 0 || rtp_payload_type == 8 || _audioCodec.Equals("PCMA") || _audioCodec.Equals("PCMU")))
                {
                    // G711 PCMA or G711 PCMU
                    byte[] rtp_payload = new byte[e.Message.Data.Length - rtp_payload_start]; // payload with RTP header removed
                    Array.Copy(e.Message.Data, rtp_payload_start, rtp_payload, 0, rtp_payload.Length); // copy payload

                    List<byte[]> audio_frames = _g711Payload.ProcessRTPPacket(rtp_payload, rtp_marker);

                    if (audio_frames == null)
                    {
                        // some error
                    }
                    else
                    {
                        // Write the audio frames to the file
                        if (Received_G711 != null)
                        {
                            Received_G711(_audioCodec, audio_frames);
                        }
                    }
                }
                else if (data_received.Channel == _audioDataChannel
                            && rtp_payload_type == _audioPayload
                            && _audioCodec.Equals("AMR"))
                {
                    // AMR
                    byte[] rtp_payload = new byte[e.Message.Data.Length - rtp_payload_start]; // payload with RTP header removed
                    Array.Copy(e.Message.Data, rtp_payload_start, rtp_payload, 0, rtp_payload.Length); // copy payload

                    List<byte[]> audio_frames = _amrPayload.ProcessRTPPacket(rtp_payload, rtp_marker);

                    if (audio_frames == null)
                    {
                        // some error
                    }
                    else
                    {
                        // Write the audio frames to the file
                        if (Received_AMR != null)
                        {
                            Received_AMR(_audioCodec, audio_frames);
                        }
                    }
                }
                else if (data_received.Channel == _audioDataChannel
                            && rtp_payload_type == _audioPayload
                            && _audioCodec.Equals("MPEG4-GENERIC")
                        && _aacPayload != null)
                {
                    // AAC
                    byte[] rtp_payload = new byte[e.Message.Data.Length - rtp_payload_start]; // payload with RTP header removed
                    Array.Copy(e.Message.Data, rtp_payload_start, rtp_payload, 0, rtp_payload.Length); // copy payload

                    List<byte[]> audio_frames = _aacPayload.ProcessRTPPacket(rtp_payload, rtp_marker);

                    if (audio_frames == null)
                    {
                        // some error
                    }
                    else
                    {
                        // Write the audio frames to the file
                        if (Received_AAC != null)
                        {
                            Received_AAC(_audioCodec, audio_frames, (uint)_aacPayload.ObjectType, (uint)_aacPayload.FrequencyIndex, (uint)_aacPayload.ChannelConfiguration);
                        }
                    }
                }
                else if (data_received.Channel == _videoDataChannel && rtp_payload_type == 26)
                {
                    _logger.LogWarning("No parser has been written for JPEG RTP packets. Please help write one");
                    return; // ignore this data
                }
                else
                {
                    _logger.LogWarning("No parser for RTP payload " + rtp_payload_type);
                }
            }
        }

        // RTSP Messages are OPTIONS, DESCRIBE, SETUP, PLAY etc
        private void Rtsp_MessageReceived(object sender, Rtsp.RtspChunkEventArgs e)
        {
            RtspResponse message = e.Message as RtspResponse;
            _logger.LogDebug("Received RTSP Message " + message.OriginalRequest.ToString());

            // If message has a 401 - Unauthorised Error, then we re-send the message with Authorization
            // using the most recently received 'realm' and 'nonce'
            if (message.IsOk == false)
            {
                _logger.LogDebug("Got Error in RTSP Reply " + message.ReturnCode + " " + message.ReturnMessage);

                if (message.ReturnCode == 401 && (message.OriginalRequest.Headers.ContainsKey(RtspHeaderNames.Authorization) == true))
                {
                    // the authorization failed.
                    Stop();
                    return;
                }

                // Check if the Reply has an Authenticate header.
                if (message.ReturnCode == 401 && message.Headers.ContainsKey(RtspHeaderNames.WWWAuthenticate))
                {
                    // Process the WWW-Authenticate header
                    // EG:   Basic realm="AProxy"
                    // EG:   Digest realm="AXIS_WS_ACCC8E3A0A8F", nonce="000057c3Y810622bff50b36005eb5efeae118626a161bf", stale=FALSE
                    // EG:   Digest realm="IP Camera(21388)", nonce="534407f373af1bdff561b7b4da295354", stale="FALSE"
                    string www_authenticate = message.Headers[RtspHeaderNames.WWWAuthenticate];
                    string auth_params = "";

                    if (www_authenticate.StartsWith("basic", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _authType = "Basic";
                        auth_params = www_authenticate.Substring(5);
                    }
                    if (www_authenticate.StartsWith("digest", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _authType = "Digest";
                        auth_params = www_authenticate.Substring(6);
                    }

                    string[] items = auth_params.Split(new char[] { ',' }); // NOTE, does not handle Commas in Quotes

                    foreach (string item in items)
                    {
                        // Split on the = symbol and update the realm and nonce
                        string[] parts = item.Trim().Split(new char[] { '=' }, 2); // max 2 parts in the results array
                        if (parts.Count() >= 2 && parts[0].Trim().Equals("realm"))
                        {
                            _realm = parts[1].Trim(new char[] { ' ', '\"' }); // trim space and quotes
                        }
                        else if (parts.Count() >= 2 && parts[0].Trim().Equals("nonce"))
                        {
                            _nonce = parts[1].Trim(new char[] { ' ', '\"' }); // trim space and quotes
                        }
                    }

                    _logger.LogDebug("WWW Authorize parsed for " + _authType + " " + _realm + " " + _nonce);
                }

                RtspMessage resend_message = message.OriginalRequest.Clone() as RtspMessage;

                if (_authType != null)
                {
                    AddAuthorization(resend_message, _userName, _password, _authType, _realm, _nonce, _rtspUrl);
                }

                _rtspClient.SendMessage(resend_message);
                return;
            }

            // If we get a reply to OPTIONS then start the Keepalive Timer and send DESCRIBE
            if (message.OriginalRequest != null && message.OriginalRequest is Rtsp.Messages.RtspRequestOptions)
            {
                // Check the capabilities returned by OPTIONS
                // The Public: header contains the list of commands the RTSP server supports
                // Eg   DESCRIBE, SETUP, TEARDOWN, PLAY, PAUSE, OPTIONS, ANNOUNCE, RECORD, GET_PARAMETER]}
                if (message.Headers.ContainsKey(RtspHeaderNames.Public))
                {
                    string[] parts = message.Headers[RtspHeaderNames.Public].Split(',');
                    foreach (string part in parts)
                    {
                        if (part.Trim().ToUpper().Equals("GET_PARAMETER"))
                        {
                            _serverSupportsGetParameter = true;
                        }

                        if (part.Trim().ToUpper().Equals("SET_PARAMETER"))
                        {
                            _serverSupportsSetParameter = true;
                        }
                    }
                }

                if (_keepaliveTimer == null)
                {
                    // Start a Timer to send an Keepalive RTSP command every 20 seconds
                    _keepaliveTimer = new System.Timers.Timer();
                    _keepaliveTimer.Elapsed += Timer_Elapsed;
                    _keepaliveTimer.Interval = 20 * 1000;
                    _keepaliveTimer.Enabled = true;

                    // Send DESCRIBE
                    RtspRequest describe_message = new RtspRequestDescribe();
                    describe_message.RtspUri = new Uri(_rtspUrl);
                    if (_authType != null)
                    {
                        AddAuthorization(describe_message, _userName, _password, _authType, _realm, _nonce, _rtspUrl);
                    }
                    _rtspClient.SendMessage(describe_message);
                }
                else
                {
                    // If the Keepalive Timer was not null, the OPTIONS reply may have come from a Keepalive
                    // So no need to generate a DESCRIBE message
                    // do nothing
                }
            }

            // If we get a reply to DESCRIBE (which was our second command), then prosess SDP and send the SETUP
            if (message.OriginalRequest != null && message.OriginalRequest is RtspRequestDescribe)
            {
                // Got a reply for DESCRIBE
                if (message.IsOk == false)
                {
                    _logger.LogDebug("Got Error in DESCRIBE Reply " + message.ReturnCode + " " + message.ReturnMessage);
                    return;
                }

                // Examine the SDP
                _logger.LogDebug(Encoding.UTF8.GetString(message.Data));

                Rtsp.Sdp.SdpFile sdp_data;
                using (StreamReader sdp_stream = new StreamReader(new MemoryStream(message.Data)))
                {
                    sdp_data = Rtsp.Sdp.SdpFile.Read(sdp_stream);
                }

                // RTP and RTCP 'channels' are used in TCP Interleaved mode (RTP over RTSP)
                // These are the channels we request. The camera confirms the channel in the SETUP Reply.
                // But, a Panasonic decides to use different channels in the reply.
                int next_free_rtp_channel = 0;
                int next_free_rtcp_channel = 1;

                // Process each 'Media' Attribute in the SDP (each sub-stream)

                for (int x = 0; x < sdp_data.Medias.Count; x++)
                {
                    bool audio = (sdp_data.Medias[x].MediaType == Rtsp.Sdp.Media.MediaTypes.audio);
                    bool video = (sdp_data.Medias[x].MediaType == Rtsp.Sdp.Media.MediaTypes.video);

                    if (video && _videoPayload != -1)
                    {
                        continue; // have already matched a video payload. don't match another
                    }

                    if (audio && _audioPayload != -1)
                    {
                        continue; // have already matched an audio payload. don't match another
                    }

                    if (audio && (_clientWantsAudio == false))
                    {
                        continue; // client does not want audio from the RTSP server
                    }

                    if (video && (_clientWantsVideo == false))
                    {
                        continue; // client does not want video from the RTSP server
                    }

                    if (audio || video)
                    {
                        // search the attributes for control, rtpmap and fmtp
                        // (fmtp only applies to video)
                        string control = "";  // the "track" or "stream id"
                        Rtsp.Sdp.AttributFmtp fmtp = null; // holds SPS and PPS in base64 (h264 video)
                        foreach (Rtsp.Sdp.Attribut attrib in sdp_data.Medias[x].Attributs)
                        {
                            if (attrib.Key.Equals("control"))
                            {
                                string sdp_control = attrib.Value;
                                if (sdp_control.ToLower().StartsWith("rtsp://"))
                                {
                                    control = sdp_control; //absolute path
                                }
                                else
                                {
                                    control = _rtspUrl + "/" + sdp_control; // relative path
                                }

                                if (video)
                                {
                                    _videoUri = new Uri(control);
                                }

                                if (audio)
                                {
                                    _audioUri = new Uri(control);
                                }
                            }
                            if (attrib.Key.Equals("fmtp"))
                            {
                                fmtp = attrib as Rtsp.Sdp.AttributFmtp;
                            }
                            if (attrib.Key.Equals("rtpmap"))
                            {
                                Rtsp.Sdp.AttributRtpMap rtpmap = attrib as Rtsp.Sdp.AttributRtpMap;

                                // Check if the Codec Used (EncodingName) is one we support
                                string[] valid_video_codecs = { "H264", "H265" };
                                string[] valid_audio_codecs = { "PCMA", "PCMU", "AMR", "MPEG4-GENERIC" /* for aac */}; // Note some are "mpeg4-generic" lower case

                                if (video && Array.IndexOf(valid_video_codecs, rtpmap.EncodingName.ToUpper()) >= 0)
                                {
                                    // found a valid codec
                                    _videoCodec = rtpmap.EncodingName.ToUpper();
                                    _videoPayload = sdp_data.Medias[x].PayloadType;
                                }

                                if (audio && Array.IndexOf(valid_audio_codecs, rtpmap.EncodingName.ToUpper()) >= 0)
                                {
                                    _audioCodec = rtpmap.EncodingName.ToUpper();
                                    _audioPayload = sdp_data.Medias[x].PayloadType;
                                }
                            }
                        }

                        // Create H264 RTP Parser
                        if (video && _videoCodec.Contains("H264"))
                        {
                            _h264Payload = new Rtsp.H264Payload(null);
                        }

                        // If the rtpmap contains H264 then split the fmtp to get the sprop-parameter-sets which hold the SPS and PPS in base64
                        if (video && _videoCodec.Contains("H264") && fmtp != null)
                        {
                            var param = Rtsp.Sdp.H264Parameters.Parse(fmtp.FormatParameter);
                            var sps_pps = param.SpropParameterSets;
                            if (sps_pps.Count() >= 2)
                            {
                                byte[] sps = sps_pps[0];
                                byte[] pps = sps_pps[1];
                                if (Received_SPS_PPS != null)
                                {
                                    Received_SPS_PPS(sps, pps);
                                }

                                _h264SpsPpsFired = true;
                            }
                        }

                        // Create H265 RTP Parser
                        if (video && _videoCodec.Contains("H265"))
                        {
                            // TODO - check if DONL is being used
                            bool has_donl = false;
                            _h265Payload = new Rtsp.H265Payload(has_donl);
                        }

                        // If the rtpmap contains H265 then split the fmtp to get the sprop-vps, sprop-sps and sprop-pps
                        // The RFC makes the VPS, SPS and PPS OPTIONAL so they may not be present. In which we pass back NULL values
                        if (video && _videoCodec.Contains("H265") && fmtp != null)
                        {
                            var param = Rtsp.Sdp.H265Parameters.Parse(fmtp.FormatParameter);
                            var vps_sps_pps = param.SpropParameterSets;

                            if (vps_sps_pps.Count() >= 3)
                            {
                                byte[] vps = vps_sps_pps[0];
                                byte[] sps = vps_sps_pps[1];
                                byte[] pps = vps_sps_pps[2];

                                if (Received_VPS_SPS_PPS != null)
                                {
                                    Received_VPS_SPS_PPS(vps, sps, pps);
                                }

                                _h265VpsSpsPpsFired = true;
                            }
                        }

                        // Create AAC RTP Parser
                        // Example fmtp is "96 profile-level-id=1;mode=AAC-hbr;sizelength=13;indexlength=3;indexdeltalength=3;config=1490"
                        // Example fmtp is ""96 streamtype=5;profile-level-id=1;mode=AAC-hbr;sizelength=13;indexlength=3;indexdeltalength=3;config=1210"
                        if (audio && _audioCodec.Contains("MPEG4-GENERIC") && fmtp["mode"].ToLower().Equals("aac-hbr"))
                        {
                            // Extract config (eg 0x1490 or 0x1210)
                            _aacPayload = new Rtsp.AACPayload(fmtp["config"]);
                        }

                        // Send the SETUP RTSP command if we have a matching Payload Decoder
                        if (video && _videoPayload == -1)
                        {
                            continue;
                        }

                        if (audio && _audioPayload == -1)
                        {
                            continue;
                        }

                        SetupCompleted?.Invoke(_videoCodec, _videoPayload, _audioCodec, _audioPayload);

                        RtspTransport transport = null;

                        if (_rtpTransport == RTP_TRANSPORT.TCP)
                        {
                            // Server interleaves the RTP packets over the RTSP connection
                            // Example for TCP mode (RTP over RTSP)   Transport: RTP/AVP/TCP;interleaved=0-1
                            if (video)
                            {
                                _videoDataChannel = next_free_rtp_channel;
                                _videoRtcpChannel = next_free_rtcp_channel;
                            }
                            if (audio)
                            {
                                _audioDataChannel = next_free_rtp_channel;
                                _audioRtcpChannel = next_free_rtcp_channel;
                            }
                            transport = new RtspTransport()
                            {
                                LowerTransport = RtspTransport.LowerTransportType.TCP,
                                Interleaved = new PortCouple(next_free_rtp_channel, next_free_rtcp_channel), // Eg Channel 0 for RTP video data. Channel 1 for RTCP status reports
                            };

                            next_free_rtp_channel += 2;
                            next_free_rtcp_channel += 2;
                        }
                        if (_rtpTransport == RTP_TRANSPORT.UDP)
                        {
                            int rtp_port = 0;
                            int rtcp_port = 0;

                            // Server sends the RTP packets to a Pair of UDP Ports (one for data, one for rtcp control messages)
                            // Example for UDP mode                   Transport: RTP/AVP;unicast;client_port=8000-8001
                            if (video)
                            {
                                _videoDataChannel = _videoUdpPair.dataPort;     // Used in DataReceived event handler
                                _videoRtcpChannel = _videoUdpPair.controlPort;  // Used in DataReceived event handler
                                rtp_port = _videoUdpPair.dataPort;
                                rtcp_port = _videoUdpPair.controlPort;
                            }

                            if (audio)
                            {
                                _audioDataChannel = _audioUdpPair.dataPort;     // Used in DataReceived event handler
                                _audioRtcpChannel = _audioUdpPair.controlPort;  // Used in DataReceived event handler
                                rtp_port = _audioUdpPair.dataPort;
                                rtcp_port = _audioUdpPair.controlPort;
                            }

                            transport = new RtspTransport()
                            {
                                LowerTransport = RtspTransport.LowerTransportType.UDP,
                                IsMulticast = false,
                                ClientPort = new PortCouple(rtp_port, rtcp_port), // a UDP Port for data (video or audio). a UDP Port for RTCP status reports
                            };
                        }
                        if (_rtpTransport == RTP_TRANSPORT.MULTICAST)
                        {
                            // Server sends the RTP packets to a Pair of UDP ports (one for data, one for rtcp control messages)
                            // using Multicast Address and Ports that are in the reply to the SETUP message
                            // Example for MULTICAST mode     Transport: RTP/AVP;multicast
                            if (video)
                            {
                                _videoDataChannel = 0; // we get this information in the SETUP message reply
                                _videoRtcpChannel = 0; // we get this information in the SETUP message reply
                            }

                            if (audio)
                            {
                                _audioDataChannel = 0; // we get this information in the SETUP message reply
                                _audioRtcpChannel = 0; // we get this information in the SETUP message reply
                            }

                            transport = new RtspTransport()
                            {
                                LowerTransport = RtspTransport.LowerTransportType.UDP,
                                IsMulticast = true
                            };
                        }

                        // Generate SETUP messages
                        RtspRequestSetup setup_message = new RtspRequestSetup();
                        setup_message.RtspUri = new Uri(control);
                        setup_message.AddTransport(transport);
                        if (_authType != null)
                        {
                            AddAuthorization(setup_message, _userName, _password, _authType, _realm, _nonce, _rtspUrl);
                        }

                        // Add SETUP message to list of mesages to send
                        _setupMessages.Add(setup_message);
                    }
                }

                // Send the FIRST SETUP message and remove it from the list of Setup Messages
                _rtspClient.SendMessage(_setupMessages[0]);
                _setupMessages.RemoveAt(0);
            }

            // If we get a reply to SETUP (which was our third command), then we
            // (i) check if the Interleaved Channel numbers have been modified by the camera (eg Panasonic cameras)
            // (ii) check if we have any more SETUP commands to send out (eg if we are doing SETUP for Video and Audio)
            // (iii) send a PLAY command if all the SETUP command have been sent
            if (message.OriginalRequest != null && message.OriginalRequest is RtspRequestSetup)
            {
                // Got Reply to SETUP
                if (message.IsOk == false)
                {
                    _logger.LogDebug("Got Error in SETUP Reply " + message.ReturnCode + " " + message.ReturnMessage);
                    return;
                }

                _logger.LogDebug("Got reply from Setup. Session is " + message.Session);

                _session = message.Session; // Session value used with Play, Pause, Teardown and and additional Setups
                if (message.Timeout > 0 && message.Timeout > _keepaliveTimer.Interval / 1000)
                {
                    _keepaliveTimer.Interval = message.Timeout * 1000 / 2;
                }

                // Check the Transport header
                if (message.Headers.ContainsKey(RtspHeaderNames.Transport))
                {
                    RtspTransport transport = RtspTransport.Parse(message.Headers[RtspHeaderNames.Transport]);

                    // Check if Transport header includes Multicast
                    if (transport.IsMulticast)
                    {
                        String multicast_address = transport.Destination;
                        _videoDataChannel = transport.Port.First;
                        _videoRtcpChannel = transport.Port.Second;

                        // Create the Pair of UDP Sockets in Multicast mode
                        _videoUdpPair = new Rtsp.UDPSocket(multicast_address, _videoDataChannel, multicast_address, _videoRtcpChannel);
                        _videoUdpPair.DataReceived += Rtp_DataReceived;
                        _videoUdpPair.Start();

                        // TODO - Need to set audio_udp_pair for Multicast
                    }

                    // check if the requested Interleaved channels have been modified by the camera
                    // in the SETUP Reply (Panasonic have a camera that does this)
                    if (transport.LowerTransport == RtspTransport.LowerTransportType.TCP)
                    {
                        if (message.OriginalRequest.RtspUri == _videoUri)
                        {
                            _videoDataChannel = transport.Interleaved.First;
                            _videoRtcpChannel = transport.Interleaved.Second;
                        }

                        if (message.OriginalRequest.RtspUri == _audioUri)
                        {
                            _audioDataChannel = transport.Interleaved.First;
                            _audioRtcpChannel = transport.Interleaved.Second;
                        }
                    }
                }

                // Check if we have another SETUP command to send, then remote it from the list
                if (_setupMessages.Count > 0)
                {
                    // send the next SETUP message, after adding in the 'session'
                    RtspRequestSetup next_setup = _setupMessages[0];
                    next_setup.Session = _session;
                    _rtspClient.SendMessage(next_setup);
                    _setupMessages.RemoveAt(0);
                }

                else
                {
                    // Send PLAY
                    RtspRequest play_message = new RtspRequestPlay();
                    play_message.RtspUri = new Uri(_rtspUrl);
                    play_message.Session = _session;
                    
                    if (_authType != null)
                    {
                        AddAuthorization(play_message, _userName, _password, _authType, _realm, _nonce, _rtspUrl);
                    }

                    _rtspClient.SendMessage(play_message);
                }
            }

            // If we get a reply to PLAY (which was our fourth command), then we should have video being received
            if (message.OriginalRequest != null && message.OriginalRequest is RtspRequestPlay)
            {
                // Got Reply to PLAY
                if (message.IsOk == false)
                {
                    _logger.LogDebug("Got Error in PLAY Reply " + message.ReturnCode + " " + message.ReturnMessage);
                    return;
                }

                _logger.LogDebug("Got reply from Play  " + message.Command);
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Send Keepalive message
            // The ONVIF Standard uses SET_PARAMETER as "an optional method to keep an RTSP session alive"
            // RFC 2326 (RTSP Standard) says "GET_PARAMETER with no entity body may be used to test client or server liveness("ping")"

            // This code uses GET_PARAMETER (unless OPTIONS report it is not supported, and then it sends OPTIONS as a keepalive)
            if (_serverSupportsGetParameter)
            {
                RtspRequest getparam_message = new RtspRequestGetParameter();
                getparam_message.RtspUri = new Uri(_rtspUrl);
                getparam_message.Session = _session;

                if (_authType != null)
                {
                    AddAuthorization(getparam_message, _userName, _password, _authType, _realm, _nonce, _rtspUrl);
                }

                _rtspClient.SendMessage(getparam_message);
            }
            else
            {
                RtspRequest options_message = new RtspRequestOptions();
                options_message.RtspUri = new Uri(_rtspUrl);

                if (_authType != null)
                {
                    AddAuthorization(options_message, _userName, _password, _authType, _realm, _nonce, _rtspUrl);
                }

                _rtspClient.SendMessage(options_message);
            }
        }

        private void AddAuthorization(RtspMessage message, string username, string password, string auth_type, string realm, string nonce, string url)
        {
            if (username == null || username.Length == 0)
            {
                return;
            }

            if (password == null || password.Length == 0)
            {
                return;
            }

            if (realm == null || realm.Length == 0)
            {
                return;
            }

            if (auth_type.Equals("Digest") && (nonce == null || nonce.Length == 0))
            {
                return;
            }

            if (auth_type.Equals("Basic"))
            {
                byte[] credentials = Encoding.UTF8.GetBytes(username + ":" + password);
                string credentials_base64 = Convert.ToBase64String(credentials);
                string basic_authorization = "Basic " + credentials_base64;

                message.Headers.Add(RtspHeaderNames.Authorization, basic_authorization);
            }
            else if (auth_type.Equals("Digest"))
            {
                string method = message.Method; // DESCRIBE, SETUP, PLAY etc

                using (MD5 md5 = MD5.Create())
                {
                    string hashA1 = CalculateMD5Hash(md5, username + ":" + realm + ":" + password);
                    string hashA2 = CalculateMD5Hash(md5, method + ":" + url);
                    string response = CalculateMD5Hash(md5, hashA1 + ":" + nonce + ":" + hashA2);

                    const string quote = "\"";
                    string digest_authorization = "Digest username=" + quote + username + quote + ", "
                        + "realm=" + quote + realm + quote + ", "
                        + "nonce=" + quote + nonce + quote + ", "
                        + "uri=" + quote + url + quote + ", "
                        + "response=" + quote + response + quote;

                    message.Headers.Add(RtspHeaderNames.Authorization, digest_authorization);
                }
            }
        }

        private static string CalculateMD5Hash(MD5 md5_session, string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = md5_session.ComputeHash(inputBytes);

            StringBuilder output = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                output.Append(hash[i].ToString("x2"));
            }

            return output.ToString();
        }
    }
}
