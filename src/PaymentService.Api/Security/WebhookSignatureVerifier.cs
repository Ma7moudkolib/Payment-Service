using System.Security.Cryptography;
using System.Text;

namespace PaymentService.Api.Security;

public static class WebhookSignatureVerifier
{
    public static bool IsValid(string payload, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(payload) ||
            string.IsNullOrWhiteSpace(signature) ||
            string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        try
        {
            var signatureBytes = Convert.FromHexString(NormalizeHexSignature(signature));
            var computedBytes = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                Encoding.UTF8.GetBytes(payload));

            return CryptographicOperations.FixedTimeEquals(signatureBytes, computedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string NormalizeHexSignature(string signature)
    {
        const string sha256Prefix = "sha256=";

        return signature.StartsWith(sha256Prefix, StringComparison.OrdinalIgnoreCase)
            ? signature[sha256Prefix.Length..]
            : signature;
    }
}
