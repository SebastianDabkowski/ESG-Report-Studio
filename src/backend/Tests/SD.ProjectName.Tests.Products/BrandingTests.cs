using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

public sealed class BrandingProfileTests
{
    [Fact]
    public void CreateBrandingProfile_WithValidRequest_CreatesProfile()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var request = new CreateBrandingProfileRequest
        {
            Name = "Main Brand",
            Description = "Primary corporate branding",
            PrimaryColor = "#1E40AF",
            SecondaryColor = "#9333EA",
            FooterText = "© 2024 Company Inc.",
            IsDefault = true,
            CreatedBy = "user-1"
        };
        
        // Act
        var (isValid, errorMessage, profile) = store.CreateBrandingProfile(request);
        
        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(profile);
        Assert.Equal(request.Name, profile!.Name);
        Assert.Equal(request.Description, profile.Description);
        Assert.Equal(request.PrimaryColor, profile.PrimaryColor);
        Assert.Equal(request.FooterText, profile.FooterText);
        Assert.True(profile.IsDefault);
        Assert.True(profile.IsActive);
    }
    
    [Fact]
    public void CreateBrandingProfile_WithMissingName_ReturnsError()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var request = new CreateBrandingProfileRequest
        {
            Name = "",
            CreatedBy = "user-1"
        };
        
        // Act
        var (isValid, errorMessage, profile) = store.CreateBrandingProfile(request);
        
        // Assert
        Assert.False(isValid);
        Assert.Equal("Name is required.", errorMessage);
        Assert.Null(profile);
    }
    
    [Fact]
    public void CreateBrandingProfile_AsDefault_UnmarksOtherDefaults()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Create first default profile
        var request1 = new CreateBrandingProfileRequest
        {
            Name = "Brand 1",
            IsDefault = true,
            CreatedBy = "user-1"
        };
        var (_, _, profile1) = store.CreateBrandingProfile(request1);
        
        // Create second default profile
        var request2 = new CreateBrandingProfileRequest
        {
            Name = "Brand 2",
            IsDefault = true,
            CreatedBy = "user-1"
        };
        
        // Act
        var (isValid, _, profile2) = store.CreateBrandingProfile(request2);
        
        // Assert
        Assert.True(isValid);
        Assert.NotNull(profile2);
        Assert.True(profile2!.IsDefault);
        
        // First profile should no longer be default
        var profiles = store.GetBrandingProfiles();
        var updatedProfile1 = profiles.First(p => p.Id == profile1!.Id);
        Assert.False(updatedProfile1.IsDefault);
    }
    
    [Fact]
    public void UpdateBrandingProfile_WithValidRequest_UpdatesProfile()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var createRequest = new CreateBrandingProfileRequest
        {
            Name = "Original Brand",
            PrimaryColor = "#000000",
            CreatedBy = "user-1"
        };
        var (_, _, profile) = store.CreateBrandingProfile(createRequest);
        
        var updateRequest = new UpdateBrandingProfileRequest
        {
            Name = "Updated Brand",
            PrimaryColor = "#FF0000",
            AccentColor = "#00FF00",
            IsDefault = false,
            IsActive = true,
            UpdatedBy = "user-2"
        };
        
        // Act
        var (isValid, errorMessage, updatedProfile) = store.UpdateBrandingProfile(profile!.Id, updateRequest);
        
        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(updatedProfile);
        Assert.Equal(updateRequest.Name, updatedProfile!.Name);
        Assert.Equal(updateRequest.PrimaryColor, updatedProfile.PrimaryColor);
        Assert.Equal(updateRequest.AccentColor, updatedProfile.AccentColor);
        Assert.Equal("user-2", updatedProfile.UpdatedBy);
        Assert.NotNull(updatedProfile.UpdatedAt);
    }
    
    [Fact]
    public void GetDefaultBrandingProfile_ReturnsDefaultProfile()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Create non-default profile
        store.CreateBrandingProfile(new CreateBrandingProfileRequest
        {
            Name = "Non-Default",
            IsDefault = false,
            CreatedBy = "user-1"
        });
        
        // Create default profile
        store.CreateBrandingProfile(new CreateBrandingProfileRequest
        {
            Name = "Default Brand",
            IsDefault = true,
            CreatedBy = "user-1"
        });
        
        // Act
        var defaultProfile = store.GetDefaultBrandingProfile();
        
        // Assert
        Assert.NotNull(defaultProfile);
        Assert.Equal("Default Brand", defaultProfile!.Name);
        Assert.True(defaultProfile.IsDefault);
        Assert.True(defaultProfile.IsActive);
    }
    
    [Fact]
    public void DeleteBrandingProfile_RemovesProfile()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var (_, _, profile) = store.CreateBrandingProfile(new CreateBrandingProfileRequest
        {
            Name = "To Delete",
            CreatedBy = "user-1"
        });
        
        // Act
        var success = store.DeleteBrandingProfile(profile!.Id, "user-2");
        
        // Assert
        Assert.True(success);
        var profiles = store.GetBrandingProfiles();
        Assert.DoesNotContain(profiles, p => p.Id == profile.Id);
    }
}

public sealed class DocumentTemplateTests
{
    [Fact]
    public void CreateDocumentTemplate_WithValidRequest_CreatesTemplate()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var request = new CreateDocumentTemplateRequest
        {
            Name = "Standard PDF Template",
            Description = "Default PDF export template",
            TemplateType = "pdf",
            Configuration = @"{""pageSize"": ""A4"", ""margins"": 20}",
            IsDefault = true,
            CreatedBy = "user-1"
        };
        
        // Act
        var (isValid, errorMessage, template) = store.CreateDocumentTemplate(request);
        
        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(template);
        Assert.Equal(request.Name, template!.Name);
        Assert.Equal(request.TemplateType, template.TemplateType);
        Assert.Equal(1, template.Version);
        Assert.True(template.IsDefault);
        Assert.True(template.IsActive);
    }
    
    [Fact]
    public void CreateDocumentTemplate_WithInvalidType_ReturnsError()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var request = new CreateDocumentTemplateRequest
        {
            Name = "Invalid Template",
            TemplateType = "invalid",
            CreatedBy = "user-1"
        };
        
        // Act
        var (isValid, errorMessage, template) = store.CreateDocumentTemplate(request);
        
        // Assert
        Assert.False(isValid);
        Assert.Contains("TemplateType must be one of", errorMessage);
        Assert.Null(template);
    }
    
    [Fact]
    public void UpdateDocumentTemplate_IncrementsVersion()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var createRequest = new CreateDocumentTemplateRequest
        {
            Name = "Template v1",
            TemplateType = "pdf",
            Configuration = @"{""version"": 1}",
            CreatedBy = "user-1"
        };
        var (_, _, template) = store.CreateDocumentTemplate(createRequest);
        
        var updateRequest = new UpdateDocumentTemplateRequest
        {
            Name = "Template v2",
            Configuration = @"{""version"": 2}",
            IsDefault = false,
            IsActive = true,
            UpdatedBy = "user-1"
        };
        
        // Act
        var (isValid, _, updatedTemplate) = store.UpdateDocumentTemplate(template!.Id, updateRequest);
        
        // Assert
        Assert.True(isValid);
        Assert.NotNull(updatedTemplate);
        Assert.Equal(2, updatedTemplate!.Version);
        Assert.Equal(updateRequest.Name, updatedTemplate.Name);
        Assert.Equal(updateRequest.Configuration, updatedTemplate.Configuration);
    }
    
    [Fact]
    public void GetDefaultDocumentTemplate_ForType_ReturnsCorrectTemplate()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Create default PDF template
        store.CreateDocumentTemplate(new CreateDocumentTemplateRequest
        {
            Name = "Default PDF",
            TemplateType = "pdf",
            IsDefault = true,
            CreatedBy = "user-1"
        });
        
        // Create non-default PDF template
        store.CreateDocumentTemplate(new CreateDocumentTemplateRequest
        {
            Name = "Other PDF",
            TemplateType = "pdf",
            IsDefault = false,
            CreatedBy = "user-1"
        });
        
        // Create default DOCX template
        store.CreateDocumentTemplate(new CreateDocumentTemplateRequest
        {
            Name = "Default DOCX",
            TemplateType = "docx",
            IsDefault = true,
            CreatedBy = "user-1"
        });
        
        // Act
        var pdfTemplate = store.GetDefaultDocumentTemplate("pdf");
        var docxTemplate = store.GetDefaultDocumentTemplate("docx");
        
        // Assert
        Assert.NotNull(pdfTemplate);
        Assert.Equal("Default PDF", pdfTemplate!.Name);
        Assert.NotNull(docxTemplate);
        Assert.Equal("Default DOCX", docxTemplate!.Name);
    }
    
    [Fact]
    public void RecordTemplateUsage_CreatesUsageRecord()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
        // Create a template
        var (_, _, template) = store.CreateDocumentTemplate(new CreateDocumentTemplateRequest
        {
            Name = "Test Template",
            TemplateType = "pdf",
            CreatedBy = "user-1"
        });
        
        // Create a period
        var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024 Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        });
        var period = snapshot!.Periods.First();
        
        // Act
        store.RecordTemplateUsage(template!.Id, period.Id, null, "pdf", "user-1");
        
        // Assert
        var usageHistory = store.GetTemplateUsageHistory(template.Id);
        Assert.Single(usageHistory);
        Assert.Equal(template.Id, usageHistory[0].TemplateId);
        Assert.Equal(1, usageHistory[0].TemplateVersion);
        Assert.Equal(period.Id, usageHistory[0].PeriodId);
        Assert.Equal("pdf", usageHistory[0].ExportType);
    }
    
    [Fact]
    public void GetPeriodTemplateUsage_ReturnsUsageForPeriod()
    {
        // Arrange
        var store = new InMemoryReportStore();
        ExportTestHelpers.CreateTestConfiguration(store);
        
        // Create templates
        var (_, _, template1) = store.CreateDocumentTemplate(new CreateDocumentTemplateRequest
        {
            Name = "PDF Template",
            TemplateType = "pdf",
            CreatedBy = "user-1"
        });
        
        var (_, _, template2) = store.CreateDocumentTemplate(new CreateDocumentTemplateRequest
        {
            Name = "DOCX Template",
            TemplateType = "docx",
            CreatedBy = "user-1"
        });
        
        // Create a period
        var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "2024 Report",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test User"
        });
        var period = snapshot!.Periods.First();
        
        // Record usage
        store.RecordTemplateUsage(template1!.Id, period.Id, null, "pdf", "user-1");
        store.RecordTemplateUsage(template2!.Id, period.Id, null, "docx", "user-1");
        
        // Act
        var periodUsage = store.GetPeriodTemplateUsage(period.Id);
        
        // Assert
        Assert.Equal(2, periodUsage.Count);
        Assert.Contains(periodUsage, u => u.TemplateId == template1.Id);
        Assert.Contains(periodUsage, u => u.TemplateId == template2.Id);
    }
}
