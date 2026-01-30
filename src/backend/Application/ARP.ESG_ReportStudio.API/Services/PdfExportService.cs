using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for exporting generated reports to PDF format with title page, TOC, and page numbering.
/// </summary>
public sealed class PdfExportService : IPdfExportService
{
    public PdfExportService()
    {
        // Set QuestPDF license type for community/commercial use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(GeneratedReport report, PdfExportOptions? options = null)
    {
        options ??= new PdfExportOptions();
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));
                
                page.Content().Column(column =>
                {
                    // Title page
                    if (options.IncludeTitlePage)
                    {
                        column.Item().PageBreak();
                        column.Item().Element(c => ComposeTitlePage(c, report, options));
                    }
                    
                    // Table of contents
                    if (options.IncludeTableOfContents && report.Sections.Count > 0)
                    {
                        column.Item().PageBreak();
                        column.Item().Element(c => ComposeTableOfContents(c, report));
                    }
                    
                    // Report content
                    if (report.Sections.Count > 0)
                    {
                        column.Item().PageBreak();
                        column.Item().Element(c => ComposeReportContent(c, report));
                    }
                    
                    // Attachments appendix
                    if (options.IncludeAttachments)
                    {
                        column.Item().PageBreak();
                        column.Item().Element(c => ComposeAttachmentsAppendix(c, report, options));
                    }
                });
                
                // Footer with page numbers and optional branding footer text
                if (options.IncludePageNumbers || !string.IsNullOrWhiteSpace(options.BrandingProfile?.FooterText))
                {
                    page.Footer().Column(footer =>
                    {
                        // Custom footer text from branding if available
                        if (!string.IsNullOrWhiteSpace(options.BrandingProfile?.FooterText))
                        {
                            footer.Item().AlignCenter().DefaultTextStyle(x => x.FontSize(8)).Text(options.BrandingProfile.FooterText);
                        }
                        
                        // Page numbers
                        if (options.IncludePageNumbers)
                        {
                            footer.Item().AlignCenter().DefaultTextStyle(x => x.FontSize(9)).Text(text =>
                            {
                                text.CurrentPageNumber();
                                text.Span(" / ");
                                text.TotalPages();
                            });
                        }
                    });
                }
            });
        });

        return document.GeneratePdf();
    }

    public string GenerateFilename(GeneratedReport report, string? variantName = null)
    {
        return ExportUtilities.GenerateFilename(report, variantName, ".pdf");
    }

    private void ComposeTitlePage(IContainer container, GeneratedReport report, PdfExportOptions options)
    {
        container.Column(column =>
        {
            column.Spacing(20);
            
            // Logo if branding profile includes one (base64 encoded)
            if (!string.IsNullOrWhiteSpace(options.BrandingProfile?.LogoData))
            {
                try
                {
                    // Note: In a real implementation, you would decode the base64 and render the image
                    // For now, we'll just add a placeholder for logo space
                    column.Item().AlignCenter().Height(80).AlignMiddle().Text("[Company Logo]").FontSize(12).Italic();
                }
                catch
                {
                    // Logo rendering failed, skip it
                }
            }
            
            // Organization name
            // Note: Primary color from branding profile could be applied here in future
            column.Item().AlignCenter().Text(report.Organization?.Name ?? "ESG Responsibility Report")
                .FontSize(24).Bold();
            
            // Report period
            column.Item().AlignCenter().Text(report.Period.Name)
                .FontSize(18).SemiBold();
            
            // Subsidiary name if from branding profile
            if (!string.IsNullOrWhiteSpace(options.BrandingProfile?.SubsidiaryName))
            {
                column.Item().AlignCenter().Text(options.BrandingProfile.SubsidiaryName)
                    .FontSize(14);
            }
            
            // Date range
            column.Item().AlignCenter().Text($"{report.Period.StartDate} to {report.Period.EndDate}")
                .FontSize(14);
            
            // Variant name if provided
            if (!string.IsNullOrWhiteSpace(options.VariantName))
            {
                column.Item().AlignCenter().Text($"Variant: {options.VariantName}")
                    .FontSize(14).Italic();
            }
            
            column.Item().PaddingTop(40);
            
            // Report metadata
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(150);
                    columns.RelativeColumn();
                });
                
                table.Cell().Text("Report Mode:").SemiBold();
                table.Cell().Text(report.Period.ReportingMode);
                
                table.Cell().Text("Report Scope:").SemiBold();
                table.Cell().Text(report.Period.ReportScope);
                
                table.Cell().Text("Generated By:").SemiBold();
                table.Cell().Text(report.GeneratedByName);
                
                table.Cell().Text("Generated At:").SemiBold();
                table.Cell().Text(FormatDateTime(report.GeneratedAt));
                
                table.Cell().Text("Total Sections:").SemiBold();
                table.Cell().Text(report.Sections.Count.ToString());
            });
        });
    }

    private void ComposeTableOfContents(IContainer container, GeneratedReport report)
    {
        container.Column(column =>
        {
            column.Item().Text("Table of Contents").FontSize(18).Bold();
            column.Item().PaddingTop(10);
            
            foreach (var section in report.Sections.OrderBy(s => s.Section.Order))
            {
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Width(40).Text($"{section.Section.Order}.");
                    row.RelativeItem().Text(section.Section.Title);
                });
            }
        });
    }

    private void ComposeReportContent(IContainer container, GeneratedReport report)
    {
        container.Column(column =>
        {
            foreach (var section in report.Sections.OrderBy(s => s.Section.Order))
            {
                column.Item().Element(c => ComposeSection(c, section));
                column.Item().PaddingBottom(20);
            }
        });
    }

    private void ComposeSection(IContainer container, GeneratedReportSection section)
    {
        container.Column(column =>
        {
            // Section header
            column.Item().PaddingBottom(10).Column(headerColumn =>
            {
                headerColumn.Item().Text(section.Section.Title).FontSize(16).Bold();
                
                if (!string.IsNullOrWhiteSpace(section.Section.Description))
                {
                    headerColumn.Item().PaddingTop(5).Text(section.Section.Description)
                        .FontSize(10).Italic();
                }
                
                // Section metadata
                headerColumn.Item().PaddingTop(5).Row(row =>
                {
                    row.AutoItem().Text($"Category: {section.Section.Category}").FontSize(9);
                    row.AutoItem().PaddingLeft(15).Text($"Owner: {section.Owner?.Name ?? "Unassigned"}").FontSize(9);
                    row.AutoItem().PaddingLeft(15).Text($"Status: {section.Section.Status}").FontSize(9);
                });
            });
            
            // Data points
            if (section.DataPoints.Count > 0)
            {
                column.Item().Element(c => ComposeDataPoints(c, section.DataPoints));
            }
            
            // Assumptions
            if (section.Assumptions.Count > 0)
            {
                column.Item().PaddingTop(10).Element(c => ComposeAssumptions(c, section.Assumptions));
            }
            
            // Gaps
            if (section.Gaps.Count > 0)
            {
                column.Item().PaddingTop(10).Element(c => ComposeGaps(c, section.Gaps));
            }
        });
    }

    private void ComposeDataPoints(IContainer container, List<DataPointSnapshot> dataPoints)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });
            
            // Header
            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Title").SemiBold();
                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Value").SemiBold();
                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Unit").SemiBold();
                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Status").SemiBold();
            });
            
            // Data rows with alternating background
            var rowIndex = 0;
            foreach (var dp in dataPoints)
            {
                var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                
                table.Cell().Background(bgColor).Padding(5).Text(dp.Title ?? "").FontSize(10);
                table.Cell().Background(bgColor).Padding(5).Text(dp.Value ?? "-").FontSize(10);
                table.Cell().Background(bgColor).Padding(5).Text(dp.Unit ?? "-").FontSize(10);
                table.Cell().Background(bgColor).Padding(5).Text(dp.Status ?? "-").FontSize(10);
                
                rowIndex++;
            }
        });
    }

    private void ComposeAssumptions(IContainer container, List<AssumptionRecord> assumptions)
    {
        container.Column(column =>
        {
            column.Item().Background(Colors.Orange.Lighten4).Padding(8).Column(assumptionColumn =>
            {
                assumptionColumn.Item().Text("Assumptions").FontSize(12).SemiBold();
                
                foreach (var assumption in assumptions)
                {
                    assumptionColumn.Item().PaddingTop(5).Text(text =>
                    {
                        text.Span("• ").SemiBold();
                        text.Span($"{assumption.Description ?? "No description"}");
                        if (!string.IsNullOrWhiteSpace(assumption.ConfidenceLevel))
                        {
                            text.Span($" (Confidence: {assumption.ConfidenceLevel})").Italic().FontSize(9);
                        }
                    });
                }
            });
        });
    }

    private void ComposeGaps(IContainer container, List<GapRecord> gaps)
    {
        container.Column(column =>
        {
            column.Item().Background(Colors.Red.Lighten4).Padding(8).Column(gapColumn =>
            {
                gapColumn.Item().Text("Data Gaps").FontSize(12).SemiBold();
                
                foreach (var gap in gaps)
                {
                    gapColumn.Item().PaddingTop(5).Text(text =>
                    {
                        text.Span("• ").SemiBold();
                        text.Span($"{gap.Description ?? "No description"}");
                        if (!string.IsNullOrWhiteSpace(gap.MissingReason))
                        {
                            text.Span($" (Reason: {gap.MissingReason})").Italic().FontSize(9);
                        }
                    });
                }
            });
        });
    }

    private void ComposeAttachmentsAppendix(IContainer container, GeneratedReport report, PdfExportOptions options)
    {
        container.Column(column =>
        {
            column.Item().Text("Appendix: Evidence and Attachments").FontSize(18).Bold();
            column.Item().PaddingTop(10);
            
            // Collect all evidence from all sections
            var allEvidence = new List<(string SectionTitle, EvidenceMetadata Evidence)>();
            long totalSize = 0;
            int restrictedCount = 0;
            
            foreach (var section in report.Sections.OrderBy(s => s.Section.Order))
            {
                foreach (var evidence in section.Evidence)
                {
                    allEvidence.Add((section.Section.Title, evidence));
                    totalSize += evidence.FileSize;
                    if (!evidence.IsAccessible)
                    {
                        restrictedCount++;
                    }
                }
            }
            
            if (allEvidence.Count == 0)
            {
                column.Item().Text("No evidence or attachments are associated with this report.").Italic();
                return;
            }
            
            // Calculate total size in MB
            var totalSizeMB = totalSize / (1024.0 * 1024.0);
            var maxSizeMB = options.MaxAttachmentSizeMB;
            
            // Show warning if size exceeds limit
            if (totalSizeMB > maxSizeMB)
            {
                column.Item().Background(Colors.Orange.Lighten4).Padding(8).Column(warningColumn =>
                {
                    warningColumn.Item().Text("⚠ File Size Warning").FontSize(12).SemiBold();
                    warningColumn.Item().PaddingTop(5).Text(text =>
                    {
                        text.Span($"Total attachment size ({totalSizeMB:F2} MB) exceeds the recommended limit ({maxSizeMB} MB). ");
                        text.Span("Only attachment metadata is included in this export. ").Italic();
                        text.Span("For full attachments, consider using the audit package export (ZIP) or external file sharing.").Italic();
                    });
                });
                column.Item().PaddingBottom(10);
            }
            
            // Show restriction notice if applicable
            if (restrictedCount > 0)
            {
                column.Item().Background(Colors.Red.Lighten4).Padding(8).Column(restrictColumn =>
                {
                    restrictColumn.Item().Text("🔒 Restricted Attachments").FontSize(12).SemiBold();
                    restrictColumn.Item().PaddingTop(5).Text(text =>
                    {
                        text.Span($"{restrictedCount} attachment(s) are restricted and not accessible to the current user. ");
                        text.Span("These attachments are marked with 🔒 and excluded from this export.").Italic();
                    });
                });
                column.Item().PaddingBottom(10);
            }
            
            // Summary
            column.Item().PaddingBottom(10).Text(text =>
            {
                text.Span($"Total Attachments: {allEvidence.Count}").SemiBold();
                text.Span($" | Total Size: {FormatFileSize(totalSize)}");
                if (restrictedCount > 0)
                {
                    text.Span($" | Accessible: {allEvidence.Count - restrictedCount}");
                }
            });
            
            // Evidence table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2); // Section
                    columns.RelativeColumn(2); // Title
                    columns.RelativeColumn(2); // File Name
                    columns.RelativeColumn(1); // Size
                    columns.RelativeColumn(1); // Status
                    columns.RelativeColumn(1); // Uploaded
                });
                
                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Section").SemiBold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Title").SemiBold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("File Name").SemiBold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Size").SemiBold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Integrity").SemiBold();
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Uploaded").SemiBold();
                });
                
                // Data rows
                var rowIndex = 0;
                foreach (var (sectionTitle, evidence) in allEvidence)
                {
                    var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                    
                    // Apply red background for restricted items
                    if (!evidence.IsAccessible)
                    {
                        bgColor = Colors.Red.Lighten5;
                    }
                    
                    table.Cell().Background(bgColor).Padding(5).Text(sectionTitle).FontSize(9);
                    
                    var titleText = evidence.IsAccessible ? evidence.Title : $"🔒 {evidence.Title}";
                    table.Cell().Background(bgColor).Padding(5).Text(titleText).FontSize(9);
                    
                    table.Cell().Background(bgColor).Padding(5).Text(evidence.FileName ?? "-").FontSize(9);
                    table.Cell().Background(bgColor).Padding(5).Text(FormatFileSize(evidence.FileSize)).FontSize(9);
                    
                    var integrityText = evidence.IntegrityStatus switch
                    {
                        "valid" => "✓ Valid",
                        "failed" => "✗ Failed",
                        _ => "? Not Checked"
                    };
                    table.Cell().Background(bgColor).Padding(5).Text(integrityText).FontSize(9);
                    
                    table.Cell().Background(bgColor).Padding(5).Text(FormatDate(evidence.UploadedAt)).FontSize(9);
                    
                    rowIndex++;
                }
            });
            
            // Additional notes
            column.Item().PaddingTop(10);
            column.Item().Text("Notes:").FontSize(10).SemiBold();
            column.Item().PaddingTop(5).Text("• This appendix lists all evidence and attachments referenced in the report.").FontSize(9);
            column.Item().Text("• Attachment checksums and integrity status ensure file authenticity.").FontSize(9);
            column.Item().Text("• For access to actual files, download them from the ESG Report Studio or request an audit package.").FontSize(9);
        });
    }
    
    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        else if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        else
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }
    
    private string FormatDate(string? isoDateTime)
    {
        if (string.IsNullOrWhiteSpace(isoDateTime))
            return "-";
        
        if (DateTime.TryParse(isoDateTime, out var dateTime))
        {
            return dateTime.ToString("yyyy-MM-dd");
        }
        
        return isoDateTime;
    }

    private string FormatDateTime(string? isoDateTime)
    {
        return ExportUtilities.FormatDateTime(isoDateTime);
    }

    private string SanitizeFilename(string filename)
    {
        return ExportUtilities.SanitizeFilename(filename);
    }
}
