using TLinkProxy.Abstractions;
using Shared.Classes;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using static TLinkProxy.Base.Constants;

namespace TLinkProxy.Services
{
    public sealed class SessionRegistry : ISessionRegistry
    {
        private const string GuidFormat = "n";
        private readonly ConcurrentDictionary<string, string> _sessions = new(StringComparer.Ordinal);
        private readonly object _gate = new();

        public string GetOrCreateSessionId(string explicitSessionId, string apiKey, string clientIp, string userAgent)
        {
            string identityKey = ComputeIdentityKey(apiKey, clientIp, userAgent);

            using (TimedLock.Lock(_gate))
            {
                if (Guid.TryParse(explicitSessionId, out _))
                {
                    _sessions[identityKey] = explicitSessionId;
                    return explicitSessionId;
                }

                if (!_sessions.TryGetValue(identityKey, out string sessionId))
                {
                    sessionId = Guid.NewGuid().ToString(GuidFormat);
                    _sessions[identityKey] = sessionId;
                }

                return sessionId;
            }
        }

        private static string ComputeIdentityKey(string apiKey, string clientIp, string userAgent)
        {
            string raw = string.Concat(
                apiKey ?? String.Empty,
                NewLine,
                clientIp ?? String.Empty,
                NewLine,
                userAgent ?? String.Empty);

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash);
        }
    }
}
