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

using SharpRTSPtoWebRTC.WebRTCProxy;
using System;
using System.Linq;

namespace SharpRTSPtoWebRTC.Tests
{
    /// <summary>The secret of a session: random of each, of 256 bits, of base64url, and taken of itself alone.</summary>
    [TestClass]
    public class SessionTokenTests
    {
        [TestMethod]
        public void EachSessionHasASecretOfItsOwn()
        {
            var tokens = Enumerable.Range(0, 100).Select(_ => SessionToken.Create()).ToList();

            Assert.AreEqual(100, tokens.Distinct().Count());
            Assert.IsTrue(tokens.All(t => t.Length == 43), "32 bytes of base64url, of no padding");
            Assert.IsTrue(tokens.All(t => t.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')), "safe in a header");
        }

        [TestMethod]
        public void OnlyTheSessionsOwnSecretMatches()
        {
            string token = SessionToken.Create();

            Assert.IsTrue(SessionToken.Matches(token, token));
            Assert.IsTrue(SessionToken.Matches(token, new string(token.ToCharArray())));
            Assert.IsFalse(SessionToken.Matches(token, SessionToken.Create()));
            Assert.IsFalse(SessionToken.Matches(token, token.Substring(0, 42)), "a prefix");
            Assert.IsFalse(SessionToken.Matches(token, token + "x"), "longer");
            Assert.IsFalse(SessionToken.Matches(token, ""));
            Assert.IsFalse(SessionToken.Matches(token, null), "of no header");
            Assert.IsFalse(SessionToken.Matches(null, token));
        }
    }
}
