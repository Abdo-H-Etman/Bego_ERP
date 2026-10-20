using System.Security.Cryptography;
using System.Text;

namespace Erp.Application.Abstractions.Security;

public interface IHmacService
{
    string ComputeSignature(string payload, string secretKey, long timestamp);
    bool ValidateSignature(string payload, string secretKey, string signature, long timestamp, TimeSpan? tolerance = null);
}

public class HmacService : IHmacService
{
    private static readonly TimeSpan DefaultTolerance = TimeSpan.FromMinutes(5);

    public string ComputeSignature(string payload, string secretKey, long timestamp)
    {
        var rawData = $"{timestamp}:{payload}";
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var dataBytes = Encoding.UTF8.GetBytes(rawData);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool ValidateSignature(string payload, string secretKey, string signature, long timestamp, TimeSpan? tolerance = null)
    {
        var maxDrift = tolerance ?? DefaultTolerance;
        var currentUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Anti-replay protection
        if (Math.Abs(currentUnix - timestamp) > maxDrift.TotalSeconds)
        {
            return false;
        }

        var expectedSignature = ComputeSignature(payload, secretKey, timestamp);

        var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
        var actualBytes = Encoding.UTF8.GetBytes(signature.ToLowerInvariant());

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
