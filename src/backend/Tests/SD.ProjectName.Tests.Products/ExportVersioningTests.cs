using Xunit;
using ARP.ESG_ReportStudio.API.Services;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Tests for export schema versioning and backward compatibility.
/// </summary>
public sealed class ExportVersioningTests
{
    [Fact]
    public void ExportSchemaVersion_ParsesVersionString()
    {
        // Arrange & Act
        var version = ExportSchemaVersion.Parse("1.2.3", "json");
        
        // Assert
        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
        Assert.Equal(3, version.Patch);
        Assert.Equal("json", version.Format);
        Assert.Equal("1.2.3", version.VersionString);
    }
    
    [Fact]
    public void ExportSchemaVersion_ThrowsOnInvalidFormat()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => ExportSchemaVersion.Parse("1.2", "json"));
        Assert.Throws<ArgumentException>(() => ExportSchemaVersion.Parse("1.2.3.4", "json"));
        Assert.Throws<ArgumentException>(() => ExportSchemaVersion.Parse("a.b.c", "json"));
    }
    
    [Fact]
    public void ExportSchemaVersion_DetectsBreakingChanges()
    {
        // Arrange
        var v1 = new ExportSchemaVersion(1, 0, 0, "json");
        var v2 = new ExportSchemaVersion(2, 0, 0, "json");
        var v1_1 = new ExportSchemaVersion(1, 1, 0, "json");
        
        // Act & Assert
        Assert.True(v2.IsBreakingChangeFrom(v1));
        Assert.False(v1_1.IsBreakingChangeFrom(v1));
        Assert.False(v1.IsBreakingChangeFrom(v1));
    }
    
    [Fact]
    public void ExportSchemaVersion_ChecksBackwardCompatibility()
    {
        // Arrange
        var v1_0_0 = new ExportSchemaVersion(1, 0, 0, "json");
        var v1_1_0 = new ExportSchemaVersion(1, 1, 0, "json");
        var v1_1_1 = new ExportSchemaVersion(1, 1, 1, "json");
        var v2_0_0 = new ExportSchemaVersion(2, 0, 0, "json");
        
        // Act & Assert
        Assert.True(v1_1_0.IsBackwardCompatibleWith(v1_0_0));
        Assert.True(v1_1_1.IsBackwardCompatibleWith(v1_1_0));
        Assert.False(v2_0_0.IsBackwardCompatibleWith(v1_0_0)); // Major version change
        Assert.False(v1_0_0.IsBackwardCompatibleWith(v1_1_0)); // Earlier version
    }
    
    [Fact]
    public void ExportSchemaRegistry_ReturnsCurrentVersions()
    {
        // Act
        var jsonVersion = ExportSchemaRegistry.GetCurrentVersion("json");
        var pdfVersion = ExportSchemaRegistry.GetCurrentVersion("pdf");
        var docxVersion = ExportSchemaRegistry.GetCurrentVersion("docx");
        
        // Assert
        Assert.Equal("json", jsonVersion.Format);
        Assert.Equal("1.0.0", jsonVersion.VersionString);
        Assert.True(jsonVersion.IsActive);
        
        Assert.Equal("pdf", pdfVersion.Format);
        Assert.Equal("1.0.0", pdfVersion.VersionString);
        Assert.True(pdfVersion.IsActive);
        
        Assert.Equal("docx", docxVersion.Format);
        Assert.Equal("1.0.0", docxVersion.VersionString);
        Assert.True(docxVersion.IsActive);
    }
    
    [Fact]
    public void ExportSchemaRegistry_ThrowsOnUnknownFormat()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ExportSchemaRegistry.GetCurrentVersion("unknown"));
    }
    
    [Fact]
    public void ExportMetadata_CreatesFromSchemaVersion()
    {
        // Arrange
        var schemaVersion = new ExportSchemaVersion(1, 0, 0, "json");
        
        // Act
        var metadata = ExportMetadata.FromSchemaVersion(schemaVersion, "user123", "John Doe");
        
        // Assert
        Assert.Equal("json", metadata.Format);
        Assert.Equal("1.0.0", metadata.SchemaVersion);
        Assert.Equal("esg-report-studio/json/v1", metadata.SchemaIdentifier);
        Assert.Equal("user123", metadata.ExportedBy);
        Assert.Equal("John Doe", metadata.ExportedByName);
        Assert.NotEmpty(metadata.ExportId);
    }
    
    [Fact]
    public void ExportMetadata_GeneratesUniqueExportIds()
    {
        // Arrange
        var schemaVersion = new ExportSchemaVersion(1, 0, 0, "json");
        
        // Act
        var metadata1 = ExportMetadata.FromSchemaVersion(schemaVersion);
        var metadata2 = ExportMetadata.FromSchemaVersion(schemaVersion);
        
        // Assert
        Assert.NotEqual(metadata1.ExportId, metadata2.ExportId);
    }
    
    [Fact]
    public void ExportMetadata_FormatsMajorVersionInSchemaIdentifier()
    {
        // Arrange & Act
        var v1 = ExportMetadata.FromSchemaVersion(new ExportSchemaVersion(1, 2, 3, "json"));
        var v2 = ExportMetadata.FromSchemaVersion(new ExportSchemaVersion(2, 0, 0, "pdf"));
        
        // Assert
        Assert.Equal("esg-report-studio/json/v1", v1.SchemaIdentifier);
        Assert.Equal("esg-report-studio/pdf/v2", v2.SchemaIdentifier);
    }
    
    [Fact]
    public void ExportMetadata_DisplayString_IncludesKeyInfo()
    {
        // Arrange
        var schemaVersion = new ExportSchemaVersion(1, 0, 0, "json");
        var metadata = ExportMetadata.FromSchemaVersion(schemaVersion, "user123", "John Doe");
        metadata.PeriodName = "2024 Annual Report";
        metadata.VariantName = "Executive Summary";
        
        // Act
        var displayString = metadata.ToDisplayString();
        
        // Assert
        Assert.Contains("Export ID:", displayString);
        Assert.Contains("Format: json", displayString);
        Assert.Contains("Schema Version: 1.0.0", displayString);
        Assert.Contains("Schema: esg-report-studio/json/v1", displayString);
        Assert.Contains("Exported By: John Doe", displayString);
        Assert.Contains("Period: 2024 Annual Report", displayString);
        Assert.Contains("Variant: Executive Summary", displayString);
    }
    
    [Fact]
    public void ExportMetadata_DocumentLines_FormatsForDocumentFooter()
    {
        // Arrange
        var schemaVersion = new ExportSchemaVersion(1, 0, 0, "pdf");
        var metadata = ExportMetadata.FromSchemaVersion(schemaVersion, "user123", "Jane Smith");
        
        // Act
        var lines = metadata.ToDocumentLines().ToList();
        
        // Assert
        Assert.Contains(lines, l => l.Contains("Export ID:"));
        Assert.Contains(lines, l => l.Contains("Schema: esg-report-studio/pdf/v1"));
        Assert.Contains(lines, l => l.Contains("v1.0.0"));
        Assert.Contains(lines, l => l.Contains("Exported By: Jane Smith"));
    }
}
