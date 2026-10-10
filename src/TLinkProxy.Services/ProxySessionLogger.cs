using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using System.Collections.Concurrent;
using System.Text;

namespace TLinkProxy.Services
{
    public sealed class ProxySessionLogger : IProxySessionLogger, IDisposable
    {
        private readonly string _logDir;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly bool _logToFile;

        public ProxySessionLogger(IAiSettings settings)
        {
            _logToFile = settings.LogToFile;
            _logDir = Path.Combine(AppContext.BaseDirectory, Constants.LogsDirectoryName);

            if (_logToFile)
            {
                Directory.CreateDirectory(_logDir);
            }
        }

        public async Task LogAsync(string sessionId, string message)
        {
            string timestamp = DateTimeOffset.UtcNow.ToString(Constants.DateTimeRoundTripFormat);
            string line = $"[{timestamp}] {message}";

            // Always print to console
            Console.WriteLine(line);

            // Conditionally append to session file
            if (!_logToFile)
            {
                return;
            }

            SemaphoreSlim sem = _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
            string path = Path.Combine(_logDir, sessionId + Constants.SessionLogFileExtension);
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                await File.AppendAllTextAsync(path, line + Environment.NewLine).ConfigureAwait(false);
            }
            finally
            {
                sem.Release();
            }
        }

        public async Task LogBytesAsync(string sessionId, byte[] bytes, string label = null)
        {
            if (!_logToFile)
            {
                // Still log a summary to console even when file logging is off
                string summary = bytes != null ? $"{bytes.Length} {Constants.BytesLabel}" : Constants.TextSummaryNull;
                string labelText = String.IsNullOrEmpty(label) ? Constants.EmptyString : $" ({label})";
                await LogAsync(sessionId, $"[bytes{labelText}] {summary}").ConfigureAwait(false);
                return;
            }

            SemaphoreSlim sem = _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                if (IsLikelyText(bytes))
                {
                    string text = Encoding.UTF8.GetString(bytes);
                    string truncated = text.Length > Constants.MaxTruncatedTextLength ? String.Concat(text.AsSpan(0, Constants.MaxTruncatedTextLength), Constants.TruncatedSuffix) : text;
                    string labelText = String.IsNullOrEmpty(label) ? Constants.BytesLabel : label;
                    string header = $"[bytes:{labelText}] {truncated}";
                    await LogAsync(sessionId, header).ConfigureAwait(false);

                    // also append full text to session file
                    string path = Path.Combine(_logDir, sessionId + Constants.SessionLogFileExtension);
                    await File.AppendAllTextAsync(path, text + Environment.NewLine).ConfigureAwait(false);
                }
                else
                {
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    string fileName = sessionId + Constants.FileNameSeparator + timestamp + (String.IsNullOrEmpty(label) ? Constants.EmptyString : Constants.FileNameSeparator + SanitizeFileName(label)) + Constants.BinaryFileExtension;
                    string path = Path.Combine(_logDir, fileName);
                    await File.WriteAllBytesAsync(path, bytes).ConfigureAwait(false);
                    await LogAsync(sessionId, $"[bytes] wrote binary file: {fileName}").ConfigureAwait(false);
                }
            }
            finally
            {
                sem.Release();
            }
        }

        private static bool IsLikelyText(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return true;

            int control = 0;
            int total = Math.Min(bytes.Length, Constants.MaxTextControlThreshold);

            for (int i = 0; i < total; i++)
            {
                byte b = bytes[i];

                if (b == 0)
                    return false;

                if (b < Constants.MinBinaryControlValue || (b > Constants.MaxControlValue && b < Constants.MinPrintableAsciiValue))
                    control++;
            }

            return (double)control / total < Constants.TextControlThresholdRatio;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, Constants.CharUnderscore);
            }
            return name;
        }

        public void Dispose()
        {
            foreach (KeyValuePair<string, SemaphoreSlim> kv in _locks)
            {
                kv.Value?.Dispose();
            }

            _locks.Clear();
        }
    }
}
