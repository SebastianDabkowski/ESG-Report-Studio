using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/organization")]
public sealed class OrganizationController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public OrganizationController(InMemoryReportStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<Organization> GetOrganization()
    {
        var organization = _store.GetOrganization();
        if (organization == null)
        {
            return NotFound("Organization not configured.");
        }

        return Ok(organization);
    }

    [HttpPost]
    public ActionResult<Organization> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.LegalForm)
            || string.IsNullOrWhiteSpace(request.Country)
            || string.IsNullOrWhiteSpace(request.Identifier))
        {
            return BadRequest("Name, legal form, country, and identifier are required.");
        }

        if (request.Name.Length > 255 || request.LegalForm.Length > 100 
            || request.Country.Length > 100 || request.Identifier.Length > 100)
        {
            return BadRequest("Input fields exceed maximum length.");
        }

        // Validate coverage type
        if (request.CoverageType != "full" && request.CoverageType != "limited")
        {
            return BadRequest("Coverage type must be either 'full' or 'limited'.");
        }

        // Validate coverage: if limited, justification is required
        if (request.CoverageType == "limited" && string.IsNullOrWhiteSpace(request.CoverageJustification))
        {
            return BadRequest("Coverage justification is required when coverage type is limited.");
        }

        var existingOrg = _store.GetOrganization();
        if (existingOrg != null)
        {
            return Conflict("Organization already exists. Use PUT to update.");
        }

        var organization = _store.CreateOrganization(request);
        return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, organization);
    }

    [HttpPut("{id}")]
    public ActionResult<Organization> UpdateOrganization(string id, [FromBody] UpdateOrganizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.LegalForm)
            || string.IsNullOrWhiteSpace(request.Country)
            || string.IsNullOrWhiteSpace(request.Identifier))
        {
            return BadRequest("Name, legal form, country, and identifier are required.");
        }

        if (request.Name.Length > 255 || request.LegalForm.Length > 100 
            || request.Country.Length > 100 || request.Identifier.Length > 100)
        {
            return BadRequest("Input fields exceed maximum length.");
        }

        // Validate coverage type
        if (request.CoverageType != "full" && request.CoverageType != "limited")
        {
            return BadRequest("Coverage type must be either 'full' or 'limited'.");
        }

        // Validate coverage: if limited, justification is required
        if (request.CoverageType == "limited" && string.IsNullOrWhiteSpace(request.CoverageJustification))
        {
            return BadRequest("Coverage justification is required when coverage type is limited.");
        }

        var organization = _store.UpdateOrganization(id, request);
        if (organization == null)
        {
            return NotFound("Organization not found.");
        }

        return Ok(organization);
    }
}
