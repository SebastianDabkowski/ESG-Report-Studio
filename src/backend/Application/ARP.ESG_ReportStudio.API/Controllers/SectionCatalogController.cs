using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/section-catalog")]
public sealed class SectionCatalogController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public SectionCatalogController(InMemoryReportStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<SectionCatalogItem>> GetSectionCatalog([FromQuery] bool includeDeprecated = false)
    {
        return Ok(_store.GetSectionCatalog(includeDeprecated));
    }

    [HttpGet("{id}")]
    public ActionResult<SectionCatalogItem> GetSectionCatalogItem(string id)
    {
        var item = _store.GetSectionCatalogItem(id);
        if (item == null)
        {
            return NotFound(new { error = "Section catalog item not found." });
        }

        return Ok(item);
    }

    [HttpPost]
    public ActionResult<SectionCatalogItem> CreateSectionCatalogItem([FromBody] CreateSectionCatalogItemRequest request)
    {
        var (isValid, errorMessage, item) = _store.CreateSectionCatalogItem(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetSectionCatalogItem), new { id = item!.Id }, item);
    }

    [HttpPut("{id}")]
    public ActionResult<SectionCatalogItem> UpdateSectionCatalogItem(string id, [FromBody] UpdateSectionCatalogItemRequest request)
    {
        var (isValid, errorMessage, item) = _store.UpdateSectionCatalogItem(id, request);
        
        if (!isValid)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        return Ok(item);
    }

    [HttpPost("{id}/deprecate")]
    public ActionResult DeprecateSectionCatalogItem(string id)
    {
        var (isValid, errorMessage) = _store.DeprecateSectionCatalogItem(id);
        
        if (!isValid)
        {
            if (errorMessage?.Contains("not found") == true)
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }

        return Ok(new { message = "Section has been deprecated successfully." });
    }
}
