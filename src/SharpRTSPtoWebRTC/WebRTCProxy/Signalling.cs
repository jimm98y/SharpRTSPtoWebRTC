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

namespace SharpRTSPtoWebRTC.WebRTCProxy
{
    /// <summary>
    /// A session description as the browser's RTCSessionDescriptionInit has it: { "type": "offer", "sdp": "..." }.
    /// </summary>
    public sealed class SessionDescriptionInit
    {
        /// <summary>"offer" or "answer".</summary>
        public string Type { get; set; }

        public string Sdp { get; set; }

        /// <summary>
        /// Of an offer, the session's secret: what its answer and candidates are to carry, of the header
        /// <see cref="SessionToken.Header"/>, as no one else knows it. Null of an answer.
        /// </summary>
        public string Token { get; set; }
    }

    /// <summary>
    /// The secret of a session: 32 random bytes, of base64url, made by the server as it offers. The session's answer and
    /// candidates are taken of it alone - not of its id, which the page chooses and anyone may guess.
    /// </summary>
    public static class SessionToken
    {
        public const string Header = "X-Session-Token";

        public static string Create()
        {
            byte[] bytes = new byte[32];
            using (var random = System.Security.Cryptography.RandomNumberGenerator.Create())
                random.GetBytes(bytes);
            return System.Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        /// <summary>Whether the token given is the session's, compared in constant time.</summary>
        public static bool Matches(string expected, string given)
        {
            if (expected == null || given == null)
                return false;
            byte[] a = System.Text.Encoding.UTF8.GetBytes(expected);
            byte[] b = System.Text.Encoding.UTF8.GetBytes(given);
            int difference = a.Length ^ b.Length;
            for (int i = 0; i < a.Length; i++)
                difference |= a[i] ^ (i < b.Length ? b[i] : 0);
            return difference == 0;
        }
    }

    /// <summary>
    /// A candidate the browser trickles, as its RTCIceCandidateInit has it: "candidate:..." of its "a=candidate" line, and the
    /// media description it is of - one transport for all of them, as BUNDLE has it.
    /// </summary>
    public sealed class IceCandidateInit
    {
        public string Candidate { get; set; }

        public string SdpMid { get; set; }

        public int? SdpMLineIndex { get; set; }

        public string UsernameFragment { get; set; }
    }
}
