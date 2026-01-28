using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for consistency validation and publication control.
/// </summary>
[ApiController]
[Route("api/consistency")]
public sealed class ConsistencyController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public ConsistencyController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Runs consistency validation on a reporting period.
    /// Checks for missing required fields, invalid units, contradictory statements, and period coverage.
    /// </summary>
    /// <param name="request">Validation request containing period ID and optional rule types.</param>
    /// <returns>Validation result with issues and publication status.</returns>
    [HttpPost("validate")]
    public ActionResult<ValidationResult> ValidateConsistency([FromBody] RunValidationRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.PeriodId))
        {
            return BadRequest(new { error = "PeriodId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.ValidatedBy))
        {
            return BadRequest(new { error = "ValidatedBy is required." });
        }

        var result = _store.RunConsistencyValidation(request);
        return Ok(result);
    }

    /// <summary>
    /// Attempts to publish a reporting period.
    /// Runs validation first and blocks publication if errors exist (unless override is specified).
    /// </summary>
    /// <param name="request">Publication request with optional override.</param>
    /// <returns>Publication result or validation failure.</returns>
    [HttpPost("publish")]
    public ActionResult<PublishReportResult> PublishReport([FromBody] PublishReportRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.PeriodId))
        {
            return BadRequest(new { error = "PeriodId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.PublishedBy))
        {
            return BadRequest(new { error = "PublishedBy is required." });
        }

        // Run validation first
        var validationResult = _store.RunConsistencyValidation(new RunValidationRequest
        {
            PeriodId = request.PeriodId,
            ValidatedBy = request.PublishedBy
        });

        // Check if publication is allowed
        if (!validationResult.CanPublish && !request.OverrideValidation)
        {
            return BadRequest(new
            {
                error = "Cannot publish report due to validation errors. Use OverrideValidation with justification to proceed anyway.",
                validationResult = validationResult
            });
        }

        // If override is used, require justification
        if (request.OverrideValidation && validationResult.ErrorCount > 0)
        {
            if (string.IsNullOrWhiteSpace(request.OverrideJustification))
            {
                return BadRequest(new { error = "OverrideJustification is required when overriding validation errors." });
            }
        }

        // Proceed with publication (simplified for now - just update period status)
        var period = _store.GetPeriods().FirstOrDefault(p => p.Id == request.PeriodId);
        if (period == null)
        {
            return NotFound(new { error = $"Reporting period with ID '{request.PeriodId}' not found." });
        }

        // Update period status to published
        var updateRequest = new UpdateReportingPeriodRequest
        {
            Name = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            ReportingMode = period.ReportingMode,
            ReportScope = period.ReportScope
        };

        var (isValid, errorMessage, updatedPeriod) = _store.ValidateAndUpdatePeriod(request.PeriodId, updateRequest);

        if (!isValid || updatedPeriod == null)
        {
            return BadRequest(new { error = errorMessage ?? "Failed to update period." });
        }

        // Create publication result
        var result = new PublishReportResult
        {
            PeriodId = request.PeriodId,
            PeriodName = period.Name,
            PublishedAt = DateTime.UtcNow.ToString("O"),
            PublishedBy = request.PublishedBy,
            ValidationOverridden = request.OverrideValidation && validationResult.ErrorCount > 0,
            OverrideJustification = request.OverrideJustification,
            ValidationResult = validationResult,
            Status = "published",
            Message = request.OverrideValidation && validationResult.ErrorCount > 0
                ? $"Report published with validation override. Justification: {request.OverrideJustification}"
                : "Report published successfully."
        };

        return Ok(result);
    }
}
