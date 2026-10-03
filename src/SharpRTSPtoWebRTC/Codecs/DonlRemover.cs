using System;

namespace SharpRTSPtoWebRTC.Codecs
{
    /// <summary>
    /// Takes the decoding order numbers out of H265 and H266 RTP payloads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A sender whose SDP has sprop-max-don-diff above 0 puts a DONL in front of every NAL unit it
    /// sends and a DOND in front of every further one in an aggregation packet. The browsers'
    /// depacketisers do not read those fields, so a payload forwarded as it came in decodes as
    /// garbage. Taking them out leaves the plain packetisation, which the browsers do read.
    /// </para>
    /// <para>
    /// The numbers are only there to put NAL units back into decoding order; dropping them assumes
    /// they were sent in that order already, which is what cameras do.
    /// </para>
    /// <see href="https://datatracker.ietf.org/doc/html/rfc7798#section-4.4" />
    /// <see href="https://datatracker.ietf.org/doc/html/rfc9328#section-4.3" />
    /// </remarks>
    internal static class DonlRemover
    {
        private const int PAYLOAD_HEADER_SIZE = 2;
        private const int DONL_SIZE = 2;
        private const int DOND_SIZE = 1;
        private const int NALU_SIZE_SIZE = 2;
        private const int FU_HEADER_SIZE = 1;
        private const byte FU_START_BIT = 0x80;

        private const int H265_AP = 48;
        private const int H265_FU = 49;
        private const int H265_PACI = 50;

        private const int H266_AP = 28;
        private const int H266_FU = 29;

        /// <summary>
        /// The H265 payload without its DONL/DOND fields, or null where it cannot be forwarded:
        /// it is malformed, or it is a PACI packet, which no browser reads either way.
        /// </summary>
        public static byte[] StripH265(byte[] payload)
        {
            if (payload == null || payload.Length < PAYLOAD_HEADER_SIZE)
                return null;

            int type = (payload[0] >> 1) & 0x3F;
            if (type == H265_PACI)
                return null;

            return Strip(payload, type, H265_AP, H265_FU);
        }

        /// <summary>
        /// The H266 payload without its DONL/DOND fields, or null where it is malformed.
        /// </summary>
        public static byte[] StripH266(byte[] payload)
        {
            if (payload == null || payload.Length < PAYLOAD_HEADER_SIZE)
                return null;

            int type = (payload[1] >> 3) & 0x1F;
            return Strip(payload, type, H266_AP, H266_FU);
        }

        private static byte[] Strip(byte[] payload, int type, int aggregationType, int fragmentType)
        {
            if (type == aggregationType)
                return StripAggregation(payload);

            if (type == fragmentType)
                return StripFragment(payload);

            // a single NAL unit: header, DONL, the rest of the unit
            if (payload.Length < PAYLOAD_HEADER_SIZE + DONL_SIZE)
                return null;

            return Remove(payload, PAYLOAD_HEADER_SIZE, DONL_SIZE);
        }

        private static byte[] StripFragment(byte[] payload)
        {
            int headerSize = PAYLOAD_HEADER_SIZE + FU_HEADER_SIZE;
            if (payload.Length < headerSize)
                return null;

            // only the first fragment of a unit carries the DONL
            if ((payload[PAYLOAD_HEADER_SIZE] & FU_START_BIT) == 0)
                return payload;

            if (payload.Length < headerSize + DONL_SIZE)
                return null;

            return Remove(payload, headerSize, DONL_SIZE);
        }

        private static byte[] StripAggregation(byte[] payload)
        {
            // payload header, then DONL, size, unit for the first unit and DOND, size, unit for each after it
            byte[] stripped = new byte[payload.Length];
            Buffer.BlockCopy(payload, 0, stripped, 0, PAYLOAD_HEADER_SIZE);

            int read = PAYLOAD_HEADER_SIZE;
            int written = PAYLOAD_HEADER_SIZE;
            bool first = true;

            while (read < payload.Length)
            {
                read += first ? DONL_SIZE : DOND_SIZE;
                first = false;

                if (read + NALU_SIZE_SIZE > payload.Length)
                    return null;

                int size = (payload[read] << 8) | payload[read + 1];
                if (size == 0 || read + NALU_SIZE_SIZE + size > payload.Length)
                    return null;

                Buffer.BlockCopy(payload, read, stripped, written, NALU_SIZE_SIZE + size);
                read += NALU_SIZE_SIZE + size;
                written += NALU_SIZE_SIZE + size;
            }

            if (written == PAYLOAD_HEADER_SIZE)
                return null; // an aggregation packet with nothing in it

            Array.Resize(ref stripped, written);
            return stripped;
        }

        private static byte[] Remove(byte[] payload, int offset, int count)
        {
            byte[] stripped = new byte[payload.Length - count];
            Buffer.BlockCopy(payload, 0, stripped, 0, offset);
            Buffer.BlockCopy(payload, offset + count, stripped, offset, payload.Length - offset - count);
            return stripped;
        }
    }
}
