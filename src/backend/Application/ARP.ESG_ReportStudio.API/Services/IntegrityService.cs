using System.Security.Cryptography;
using System.Text;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for calculating and verifying integrity hashes of critical entities.
/// </summary>
public sealed class IntegrityService
{
    /// <summary>
    /// Calculates SHA-256 hash for a ReportingPeriod.
    /// Hash is calculated from: Id, Name, StartDate, EndDate, ReportingMode, ReportScope, OwnerId, OrganizationId
    /// </summary>
    public static string CalculateReportingPeriodHash(ReportingPeriod period)
    {
        var content = $"{period.Id}|{period.Name}|{period.StartDate}|{period.EndDate}|{period.ReportingMode}|{period.ReportScope}|{period.OwnerId}|{period.OrganizationId ?? ""}";
        return ComputeSha256Hash(content);
    }
    
    /// <summary>
    /// Calculates SHA-256 hash for a Decision.
    /// Hash is calculated from: Id, Version, Title, Context, DecisionText, Alternatives, Consequences
    /// </summary>
    public static string CalculateDecisionHash(Decision decision)
    {
        var content = $"{decision.Id}|{decision.Version}|{decision.Title}|{decision.Context}|{decision.DecisionText}|{decision.Alternatives}|{decision.Consequences}";
        return ComputeSha256Hash(content);
    }
    
    /// <summary>
    /// Calculates SHA-256 hash for a DecisionVersion.
    /// Hash is calculated from: DecisionId, Version, Title, Context, DecisionText, Alternatives, Consequences
    /// </summary>
    public static string CalculateDecisionVersionHash(DecisionVersion version)
    {
        var content = $"{version.DecisionId}|{version.Version}|{version.Title}|{version.Context}|{version.DecisionText}|{version.Alternatives}|{version.Consequences}";
        return ComputeSha256Hash(content);
    }
    
    /// <summary>
    /// Verifies that a ReportingPeriod's stored hash matches its current content.
    /// </summary>
    /// <returns>True if hash matches or no hash is stored, false if mismatch detected.</returns>
    public static bool VerifyReportingPeriodIntegrity(ReportingPeriod period)
    {
        if (string.IsNullOrEmpty(period.IntegrityHash))
        {
            return true; // No hash stored, can't verify
        }
        
        var currentHash = CalculateReportingPeriodHash(period);
        return currentHash == period.IntegrityHash;
    }
    
    /// <summary>
    /// Verifies that a Decision's stored hash matches its current content.
    /// </summary>
    /// <returns>True if hash matches or no hash is stored, false if mismatch detected.</returns>
    public static bool VerifyDecisionIntegrity(Decision decision)
    {
        if (string.IsNullOrEmpty(decision.IntegrityHash))
        {
            return true; // No hash stored, can't verify
        }
        
        var currentHash = CalculateDecisionHash(decision);
        return currentHash == decision.IntegrityHash;
    }
    
    /// <summary>
    /// Computes SHA-256 hash of the given string content.
    /// </summary>
    private static string ComputeSha256Hash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
