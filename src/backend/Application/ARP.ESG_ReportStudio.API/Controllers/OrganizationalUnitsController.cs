using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/organizational-units")]
public sealed class OrganizationalUnitsController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public OrganizationalUnitsController(InMemoryReportStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<OrganizationalUnit>> GetOrganizationalUnits()
    {
        return Ok(_store.GetOrganizationalUnits());
    }

    [HttpGet("{id}")]
    public ActionResult<OrganizationalUnit> GetOrganizationalUnit(string id)
    {
        var unit = _store.GetOrganizationalUnit(id);
        if (unit == null)
        {
            return NotFound($"Organizational unit with ID '{id}' not found.");
        }

        return Ok(unit);
    }

    [HttpPost]
    public ActionResult<OrganizationalUnit> CreateOrganizationalUnit([FromBody] CreateOrganizationalUnitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CreatedBy))
        {
            return BadRequest("CreatedBy is required.");
        }

        if (request.Name.Length > 255)
        {
            return BadRequest("Name must be 255 characters or less.");
        }

        if (request.Description.Length > 1000)
        {
            return BadRequest("Description must be 1000 characters or less.");
        }

        try
        {
            var unit = _store.CreateOrganizationalUnit(request);
            return CreatedAtAction(nameof(GetOrganizationalUnit), new { id = unit.Id }, unit);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public ActionResult<OrganizationalUnit> UpdateOrganizationalUnit(string id, [FromBody] UpdateOrganizationalUnitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            return BadRequest("UpdatedBy is required.");
        }

        if (request.Name.Length > 255)
        {
            return BadRequest("Name must be 255 characters or less.");
        }

        if (request.Description.Length > 1000)
        {
            return BadRequest("Description must be 1000 characters or less.");
        }

        try
        {
            var unit = _store.UpdateOrganizationalUnit(id, request);
            if (unit == null)
            {
                return NotFound($"Organizational unit with ID '{id}' not found.");
            }

            return Ok(unit);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteOrganizationalUnit(string id, [FromQuery] string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            return BadRequest("deletedBy query parameter is required.");
        }

        try
        {
            var deleted = _store.DeleteOrganizationalUnit(id, deletedBy);
            if (!deleted)
            {
                return NotFound($"Organizational unit with ID '{id}' not found.");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
