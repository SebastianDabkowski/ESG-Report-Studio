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
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "PeriodId is required.",
                Status = 400
            });
        }

        if (string.IsNullOrWhiteSpace(request.ValidatedBy))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "ValidatedBy is required.",
                Status = 400
            });
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
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "PeriodId is required.",
                Status = 400
            });
        }

        if (string.IsNullOrWhiteSpace(request.PublishedBy))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "PublishedBy is required.",
                Status = 400
            });
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
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = "Cannot publish report due to validation errors. Use OverrideValidation with justification to proceed anyway.",
                Status = 400,
                Extensions =
                {
                    ["validationResult"] = validationResult
                }
            });
        }

        // If override is used, require and validate justification
        if (request.OverrideValidation && validationResult.ErrorCount > 0)
        {
            if (string.IsNullOrWhiteSpace(request.OverrideJustification))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "OverrideJustification is required when overriding validation errors.",
                    Status = 400
                });
            }

            // Validate justification length and content
            if (request.OverrideJustification.Length > 2000)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "OverrideJustification must be less than 2000 characters.",
                    Status = 400
                });
            }
        }

        // Get the period
        var period = _store.GetPeriods().FirstOrDefault(p => p.Id == request.PeriodId);
        if (period == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Reporting period with ID '{request.PeriodId}' not found.",
                Status = 404
            });
        }

        // Note: In a real implementation, this would update the period status to "published"
        // For now, we just return success with the validation result

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
