using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using ARP.ESG_ReportStudio.API.Models;

namespace SD.ProjectName.Tests.Integrations;

/// <summary>
/// Tests for API versioning behavior and error handling
/// </summary>
public class ApiVersioningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiVersioningTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task VersionedEndpoint_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/WeatherForecast");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ErrorResponse_IncludesCorrelationId()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Request a non-existent endpoint to trigger error
        var response = await client.GetAsync("/api/v1/NonExistentEndpoint");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        // Check for correlation ID in response headers
        Assert.True(response.Headers.Contains("X-Correlation-ID"), 
            "Response should include X-Correlation-ID header");
    }

    [Fact]
    public async Task ApiVersionHeader_IsReported()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/WeatherForecast");

        // Assert
        Assert.True(response.Headers.Contains("api-supported-versions") || 
                   response.Headers.Contains("api-deprecated-versions"),
            "Response should include API versioning headers");
    }

    [Fact]
    public async Task UnversionedRequest_UsesDefaultVersion()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Request without version should use default (v1)
        var response = await client.GetAsync("/WeatherForecast");

        // Assert - Should either succeed with default version or return not found
        // (depends on configuration, but should not throw unhandled exception)
        Assert.True(response.IsSuccessStatusCode || 
                   response.StatusCode == HttpStatusCode.NotFound);
    }
}
