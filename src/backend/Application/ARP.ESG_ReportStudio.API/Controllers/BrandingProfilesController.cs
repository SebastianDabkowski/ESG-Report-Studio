using Microsoft.AspNetCore.Mvc;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Controllers;

[ApiController]
[Route("api/branding-profiles")]
public sealed class BrandingProfilesController : ControllerBase
{
    private readonly InMemoryReportStore _store;
    
    public BrandingProfilesController(InMemoryReportStore store)
    {
        _store = store;
    }
    
    /// <summary>
    /// Get all branding profiles
    /// </summary>
    [HttpGet]
    public ActionResult<List<BrandingProfile>> GetProfiles()
    {
        var profiles = _store.GetBrandingProfiles();
        return Ok(profiles);
    }
    
    /// <summary>
    /// Get a specific branding profile by ID
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<BrandingProfile> GetProfile(string id)
    {
        var profile = _store.GetBrandingProfile(id);
        if (profile == null)
        {
            return NotFound(new { error = "BrandingProfile not found." });
        }
        return Ok(profile);
    }
    
    /// <summary>
    /// Get the default branding profile
    /// </summary>
    [HttpGet("default")]
    public ActionResult<BrandingProfile> GetDefaultProfile()
    {
        var profile = _store.GetDefaultBrandingProfile();
        if (profile == null)
        {
            return NotFound(new { error = "No default branding profile configured." });
        }
        return Ok(profile);
    }
    
    /// <summary>
    /// Create a new branding profile
    /// </summary>
    [HttpPost]
    public ActionResult<BrandingProfile> CreateProfile([FromBody] CreateBrandingProfileRequest request)
    {
        var (isValid, errorMessage, profile) = _store.CreateBrandingProfile(request);
        if (!isValid)
        {
            return BadRequest(new { error = errorMessage });
        }
        return CreatedAtAction(nameof(GetProfile), new { id = profile!.Id }, profile);
    }
    
    /// <summary>
    /// Update an existing branding profile
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<BrandingProfile> UpdateProfile(string id, [FromBody] UpdateBrandingProfileRequest request)
    {
        var (isValid, errorMessage, profile) = _store.UpdateBrandingProfile(id, request);
        if (!isValid)
        {
            if (errorMessage == "BrandingProfile not found.")
            {
                return NotFound(new { error = errorMessage });
            }
            return BadRequest(new { error = errorMessage });
        }
        return Ok(profile);
    }
    
    /// <summary>
    /// Delete a branding profile
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteProfile(string id, [FromQuery] string deletedBy)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            return BadRequest(new { error = "deletedBy query parameter is required." });
        }
        
        var success = _store.DeleteBrandingProfile(id, deletedBy);
        if (!success)
        {
            return NotFound(new { error = "BrandingProfile not found." });
        }
        return NoContent();
    }
}
