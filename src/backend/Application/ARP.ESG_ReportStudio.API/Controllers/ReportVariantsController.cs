using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

/// <summary>
/// Controller for managing report variants - audience-specific report configurations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/variants")]
public sealed class ReportVariantsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public ReportVariantsController(InMemoryReportStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Get all report variants.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<ReportVariant>> GetVariants()
    {
        var variants = _store.GetVariants();
        return Ok(variants);
    }

    /// <summary>
    /// Get a specific report variant by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<ReportVariant> GetVariant(string id)
    {
        var variant = _store.GetVariant(id);
        if (variant == null)
        {
            return NotFound(new { error = $"Variant '{id}' not found." });
        }
        return Ok(variant);
    }

    /// <summary>
    /// Create a new report variant.
    /// </summary>
    [HttpPost]
    public ActionResult<ReportVariant> CreateVariant([FromBody] CreateVariantRequest request)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.AudienceType))
        {
            return BadRequest(new { error = "AudienceType is required." });
        }

        if (string.IsNullOrWhiteSpace(request.CreatedBy))
        {
            return BadRequest(new { error = "CreatedBy is required." });
        }

        var (isValid, errorMessage, variant) = _store.CreateVariant(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(variant);
    }

    /// <summary>
    /// Update an existing report variant.
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<ReportVariant> UpdateVariant(string id, [FromBody] UpdateVariantRequest request)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.AudienceType))
        {
            return BadRequest(new { error = "AudienceType is required." });
        }

        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            return BadRequest(new { error = "UpdatedBy is required." });
        }

        var (isValid, errorMessage, variant) = _store.UpdateVariant(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(variant);
    }

    /// <summary>
    /// Delete a report variant.
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteVariant(string id, [FromQuery] string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            return BadRequest(new { error = "deletedBy query parameter is required." });
        }

        var (isValid, errorMessage) = _store.DeleteVariant(id, deletedBy);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return NoContent();
    }

    /// <summary>
    /// Generate a report using a specific variant configuration.
    /// Applies variant rules to filter sections, redact sensitive fields, and exclude attachments.
    /// </summary>
    [HttpPost("generate")]
    public ActionResult<GeneratedReportVariant> GenerateVariant([FromBody] GenerateVariantRequest request)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.PeriodId))
        {
            return BadRequest(new { error = "PeriodId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.VariantId))
        {
            return BadRequest(new { error = "VariantId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.GeneratedBy))
        {
            return BadRequest(new { error = "GeneratedBy is required." });
        }

        var (isValid, errorMessage, variantReport) = _store.GenerateReportVariant(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(variantReport);
    }

    /// <summary>
    /// Compare multiple report variants to show differences in sections and fields.
    /// Returns a detailed comparison showing which sections and fields are included/excluded/redacted in each variant.
    /// </summary>
    [HttpPost("compare")]
    public ActionResult<VariantComparison> CompareVariants([FromBody] CompareVariantsRequest request)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.PeriodId))
        {
            return BadRequest(new { error = "PeriodId is required." });
        }

        if (request.VariantIds == null || request.VariantIds.Count < 2)
        {
            return BadRequest(new { error = "At least 2 variant IDs are required for comparison." });
        }

        if (string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            return BadRequest(new { error = "RequestedBy is required." });
        }

        var (isValid, errorMessage, comparison) = _store.CompareVariants(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(comparison);
    }
}
