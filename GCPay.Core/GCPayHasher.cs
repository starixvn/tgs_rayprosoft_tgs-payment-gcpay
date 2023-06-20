using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.GCPay
{
    public class GCPayHasher
    {
        private readonly ILogger<GCPayHasher> _logger;

        public GCPayHasher(ILogger<GCPayHasher> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ComputeHash(string key, IEnumerable<KeyValuePair<string, string?>> dict)
        {
            var content = string.Join(
                "&",
                dict.Where(kvp => string.IsNullOrWhiteSpace(kvp.Value) is false && kvp.Key is not "sign")
                    .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                    .Select(p => $"{p.Key}={p.Value}"));

            var fullText = $"{content}&key={key}";

            _logger.LogTrace("FullText: {fullText}", fullText);

            var data = MD5.HashData(Encoding.UTF8.GetBytes(fullText));

            return Convert.ToHexString(data).ToUpper();
        }
    }
}
