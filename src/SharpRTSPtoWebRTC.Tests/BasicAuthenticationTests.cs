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

using RTSPtoWebRTC.Server;

namespace SharpRTSPtoWebRTC.Tests
{
    /// <summary>
    /// The passwords of the gateway's Basic authentication: a hash checks its password alone, each hash of a salt of its own,
    /// and a password kept as it is - or a hash not well formed - checks as it should.
    /// </summary>
    [TestClass]
    public class BasicAuthenticationTests
    {
        [TestMethod]
        public void AHashChecksItsPasswordAlone()
        {
            string hash = BasicPasswords.Hash("S3cret pass");

            StringAssert.StartsWith(hash, "pbkdf2-sha256$210000$");
            Assert.IsTrue(BasicPasswords.Verify("S3cret pass", hash));
            Assert.IsFalse(BasicPasswords.Verify("S3cret pas", hash));
            Assert.IsFalse(BasicPasswords.Verify("", hash));
            Assert.IsFalse(BasicPasswords.Verify(null, hash));
        }

        [TestMethod]
        public void EachHashIsOfASaltOfItsOwn()
        {
            Assert.AreNotEqual(BasicPasswords.Hash("same"), BasicPasswords.Hash("same"));
        }

        [TestMethod]
        public void APasswordKeptAsItIsChecksAsItIs()
        {
            Assert.IsTrue(BasicPasswords.Verify("letmein", "letmein"));
            Assert.IsFalse(BasicPasswords.Verify("letmeout", "letmein"));
            Assert.IsFalse(BasicPasswords.Verify("letmein", ""));
        }

        [TestMethod]
        [DataRow("pbkdf2-sha256$x$AAAA$AAAA")]
        [DataRow("pbkdf2-sha256$0$AAAA$AAAA")]
        [DataRow("pbkdf2-sha256$99999999$AAAA$AAAA")]
        [DataRow("pbkdf2-sha256$1000$!!$AAAA")]
        [DataRow("pbkdf2-sha256$1000$AAAA")]
        public void AHashNotWellFormedChecksNothing(string stored)
        {
            Assert.IsFalse(BasicPasswords.Verify("anything", stored));
        }
    }
}
