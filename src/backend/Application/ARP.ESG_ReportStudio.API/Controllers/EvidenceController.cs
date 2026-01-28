using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/evidence")]
public sealed class EvidenceController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public EvidenceController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get all evidence items, optionally filtered by section.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<Evidence>> GetEvidence([FromQuery] string? sectionId)
    {
        return Ok(_store.GetEvidence(sectionId));
    }

    /// <summary>
    /// Get a specific evidence item by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<Evidence> GetEvidenceById(string id)
    {
        var evidence = _store.GetEvidenceById(id);
        if (evidence == null)
        {
            return NotFound(new { error = $"Evidence with ID '{id}' not found." });
        }

        return Ok(evidence);
    }

    /// <summary>
    /// Create a new evidence item with a reference or URL.
    /// </summary>
    [HttpPost]
    public ActionResult<Evidence> CreateEvidence([FromBody] CreateEvidenceRequest request)
    {
        var (isValid, errorMessage, evidence) = _store.CreateEvidence(
            request.SectionId,
            request.Title,
            request.Description,
            null, // fileName - will be set during file upload
            null, // fileUrl - will be set during file upload
            request.SourceUrl,
            request.UploadedBy
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetEvidenceById), new { id = evidence!.Id }, evidence);
    }

    /// <summary>
    /// Upload a file as evidence for a section.
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<Evidence>> UploadEvidence(
        [FromForm] string sectionId,
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] string uploadedBy,
        [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required." });
        }

        // Validate file size (10MB max)
        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { error = "File size must not exceed 10MB." });
        }

        // Validate file type by content type
        var allowedContentTypes = new[]
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "text/csv",
            "image/png",
            "image/jpeg",
            "image/jpg"
        };

        if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "File type not allowed. Accepted formats: PDF, Word, Excel, CSV, PNG, JPEG." });
        }

        // Sanitize filename
        var sanitizedFileName = SanitizeFileName(file.FileName);
        
        // Calculate SHA-256 checksum
        string checksum;
        using (var stream = file.OpenReadStream())
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashBytes = await sha256.ComputeHashAsync(stream);
            checksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
        
        // For now, we'll create a mock file URL
        // In a real implementation, this would upload to Azure Blob Storage or similar
        var fileId = Guid.NewGuid();
        var fileUrl = $"/api/evidence/files/{fileId}";

        var (isValid, errorMessage, evidence) = _store.CreateEvidence(
            sectionId,
            title,
            description,
            sanitizedFileName,
            fileUrl,
            null, // sourceUrl
            uploadedBy,
            file.Length, // fileSize
            checksum,
            file.ContentType
        );

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        // In a real implementation, you would save the file here
        // await SaveFileAsync(file, fileId, sanitizedFileName);

        return CreatedAtAction(nameof(GetEvidenceById), new { id = evidence!.Id }, evidence);
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove path traversal sequences and dangerous characters
        var sanitized = Path.GetFileName(fileName);
        
        // Remove or replace invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        // Limit filename length
        if (sanitized.Length > 255)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExtension.Substring(0, 255 - extension.Length) + extension;
        }

        return sanitized;
    }

    /// <summary>
    /// Link evidence to a data point.
    /// </summary>
    [HttpPost("{evidenceId}/link")]
    public ActionResult LinkEvidenceToDataPoint(string evidenceId, [FromBody] LinkEvidenceRequest request)
    {
        var (isValid, errorMessage) = _store.LinkEvidenceToDataPoint(evidenceId, request.DataPointId);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Unlink evidence from a data point.
    /// </summary>
    [HttpPost("{evidenceId}/unlink")]
    public ActionResult UnlinkEvidenceFromDataPoint(string evidenceId, [FromBody] LinkEvidenceRequest request)
    {
        var (isValid, errorMessage) = _store.UnlinkEvidenceFromDataPoint(evidenceId, request.DataPointId);

        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Delete an evidence item.
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteEvidence(string id)
    {
        var deleted = _store.DeleteEvidence(id);
        if (!deleted)
        {
            return NotFound(new { error = $"Evidence with ID '{id}' not found." });
        }

        return NoContent();
    }

    /// <summary>
    /// Download an evidence file with access logging.
    /// </summary>
    [HttpPost("{id}/download")]
    public ActionResult DownloadEvidence(string id, [FromBody] DownloadEvidenceRequest request)
    {
        var evidence = _store.GetEvidenceById(id);
        if (evidence == null)
        {
            return NotFound(new { error = $"Evidence with ID '{id}' not found." });
        }

        // Check if evidence can be published (integrity check)
        if (!_store.CanPublishEvidence(id))
        {
            return BadRequest(new { 
                error = "Evidence file failed integrity check and cannot be downloaded.",
                integrityStatus = evidence.IntegrityStatus 
            });
        }

        // Log the access
        _store.LogEvidenceAccess(
            id, 
            request.UserId, 
            request.UserName, 
            "download", 
            request.Purpose
        );

        // In a real implementation, this would return the actual file from storage
        // For now, return the file URL
        return Ok(new { 
            fileUrl = evidence.FileUrl,
            fileName = evidence.FileName,
            message = "Access logged. In production, file would be streamed here."
        });
    }

    /// <summary>
    /// Validate evidence file integrity using checksum.
    /// </summary>
    [HttpPost("{id}/validate")]
    public ActionResult ValidateEvidence(string id, [FromBody] ValidateEvidenceRequest request)
    {
        var evidence = _store.GetEvidenceById(id);
        if (evidence == null)
        {
            return NotFound(new { error = $"Evidence with ID '{id}' not found." });
        }

        var (isValid, errorMessage) = _store.ValidateEvidenceIntegrity(id, request.Checksum);

        // Log the validation attempt
        _store.LogEvidenceAccess(
            id,
            request.UserId,
            request.UserName,
            "validate",
            $"Integrity validation: {(isValid ? "passed" : "failed")}"
        );

        if (!isValid)
        {
            return BadRequest(new { 
                error = errorMessage,
                integrityStatus = "failed"
            });
        }

        return Ok(new { 
            message = "Evidence file integrity validated successfully.",
            integrityStatus = "valid",
            checksum = evidence.Checksum
        });
    }

    /// <summary>
    /// Get access log for evidence files.
    /// </summary>
    [HttpGet("access-log")]
    public ActionResult<IReadOnlyList<EvidenceAccessLog>> GetAccessLog([FromQuery] string? evidenceId)
    {
        var logs = _store.GetEvidenceAccessLog(evidenceId);
        return Ok(logs);
    }
}

// Request models
public sealed class DownloadEvidenceRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Purpose { get; set; }
}

public sealed class ValidateEvidenceRequest
{
    public string Checksum { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
