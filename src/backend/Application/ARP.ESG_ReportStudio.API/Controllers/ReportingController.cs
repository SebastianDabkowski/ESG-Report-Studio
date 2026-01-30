using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api")]
public sealed class ReportingController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    private readonly INotificationService _notificationService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IDocxExportService _docxExportService;

    public ReportingController(
        InMemoryReportStore store, 
        INotificationService notificationService,
        IPdfExportService pdfExportService,
        IDocxExportService docxExportService)
    {
        _store = store;
        _notificationService = notificationService;
        _pdfExportService = pdfExportService;
        _docxExportService = docxExportService;
    }

    [HttpGet("periods")]
    public ActionResult<IReadOnlyList<ReportingPeriod>> GetPeriods()
    {
        return Ok(_store.GetPeriods());
    }

    [HttpPost("periods")]
    public ActionResult<ReportingDataSnapshot> CreatePeriod([FromBody] CreateReportingPeriodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.StartDate)
            || string.IsNullOrWhiteSpace(request.EndDate)
            || string.IsNullOrWhiteSpace(request.OwnerId)
            || string.IsNullOrWhiteSpace(request.OwnerName))
        {
            return BadRequest("Name, dates, and owner info are required.");
        }

        var (isValid, errorMessage, snapshot) = _store.ValidateAndCreatePeriod(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(snapshot);
    }

    [HttpPut("periods/{id}")]
    public ActionResult<ReportingPeriod> UpdatePeriod(string id, [FromBody] UpdateReportingPeriodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.StartDate)
            || string.IsNullOrWhiteSpace(request.EndDate))
        {
            return BadRequest("Name and dates are required.");
        }

        var (isValid, errorMessage, period) = _store.ValidateAndUpdatePeriod(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(period);
    }

    [HttpGet("periods/{id}/has-started")]
    public ActionResult<bool> HasReportingStarted(string id)
    {
        return Ok(_store.HasReportingStarted(id));
    }

    [HttpGet("sections")]
    public ActionResult<IReadOnlyList<ReportSection>> GetSections([FromQuery] string? periodId)
    {
        return Ok(_store.GetSections(periodId));
    }

    [HttpGet("section-summaries")]
    public ActionResult<IReadOnlyList<SectionSummary>> GetSectionSummaries([FromQuery] string? periodId)
    {
        return Ok(_store.GetSectionSummaries(periodId));
    }

    [HttpGet("reporting-data")]
    public ActionResult<ReportingDataSnapshot> GetReportingData()
    {
        return Ok(_store.GetSnapshot());
    }

    [HttpPut("sections/{id}/owner")]
    public async Task<ActionResult<ReportSection>> UpdateSectionOwner(string id, [FromBody] UpdateSectionOwnerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            return BadRequest(new { error = "UpdatedBy is required." });
        }

        var (isValid, errorMessage, result) = _store.UpdateSectionOwner(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        // Send notifications after successful update
        if (result != null && result.Section != null && result.ChangedBy != null)
        {
            // Send removal notification to old owner if they exist and are different from new owner
            if (result.OldOwner != null && result.NewOwner?.Id != result.OldOwner.Id)
            {
                await _notificationService.SendSectionRemovedNotificationAsync(
                    result.Section, result.OldOwner, result.ChangedBy, request.ChangeNote);
            }
            
            // Send assignment notification to new owner if they exist and are different from old owner
            if (result.NewOwner != null && result.OldOwner?.Id != result.NewOwner.Id)
            {
                await _notificationService.SendSectionAssignedNotificationAsync(
                    result.Section, result.NewOwner, result.ChangedBy, request.ChangeNote);
            }
        }

        return Ok(result?.Section);
    }

    [HttpPost("sections/bulk-owner")]
    public async Task<ActionResult<BulkUpdateSectionOwnerResult>> UpdateSectionOwnersBulk([FromBody] BulkUpdateSectionOwnerRequest request)
    {
        if (request.SectionIds == null || request.SectionIds.Count == 0)
        {
            return BadRequest(new { error = "SectionIds are required." });
        }

        if (string.IsNullOrWhiteSpace(request.OwnerId) || string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            return BadRequest(new { error = "OwnerId and UpdatedBy are required." });
        }

        var result = _store.UpdateSectionOwnersBulk(request);
        
        // Send notifications for all successful updates concurrently
        if (result.OwnerUpdates.Count > 0)
        {
            var changedBy = _store.GetUser(request.UpdatedBy);
            if (changedBy != null)
            {
                var notificationTasks = new List<Task>();
                
                foreach (var update in result.OwnerUpdates)
                {
                    // Send removal notification to old owner if they exist and are different from new owner
                    if (update.OldOwner != null && update.OldOwner.Id != update.NewOwner?.Id)
                    {
                        notificationTasks.Add(_notificationService.SendSectionRemovedNotificationAsync(
                            update.Section, update.OldOwner, changedBy, request.ChangeNote));
                    }
                    
                    // Send assignment notification to new owner if they exist and are different from old owner
                    if (update.NewOwner != null && update.OldOwner?.Id != update.NewOwner.Id)
                    {
                        notificationTasks.Add(_notificationService.SendSectionAssignedNotificationAsync(
                            update.Section, update.NewOwner, changedBy, request.ChangeNote));
                    }
                }
                
                // Send all notifications concurrently
                await Task.WhenAll(notificationTasks);
            }
        }
        
        return Ok(result);
    }

    [HttpGet("responsibility-matrix")]
    public ActionResult<ResponsibilityMatrix> GetResponsibilityMatrix([FromQuery] string? periodId, [FromQuery] string? ownerFilter)
    {
        var matrix = _store.GetResponsibilityMatrix(periodId, ownerFilter);
        return Ok(matrix);
    }
    
    [HttpPost("periods/rollover")]
    public ActionResult<RolloverResult> RolloverPeriod([FromBody] RolloverRequest request)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.SourcePeriodId))
        {
            return BadRequest(new { error = "SourcePeriodId is required." });
        }
        
        if (string.IsNullOrWhiteSpace(request.TargetPeriodName))
        {
            return BadRequest(new { error = "TargetPeriodName is required." });
        }
        
        if (string.IsNullOrWhiteSpace(request.TargetPeriodStartDate))
        {
            return BadRequest(new { error = "TargetPeriodStartDate is required." });
        }
        
        if (string.IsNullOrWhiteSpace(request.TargetPeriodEndDate))
        {
            return BadRequest(new { error = "TargetPeriodEndDate is required." });
        }
        
        if (string.IsNullOrWhiteSpace(request.PerformedBy))
        {
            return BadRequest(new { error = "PerformedBy is required." });
        }
        
        // Perform rollover
        var (success, errorMessage, result) = _store.RolloverPeriod(request);
        
        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }
        
        return Ok(result);
    }
    
    [HttpGet("periods/{periodId}/rollover-audit")]
    public ActionResult<IReadOnlyList<RolloverAuditLog>> GetRolloverAuditLogs(string periodId)
    {
        var logs = _store.GetRolloverAuditLogs(periodId);
        return Ok(logs);
    }
    
    /// <summary>
    /// Get the rollover reconciliation report for a target period.
    /// Returns the mapping results showing which sections were successfully mapped
    /// and which sections could not be mapped with reasons and suggested actions.
    /// </summary>
    [HttpGet("periods/{periodId}/rollover-reconciliation")]
    public ActionResult<RolloverReconciliation> GetRolloverReconciliation(string periodId)
    {
        var reconciliation = _store.GetRolloverReconciliation(periodId);
        
        if (reconciliation == null)
        {
            return NotFound(new { message = $"No rollover reconciliation found for period '{periodId}'" });
        }
        
        return Ok(reconciliation);
    }
    
    /// <summary>
    /// Lock a reporting period to prevent accidental edits.
    /// </summary>
    [HttpPost("periods/{periodId}/lock")]
    public ActionResult<ReportingPeriod> LockPeriod(string periodId, [FromBody] LockPeriodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequest(new { error = "UserId and UserName are required." });
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { error = "A reason is required to lock a reporting period." });
        }

        var (isSuccess, errorMessage, period) = _store.LockPeriod(periodId, request);
        
        if (!isSuccess)
        {
            return BadRequest(new { error = errorMessage });
        }
        
        return Ok(period);
    }
    
    /// <summary>
    /// Unlock a locked reporting period (admin only). Requires a documented reason.
    /// </summary>
    [HttpPost("periods/{periodId}/unlock")]
    public ActionResult<ReportingPeriod> UnlockPeriod(string periodId, [FromBody] UnlockPeriodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequest(new { error = "UserId and UserName are required." });
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { error = "A reason is required to unlock a reporting period." });
        }

        // Note: In a real implementation, you would check if the user is an admin
        // For now, we assume the frontend has already validated this
        // You could get the user from the store and check their role here
        var user = _store.GetUser(request.UserId);
        var isAdmin = user?.Role == "admin";

        var (isSuccess, errorMessage, period) = _store.UnlockPeriod(periodId, request, isAdmin);
        
        if (!isSuccess)
        {
            return BadRequest(new { error = errorMessage });
        }
        
        return Ok(period);
    }
    
    /// <summary>
    /// Get audit trail for a specific period, including lock/unlock operations.
    /// </summary>
    [HttpGet("periods/{periodId}/audit")]
    public ActionResult<IReadOnlyList<AuditLogEntry>> GetPeriodAuditTrail(string periodId)
    {
        var auditTrail = _store.GetPeriodAuditTrail(periodId);
        return Ok(auditTrail);
    }
    
    /// <summary>
    /// Generate a report from the selected structure for a reporting period.
    /// Returns a structured report containing enabled sections in their defined order,
    /// with the latest data snapshot for each section.
    /// </summary>
    [HttpPost("periods/{periodId}/generate-report")]
    public ActionResult<GeneratedReport> GenerateReport(string periodId, [FromBody] GenerateReportRequest request)
    {
        // Ensure periodId from route matches request
        if (request.PeriodId != periodId)
        {
            request.PeriodId = periodId;
        }
        
        if (string.IsNullOrWhiteSpace(request.GeneratedBy))
        {
            return BadRequest(new { error = "GeneratedBy is required." });
        }
        
        var (isValid, errorMessage, report) = _store.GenerateReport(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }
        
        return Ok(report);
    }

    /// <summary>
    /// Preview the report for a given period with permission-based filtering.
    /// Sections without view permission are excluded from the preview.
    /// </summary>
    [HttpGet("periods/{periodId}/preview-report")]
    public ActionResult<GeneratedReport> PreviewReport(
        string periodId, 
        [FromQuery] string? userId,
        [FromQuery] string? sectionIds)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { error = "userId query parameter is required." });
        }

        // Parse section IDs if provided
        List<string>? sectionIdList = null;
        if (!string.IsNullOrWhiteSpace(sectionIds))
        {
            sectionIdList = sectionIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        // Create a request for report generation
        var request = new GenerateReportRequest
        {
            PeriodId = periodId,
            GeneratedBy = userId,
            SectionIds = sectionIdList,
            GenerationNote = "Preview"
        };

        var (isValid, errorMessage, report) = _store.GenerateReport(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        // Apply permission filtering - filter sections based on user permissions
        // For now, we'll use a simple rule: users can see sections they own or sections without an owner
        // In a production system, this would use a proper authorization service
        var filteredSections = report!.Sections.Where(s => 
            string.IsNullOrEmpty(s.Section.OwnerId) || 
            s.Section.OwnerId == userId ||
            s.Owner == null
        ).ToList();

        report.Sections = filteredSections;
        
        return Ok(report);
    }
    
    /// <summary>
    /// Export a generated report to PDF format with title page, table of contents, and page numbering.
    /// </summary>
    [HttpPost("periods/{periodId}/export-pdf")]
    public ActionResult ExportPdf(string periodId, [FromBody] ExportPdfRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GeneratedBy))
        {
            return BadRequest(new { error = "GeneratedBy is required." });
        }
        
        // Check if period exists
        var period = _store.GetPeriods().FirstOrDefault(p => p.Id == periodId);
        if (period == null)
        {
            return NotFound(new { error = $"Period with ID '{periodId}' not found." });
        }
        
        // Generate the report first
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = periodId,
            GeneratedBy = request.GeneratedBy,
            SectionIds = request.SectionIds,
            GenerationNote = "PDF Export"
        };
        
        var (isValid, errorMessage, report) = _store.GenerateReport(generateRequest);
        
        if (!isValid || report == null)
        {
            return BadRequest(new { error = errorMessage ?? "Failed to generate report." });
        }
        
        // Create PDF export options
        var options = new PdfExportOptions
        {
            IncludeTitlePage = request.IncludeTitlePage ?? true,
            IncludeTableOfContents = request.IncludeTableOfContents ?? true,
            IncludePageNumbers = request.IncludePageNumbers ?? true,
            VariantName = request.VariantName,
            IncludeAttachments = request.IncludeAttachments ?? false,
            UserId = request.GeneratedBy,
            MaxAttachmentSizeMB = request.MaxAttachmentSizeMB ?? 50
        };
        
        // Generate PDF
        byte[] pdfBytes;
        try
        {
            pdfBytes = _pdfExportService.GeneratePdf(report, options);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to generate PDF: {ex.Message}" });
        }
        
        // Generate filename
        var filename = _pdfExportService.GenerateFilename(report, request.VariantName);
        
        // Record export in history
        var exportEntry = new ExportHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            GenerationId = report.Id,
            PeriodId = periodId,
            Format = "pdf",
            FileName = filename,
            FileSize = pdfBytes.Length,
            FileChecksum = CalculateFileChecksum(pdfBytes),
            ExportedAt = DateTime.UtcNow.ToString("O"),
            ExportedBy = request.GeneratedBy,
            ExportedByName = report.GeneratedByName,
            VariantName = request.VariantName,
            IncludedTitlePage = options.IncludeTitlePage,
            IncludedTableOfContents = options.IncludeTableOfContents,
            IncludedAttachments = options.IncludeAttachments,
            DownloadCount = 0
        };
        _store.RecordExport(exportEntry);
        
        // Return PDF file
        return File(pdfBytes, "application/pdf", filename);
    }
    
    private static string CalculateFileChecksum(byte[] fileBytes)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(fileBytes);
        return Convert.ToBase64String(hash);
    }
    
    /// <summary>
    /// Export a generated report to DOCX format with proper heading styles, tables, and formatting.
    /// </summary>
    [HttpPost("periods/{periodId}/export-docx")]
    public ActionResult ExportDocx(string periodId, [FromBody] ExportDocxRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GeneratedBy))
        {
            return BadRequest(new { error = "GeneratedBy is required." });
        }
        
        // Check if period exists
        var period = _store.GetPeriods().FirstOrDefault(p => p.Id == periodId);
        if (period == null)
        {
            return NotFound(new { error = $"Period with ID '{periodId}' not found." });
        }
        
        // Generate the report first
        var generateRequest = new GenerateReportRequest
        {
            PeriodId = periodId,
            GeneratedBy = request.GeneratedBy,
            SectionIds = request.SectionIds,
            GenerationNote = "DOCX Export"
        };
        
        var (isValid, errorMessage, report) = _store.GenerateReport(generateRequest);
        
        if (!isValid || report == null)
        {
            return BadRequest(new { error = errorMessage ?? "Failed to generate report." });
        }
        
        // Create DOCX export options
        var options = new DocxExportOptions
        {
            IncludeTitlePage = request.IncludeTitlePage ?? true,
            IncludeTableOfContents = request.IncludeTableOfContents ?? true,
            IncludePageNumbers = request.IncludePageNumbers ?? true,
            VariantName = request.VariantName,
            IncludeAttachments = request.IncludeAttachments ?? false,
            UserId = request.GeneratedBy,
            MaxAttachmentSizeMB = request.MaxAttachmentSizeMB ?? 50
        };
        
        // Generate DOCX
        byte[] docxBytes;
        try
        {
            docxBytes = _docxExportService.GenerateDocx(report, options);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to generate DOCX: {ex.Message}" });
        }
        
        // Generate filename
        var filename = _docxExportService.GenerateFilename(report, request.VariantName);
        
        // Record export in history
        var exportEntry = new ExportHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            GenerationId = report.Id,
            PeriodId = periodId,
            Format = "docx",
            FileName = filename,
            FileSize = docxBytes.Length,
            FileChecksum = CalculateFileChecksum(docxBytes),
            ExportedAt = DateTime.UtcNow.ToString("O"),
            ExportedBy = request.GeneratedBy,
            ExportedByName = report.GeneratedByName,
            VariantName = request.VariantName,
            IncludedTitlePage = options.IncludeTitlePage,
            IncludedTableOfContents = options.IncludeTableOfContents,
            IncludedAttachments = options.IncludeAttachments,
            DownloadCount = 0
        };
        _store.RecordExport(exportEntry);
        
        // Return DOCX file
        return File(docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", filename);
    }
    
    /// <summary>
    /// Get generation history for a reporting period.
    /// </summary>
    [HttpGet("periods/{periodId}/generation-history")]
    public ActionResult<IReadOnlyList<GenerationHistoryEntry>> GetGenerationHistory(string periodId)
    {
        var period = _store.GetPeriods().FirstOrDefault(p => p.Id == periodId);
        if (period == null)
        {
            return NotFound(new { error = $"Period with ID '{periodId}' not found." });
        }
        
        var history = _store.GetGenerationHistory(periodId);
        return Ok(history);
    }
    
    /// <summary>
    /// Get a specific generation by ID.
    /// </summary>
    [HttpGet("generation-history/{generationId}")]
    public ActionResult<GenerationHistoryEntry> GetGeneration(string generationId)
    {
        var generation = _store.GetGeneration(generationId);
        if (generation == null)
        {
            return NotFound(new { error = $"Generation with ID '{generationId}' not found." });
        }
        
        return Ok(generation);
    }
    
    /// <summary>
    /// Mark a generation as final.
    /// </summary>
    [HttpPost("generation-history/{generationId}/mark-final")]
    public ActionResult<GenerationHistoryEntry> MarkGenerationFinal(string generationId, [FromBody] MarkGenerationFinalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequest(new { error = "UserId and UserName are required." });
        }
        
        request.GenerationId = generationId;
        var (isSuccess, errorMessage, entry) = _store.MarkGenerationAsFinal(request);
        
        if (!isSuccess)
        {
            return BadRequest(new { error = errorMessage });
        }
        
        return Ok(entry);
    }
    
    /// <summary>
    /// Compare two report generations.
    /// </summary>
    [HttpPost("generation-history/compare")]
    public ActionResult<GenerationComparison> CompareGenerations([FromBody] CompareGenerationsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Generation1Id) || string.IsNullOrWhiteSpace(request.Generation2Id))
        {
            return BadRequest(new { error = "Both Generation1Id and Generation2Id are required." });
        }
        
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return BadRequest(new { error = "UserId is required." });
        }
        
        var (isSuccess, errorMessage, comparison) = _store.CompareGenerations(request);
        
        if (!isSuccess)
        {
            return BadRequest(new { error = errorMessage });
        }
        
        return Ok(comparison);
    }
    
    /// <summary>
    /// Get export history for a reporting period.
    /// </summary>
    [HttpGet("periods/{periodId}/export-history")]
    public ActionResult<IReadOnlyList<ExportHistoryEntry>> GetExportHistory(string periodId)
    {
        var period = _store.GetPeriods().FirstOrDefault(p => p.Id == periodId);
        if (period == null)
        {
            return NotFound(new { error = $"Period with ID '{periodId}' not found." });
        }
        
        var history = _store.GetExportHistory(periodId);
        return Ok(history);
    }
}

/// <summary>
/// Request to export a report to PDF.
/// </summary>
public sealed class ExportPdfRequest
{
    /// <summary>
    /// User ID generating the export.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional list of section IDs to include. If null, all enabled sections are included.
    /// </summary>
    public List<string>? SectionIds { get; set; }
    
    /// <summary>
    /// Optional variant name to include in the filename and title page.
    /// </summary>
    public string? VariantName { get; set; }
    
    /// <summary>
    /// Whether to include a title page. Default: true.
    /// </summary>
    public bool? IncludeTitlePage { get; set; }
    
    /// <summary>
    /// Whether to include a table of contents. Default: true.
    /// </summary>
    public bool? IncludeTableOfContents { get; set; }
    
    /// <summary>
    /// Whether to include page numbers. Default: true.
    /// </summary>
    public bool? IncludePageNumbers { get; set; }
    
    /// <summary>
    /// Whether to include evidence and attachments as an appendix. Default: false.
    /// </summary>
    public bool? IncludeAttachments { get; set; }
    
    /// <summary>
    /// Maximum total size of attachments to include (in MB). Default: 50.
    /// </summary>
    public int? MaxAttachmentSizeMB { get; set; }
}

/// <summary>
/// Request to export a report to DOCX.
/// </summary>
public sealed class ExportDocxRequest
{
    /// <summary>
    /// User ID generating the export.
    /// </summary>
    public string GeneratedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional list of section IDs to include. If null, all enabled sections are included.
    /// </summary>
    public List<string>? SectionIds { get; set; }
    
    /// <summary>
    /// Optional variant name to include in the filename and title page.
    /// </summary>
    public string? VariantName { get; set; }
    
    /// <summary>
    /// Whether to include a title page. Default: true.
    /// </summary>
    public bool? IncludeTitlePage { get; set; }
    
    /// <summary>
    /// Whether to include a table of contents. Default: true.
    /// </summary>
    public bool? IncludeTableOfContents { get; set; }
    
    /// <summary>
    /// Whether to include page numbers. Default: true.
    /// </summary>
    public bool? IncludePageNumbers { get; set; }
    
    /// <summary>
    /// Whether to include evidence and attachments as an appendix. Default: false.
    /// </summary>
    public bool? IncludeAttachments { get; set; }
    
    /// <summary>
    /// Maximum total size of attachments to include (in MB). Default: 50.
    /// </summary>
    public int? MaxAttachmentSizeMB { get; set; }
}
