using System.Security.Cryptography;
using System.Text;

namespace SD.ProjectName.Modules.Integrations.Application;

/// <summary>
/// Service for generating and verifying webhook signatures
/// </summary>
public class WebhookSignatureService
{
    /// <summary>
    /// Generate HMAC-SHA256 signature for a payload
    /// </summary>
    /// <param name="payload">The payload to sign</param>
    /// <param name="secret">The signing secret</param>
    /// <returns>Hex-encoded signature</returns>
    public string GenerateSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    /// <summary>
    /// Verify a webhook signature
    /// </summary>
    /// <param name="payload">The payload that was signed</param>
    /// <param name="signature">The signature to verify</param>
    /// <param name="secret">The signing secret</param>
    /// <returns>True if the signature is valid</returns>
    public bool VerifySignature(string payload, string signature, string secret)
    {
        var expectedSignature = GenerateSignature(payload, secret);
        return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Generate a new signing secret
    /// </summary>
    /// <returns>A cryptographically random signing secret</returns>
    public string GenerateSigningSecret()
    {
        var bytes = new byte[32]; // 256 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
    
    /// <summary>
    /// Generate a verification token for handshake
    /// </summary>
    /// <returns>A random verification token</returns>
    public string GenerateVerificationToken()
    {
        var bytes = new byte[16]; // 128 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
