using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api")]
public sealed class ReportingController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public ReportingController(InMemoryReportStore store)
    {
        _store = store;
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

        var snapshot = _store.CreatePeriod(request);
        return Ok(snapshot);
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
}
