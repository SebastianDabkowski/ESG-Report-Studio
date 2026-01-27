using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/organization")]
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

        var existingOrg = _store.GetOrganization();
        if (existingOrg != null)
        {
            return Conflict("Organization already exists. Use PUT to update.");
        }

        var organization = _store.CreateOrganization(request);
        return CreatedAtAction(nameof(GetOrganization), organization);
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

        var organization = _store.UpdateOrganization(id, request);
        if (organization == null)
        {
            return NotFound("Organization not found.");
        }

        return Ok(organization);
    }
}
