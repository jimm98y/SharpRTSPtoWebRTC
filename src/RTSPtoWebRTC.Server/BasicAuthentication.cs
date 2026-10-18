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

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace RTSPtoWebRTC.Server
{
    /// <summary>
    /// The users of HTTP Basic authentication (RFC 7617), as "BasicAuthentication" of appsettings.json has them: of each name,
    /// its password - a hash of <see cref="BasicPasswords.Hash"/>, "pbkdf2-sha256$iterations$salt$hash", or the password as it is.
    /// </summary>
    public sealed class BasicAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string Scheme = "Basic";

        /// <summary>The realm the browser's prompt names.</summary>
        public string Realm { get; set; } = "RTSPtoWebRTC";

        public Dictionary<string, string> Users { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// HTTP Basic authentication (RFC 7617): the user and password of each request's Authorization header, checked against the
    /// users of the options. Of none, or of a wrong one, a 401 of a challenge: the browser prompts once, and sends them of each
    /// request after it.
    /// </summary>
    /// <remarks>Over HTTP the password goes as it is, of base64 alone: of anything but the loopback, HTTPS is to be used.</remarks>
    public sealed class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
    {
        public BasicAuthenticationHandler(IOptionsMonitor<BasicAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var values) || !AuthenticationHeaderValue.TryParse(values.ToString(), out var header))
                return Task.FromResult(AuthenticateResult.NoResult());
            if (!string.Equals(header.Scheme, BasicAuthenticationOptions.Scheme, StringComparison.OrdinalIgnoreCase) || header.Parameter == null)
                return Task.FromResult(AuthenticateResult.NoResult());

            string user, password;
            try
            {
                // RFC 7617 2.1: user-id ":" password, of UTF-8; the user-id of no colon
                string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
                int colon = credentials.IndexOf(':');
                if (colon < 0)
                    return Task.FromResult(AuthenticateResult.Fail("Malformed credentials."));
                user = credentials.Substring(0, colon);
                password = credentials.Substring(colon + 1);
            }
            catch (FormatException)
            {
                return Task.FromResult(AuthenticateResult.Fail("Malformed credentials."));
            }

            // an unknown user checked against a hash all the same, of the same time a known one's takes
            bool known = Options.Users.TryGetValue(user, out string stored);
            bool valid = BasicPasswords.Verify(password, known ? stored : BasicPasswords.Unknown);
            if (!known || !valid)
            {
                Logger.LogWarning("Basic authentication of {RemoteIp} failed.", Context.Connection.RemoteIpAddress);
                return Task.FromResult(AuthenticateResult.Fail("Invalid user name or password."));
            }

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            Response.Headers["WWW-Authenticate"] = $"Basic realm=\"{Options.Realm.Replace("\"", "")}\", charset=\"UTF-8\"";
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// The passwords of the users: kept as PBKDF2-SHA256 hashes (RFC 8018), each of a salt of its own, compared in constant time.
    /// </summary>
    public static class BasicPasswords
    {
        private const string Prefix = "pbkdf2-sha256";
        private const int Iterations = 210000;
        private const int SaltLength = 16;
        private const int HashLength = 32;

        // what an unknown user's password is checked against, as long as a known one's check takes
        internal static readonly string Unknown = Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)));

        /// <summary>The hash of the password, to keep in appsettings.json: "pbkdf2-sha256$210000$salt$hash".</summary>
        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltLength);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, HashLength);
            return $"{Prefix}${Iterations.ToString(CultureInfo.InvariantCulture)}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>Whether the password is the one stored: of its hash, or - of no hash - of the password itself.</summary>
        public static bool Verify(string password, string stored)
        {
            if (password == null || string.IsNullOrEmpty(stored))
                return false;
            byte[] given = Encoding.UTF8.GetBytes(password);
            if (!stored.StartsWith(Prefix + "$", StringComparison.Ordinal))
            {
                // of the password as it is: compared of their hashes, of one length, so the time says nothing of either
                return CryptographicOperations.FixedTimeEquals(SHA256.HashData(given), SHA256.HashData(Encoding.UTF8.GetBytes(stored)));
            }

            string[] parts = stored.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int iterations) || iterations < 1 || iterations > 10_000_000)
                return false;
            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(given, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
