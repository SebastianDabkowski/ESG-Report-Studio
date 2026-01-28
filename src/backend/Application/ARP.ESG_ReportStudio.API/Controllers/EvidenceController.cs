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
            uploadedBy
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
}
