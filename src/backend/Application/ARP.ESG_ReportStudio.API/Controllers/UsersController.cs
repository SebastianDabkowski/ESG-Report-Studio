using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly InMemoryReportStore _store;

    public UsersController(InMemoryReportStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<User>> GetUsers()
    {
        return Ok(_store.GetUsers());
    }

    [HttpGet("{id}")]
    public ActionResult<User> GetUser(string id)
    {
        var user = _store.GetUser(id);
        if (user == null)
        {
            return NotFound(new { error = $"User with ID '{id}' not found." });
        }

        return Ok(user);
    }
}
