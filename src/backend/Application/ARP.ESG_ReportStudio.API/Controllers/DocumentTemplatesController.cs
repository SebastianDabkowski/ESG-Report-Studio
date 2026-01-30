using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/document-templates")]
public sealed class DocumentTemplatesController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    
    public DocumentTemplatesController(InMemoryReportStore store)
    {
        _store = store;
    }
    
    /// <summary>
    /// Get all document templates
    /// </summary>
    [HttpGet]
    public ActionResult<List<DocumentTemplate>> GetTemplates([FromQuery] string? templateType = null)
    {
        if (!string.IsNullOrWhiteSpace(templateType))
        {
            var templates = _store.GetDocumentTemplatesByType(templateType);
            return Ok(templates);
        }
        else
        {
            var templates = _store.GetDocumentTemplates();
            return Ok(templates);
        }
    }
    
    /// <summary>
    /// Get a specific document template by ID
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<DocumentTemplate> GetTemplate(string id)
    {
        var template = _store.GetDocumentTemplate(id);
        if (template == null)
        {
            return NotFound(new { error = "DocumentTemplate not found." });
        }
        return Ok(template);
    }
    
    /// <summary>
    /// Get the default template for a specific type
    /// </summary>
    [HttpGet("default/{templateType}")]
    public ActionResult<DocumentTemplate> GetDefaultTemplate(string templateType)
    {
        var template = _store.GetDefaultDocumentTemplate(templateType);
        if (template == null)
        {
            return NotFound(new { error = $"No default {templateType} template configured." });
        }
        return Ok(template);
    }
    
    /// <summary>
    /// Create a new document template
    /// </summary>
    [HttpPost]
    public ActionResult<DocumentTemplate> CreateTemplate([FromBody] CreateDocumentTemplateRequest request)
    {
        var (isValid, errorMessage, template) = _store.CreateDocumentTemplate(request);
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }
        return CreatedAtAction(nameof(GetTemplate), new { id = template!.Id }, template);
    }
    
    /// <summary>
    /// Update an existing document template (creates a new version)
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<DocumentTemplate> UpdateTemplate(string id, [FromBody] UpdateDocumentTemplateRequest request)
    {
        var (isValid, errorMessage, template) = _store.UpdateDocumentTemplate(id, request);
        if (!isValid)
        {
            if (errorMessage == "DocumentTemplate not found.")
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }
        return Ok(template);
    }
    
    /// <summary>
    /// Delete a document template
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteTemplate(string id, [FromQuery] string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            return BadRequest(new { error = "deletedBy query parameter is required." });
        }
        
        var success = _store.DeleteDocumentTemplate(id, deletedBy);
        if (!success)
        {
            return NotFound(new { error = "DocumentTemplate not found." });
        }
        return NoContent();
    }
    
    /// <summary>
    /// Get usage history for a specific template
    /// </summary>
    [HttpGet("{id}/usage-history")]
    public ActionResult<List<TemplateUsageRecord>> GetTemplateUsageHistory(string id)
    {
        var template = _store.GetDocumentTemplate(id);
        if (template == null)
        {
            return NotFound(new { error = "DocumentTemplate not found." });
        }
        
        var history = _store.GetTemplateUsageHistory(id);
        return Ok(history);
    }
    
    /// <summary>
    /// Get template usage for a specific period
    /// </summary>
    [HttpGet("usage/period/{periodId}")]
    public ActionResult<List<TemplateUsageRecord>> GetPeriodTemplateUsage(string periodId)
    {
        var usage = _store.GetPeriodTemplateUsage(periodId);
        return Ok(usage);
    }
}
