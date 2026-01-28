using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/validation-rules")]
public sealed class ValidationRulesController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public ValidationRulesController(InMemoryReportStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<ValidationRule>> GetValidationRules([FromQuery] string? sectionId)
    {
        return Ok(_store.GetValidationRules(sectionId));
    }

    [HttpGet("{id}")]
    public ActionResult<ValidationRule> GetValidationRule(string id)
    {
        var rule = _store.GetValidationRule(id);
        if (rule == null)
        {
            return NotFound(new { error = $"ValidationRule with ID '{id}' not found." });
        }

        return Ok(rule);
    }

    [HttpPost]
    public ActionResult<ValidationRule> CreateValidationRule([FromBody] CreateValidationRuleRequest request)
    {
        var (isValid, errorMessage, rule) = _store.CreateValidationRule(request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return CreatedAtAction(nameof(GetValidationRule), new { id = rule!.Id }, rule);
    }

    [HttpPut("{id}")]
    public ActionResult<ValidationRule> UpdateValidationRule(string id, [FromBody] UpdateValidationRuleRequest request)
    {
        var (isValid, errorMessage, rule) = _store.UpdateValidationRule(id, request);
        
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }

        return Ok(rule);
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteValidationRule(string id)
    {
        var deleted = _store.DeleteValidationRule(id);
        if (!deleted)
        {
            return NotFound(new { error = $"ValidationRule with ID '{id}' not found." });
        }

        return NoContent();
    }
}
