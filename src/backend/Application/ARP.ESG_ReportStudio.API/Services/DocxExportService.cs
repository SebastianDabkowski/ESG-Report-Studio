using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ARP.ESG_ReportStudio.API.Reporting;

namespace ARP.ESG_ReportStudio.API.Services;

/// <summary>
/// Service for exporting generated reports to DOCX format with proper heading styles, tables, and formatting.
/// </summary>
public sealed class DocxExportService : IDocxExportService
{
    private readonly ILocalizationService _localizationService;
    
    public DocxExportService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }
    
    public byte[] GenerateDocx(GeneratedReport report, DocxExportOptions? options = null)
    {
        options ??= new DocxExportOptions();
        
        // Get schema version (use provided or default to current)
        var schemaVersion = options.SchemaVersion ?? ExportSchemaRegistry.GetCurrentVersion("docx");
        
        // Create export metadata
        var exportMetadata = ExportMetadata.FromSchemaVersion(
            schemaVersion,
            options.UserId,
            options.UserName
        );
        exportMetadata.PeriodId = report.Period.Id;
        exportMetadata.PeriodName = report.Period.Name;
        exportMetadata.GenerationId = report.Id;
        exportMetadata.VariantName = options.VariantName;
        
        using var memStream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(memStream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            
            // Title page
            if (options.IncludeTitlePage)
            {
                AddTitlePage(body, report, options, exportMetadata);
                AddPageBreak(body);
            }
            
            // Table of contents
            if (options.IncludeTableOfContents && report.Sections.Count > 0)
            {
                AddTableOfContents(body, report, options.Language);
                AddPageBreak(body);
            }
            
            // Report sections
            if (report.Sections.Count > 0)
            {
                AddReportSections(body, report, options.Language);
            }
            
            // Attachments appendix
            if (options.IncludeAttachments)
            {
                AddPageBreak(body);
                AddAttachmentsAppendix(body, report, options);
            }
            
            // Add page numbering to footer if requested
            if (options.IncludePageNumbers)
            {
                AddPageNumbering(mainPart, exportMetadata);
            }
            
            mainPart.Document.Save();
        }
        
        return memStream.ToArray();
    }

    public string GenerateFilename(GeneratedReport report, string? variantName = null)
    {
        return ExportUtilities.GenerateFilename(report, variantName, ".docx");
    }

    private void AddTitlePage(Body body, GeneratedReport report, DocxExportOptions options, ExportMetadata exportMetadata)
    {
        // Organization name - Heading 1
        AddParagraph(body, report.Organization?.Name ?? "ESG Responsibility Report", "Heading1", true);
        AddEmptyParagraph(body);
        
        // Report period - Heading 2
        AddParagraph(body, report.Period.Name, "Heading2", true);
        AddEmptyParagraph(body);
        
        // Date range
        AddParagraph(body, $"{report.Period.StartDate} to {report.Period.EndDate}", null, true);
        AddEmptyParagraph(body);
        
        // Variant name if provided
        if (!string.IsNullOrWhiteSpace(options.VariantName))
        {
            var variantLabel = _localizationService.GetLabel("label.variant", options.Language);
            var variantPara = AddParagraph(body, $"{variantLabel} {options.VariantName}", null, true);
            MakeParagraphItalic(variantPara);
            AddEmptyParagraph(body);
        }
        
        // Language indicator if non-English
        if (!string.IsNullOrWhiteSpace(options.Language) && options.Language != "en-US")
        {
            var languageLabel = _localizationService.GetLabel("label.language", options.Language);
            var langPara = AddParagraph(body, $"{languageLabel} {options.Language}", null, true);
            MakeParagraphItalic(langPara);
            AddEmptyParagraph(body);
        }
        
        AddEmptyParagraph(body);
        AddEmptyParagraph(body);
        
        // Metadata table
        var table = CreateMetadataTable(report, options.Language);
        body.AppendChild(table);
        
        AddEmptyParagraph(body);
        AddEmptyParagraph(body);
        
        // Export metadata section
        AddParagraph(body, "Export Metadata", "Heading3");
        var exportMetaTable = new Table();
        
        // Table properties
        var tblProp = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        exportMetaTable.AppendChild(tblProp);
        
        AddTableRow(exportMetaTable, "Schema", exportMetadata.SchemaIdentifier);
        AddTableRow(exportMetaTable, "Version", exportMetadata.SchemaVersion);
        AddTableRow(exportMetaTable, "Export ID", exportMetadata.ExportId);
        AddTableRow(exportMetaTable, "Exported At", exportMetadata.ExportedAt);
        
        body.AppendChild(exportMetaTable);
    }

    private void AddTableOfContents(Body body, GeneratedReport report, string? language = null)
    {
        // TOC Title - Heading 1
        var tocLabel = _localizationService.GetLabel("label.table-of-contents", language);
        AddParagraph(body, tocLabel, "Heading1");
        AddEmptyParagraph(body);
        
        // List sections
        foreach (var section in report.Sections.OrderBy(s => s.Section.Order))
        {
            var para = body.AppendChild(new Paragraph());
            var run = para.AppendChild(new Run());
            run.AppendChild(new Text($"{section.Section.Order}. {section.Section.Title}"));
        }
    }

    private void AddReportSections(Body body, GeneratedReport report, string? language = null)
    {
        foreach (var section in report.Sections.OrderBy(s => s.Section.Order))
        {
            AddSection(body, section, language);
            AddEmptyParagraph(body);
            AddEmptyParagraph(body);
        }
    }

    private void AddSection(Body body, GeneratedReportSection section, string? language = null)
    {
        // Section title - Heading 2
        AddParagraph(body, section.Section.Title, "Heading2");
        
        // Section description
        if (!string.IsNullOrWhiteSpace(section.Section.Description))
        {
            var descPara = AddParagraph(body, section.Section.Description, null);
            MakeParagraphItalic(descPara);
            AddEmptyParagraph(body);
        }
        
        // Section metadata - Heading 3
        var categoryLabel = _localizationService.GetLabel("label.category", language);
        var ownerLabel = _localizationService.GetLabel("label.owner", language);
        var statusLabel = _localizationService.GetLabel("label.status", language);
        
        var metaPara = body.AppendChild(new Paragraph());
        var metaRun = metaPara.AppendChild(new Run());
        metaRun.AppendChild(new Text(
            $"{categoryLabel} {section.Section.Category} | {ownerLabel} {section.Owner?.Name ?? "Unassigned"} | {statusLabel} {section.Section.Status}"
        ));
        metaPara.AppendChild(new ParagraphProperties(
            new ParagraphStyleId { Val = "Heading3" }
        ));
        AddEmptyParagraph(body);
        
        // Data points table
        if (section.DataPoints.Count > 0)
        {
            var dataPointsTable = CreateDataPointsTable(section.DataPoints, language);
            body.AppendChild(dataPointsTable);
            AddEmptyParagraph(body);
        }
        
        // Assumptions
        if (section.Assumptions.Count > 0)
        {
            AddAssumptions(body, section.Assumptions, language);
            AddEmptyParagraph(body);
        }
        
        // Gaps
        if (section.Gaps.Count > 0)
        {
            AddGaps(body, section.Gaps, language);
            AddEmptyParagraph(body);
        }
    }

    private Table CreateMetadataTable(GeneratedReport report, string? language = null)
    {
        var table = new Table();
        
        // Table properties
        var tblProp = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        table.AppendChild(tblProp);
        
        // Add rows with localized labels
        var reportModeLabel = _localizationService.GetLabel("label.report-mode", language);
        var reportScopeLabel = _localizationService.GetLabel("label.report-scope", language);
        var generatedByLabel = _localizationService.GetLabel("label.generated-by", language);
        var generatedAtLabel = _localizationService.GetLabel("label.generated-at", language);
        var totalSectionsLabel = _localizationService.GetLabel("label.total-sections", language);
        
        AddTableRow(table, reportModeLabel, report.Period.ReportingMode);
        AddTableRow(table, reportScopeLabel, report.Period.ReportScope);
        AddTableRow(table, generatedByLabel, report.GeneratedByName);
        AddTableRow(table, generatedAtLabel, _localizationService.FormatDate(report.GeneratedAt, language));
        AddTableRow(table, totalSectionsLabel, report.Sections.Count.ToString());
        
        return table;
    }

    private Table CreateDataPointsTable(List<DataPointSnapshot> dataPoints, string? language = null)
    {
        var table = new Table();
        
        // Table properties
        var tblProp = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        table.AppendChild(tblProp);
        
        // Header row with localized labels
        var fieldLabel = _localizationService.GetLabel("label.field", language);
        var valueLabel = _localizationService.GetLabel("label.value", language);
        var unitLabel = _localizationService.GetLabel("label.unit", language);
        var statusLabel = _localizationService.GetLabel("label.point-status", language);
        
        var headerRow = new TableRow();
        AddTableCell(headerRow, fieldLabel, true);
        AddTableCell(headerRow, valueLabel, true);
        AddTableCell(headerRow, unitLabel, true);
        AddTableCell(headerRow, statusLabel, true);
        table.AppendChild(headerRow);
        
        // Data rows
        foreach (var dp in dataPoints)
        {
            var row = new TableRow();
            AddTableCell(row, dp.Title ?? "");
            AddTableCell(row, dp.Value ?? "-");
            AddTableCell(row, dp.Unit ?? "-");
            AddTableCell(row, dp.Status ?? "-");
            table.AppendChild(row);
        }
        
        return table;
    }

    private void AddTableRow(Table table, string label, string value)
    {
        var row = new TableRow();
        
        var labelCell = new TableCell();
        labelCell.Append(new Paragraph(new Run(new Text(label))));
        var labelProps = new TableCellProperties(
            new TableCellWidth { Width = "2000", Type = TableWidthUnitValues.Pct }
        );
        labelCell.Append(labelProps);
        row.Append(labelCell);
        
        var valueCell = new TableCell();
        valueCell.Append(new Paragraph(new Run(new Text(value))));
        var valueProps = new TableCellProperties(
            new TableCellWidth { Width = "3000", Type = TableWidthUnitValues.Pct }
        );
        valueCell.Append(valueProps);
        row.Append(valueCell);
        
        table.Append(row);
    }

    private void AddTableCell(TableRow row, string text, bool isHeader = false)
    {
        var cell = new TableCell();
        var para = new Paragraph(new Run(new Text(text)));
        
        if (isHeader)
        {
            var runProps = new RunProperties(new Bold());
            para.PrependChild(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
            para.GetFirstChild<Run>()?.PrependChild(runProps);
        }
        
        cell.Append(para);
        row.Append(cell);
    }

    private void AddAssumptions(Body body, List<AssumptionRecord> assumptions, string? language = null)
    {
        var assumptionsLabel = _localizationService.GetLabel("label.assumptions", language);
        var confidenceLabel = _localizationService.GetLabel("label.confidence-level", language);
        
        AddParagraph(body, assumptionsLabel, "Heading3");
        
        foreach (var assumption in assumptions)
        {
            var text = $"• {assumption.Description ?? "No description"}";
            if (!string.IsNullOrWhiteSpace(assumption.ConfidenceLevel))
            {
                text += $" ({confidenceLabel} {assumption.ConfidenceLevel})";
            }
            
            var para = AddParagraph(body, text, null);
            // Add orange-ish highlighting via shading
            var paraProp = para.GetFirstChild<ParagraphProperties>();
            if (paraProp == null)
            {
                paraProp = new ParagraphProperties();
                para.PrependChild(paraProp);
            }
            paraProp.AppendChild(new Shading 
            { 
                Val = ShadingPatternValues.Clear, 
                Fill = "FFE5CC" // Light orange
            });
        }
    }

    private void AddGaps(Body body, List<GapRecord> gaps, string? language = null)
    {
        var gapsLabel = _localizationService.GetLabel("label.data-gaps", language);
        var reasonLabel = _localizationService.GetLabel("label.reason", language);
        
        AddParagraph(body, gapsLabel, "Heading3");
        
        foreach (var gap in gaps)
        {
            var text = $"• {gap.Description ?? "No description"}";
            if (!string.IsNullOrWhiteSpace(gap.MissingReason))
            {
                text += $" ({reasonLabel} {gap.MissingReason})";
            }
            
            var para = AddParagraph(body, text, null);
            // Add red-ish highlighting via shading
            var paraProp = para.GetFirstChild<ParagraphProperties>();
            if (paraProp == null)
            {
                paraProp = new ParagraphProperties();
                para.PrependChild(paraProp);
            }
            paraProp.AppendChild(new Shading 
            { 
                Val = ShadingPatternValues.Clear, 
                Fill = "FFCCCC" // Light red
            });
        }
    }

    private Paragraph AddParagraph(Body body, string text, string? styleId = null, bool centered = false)
    {
        var para = body.AppendChild(new Paragraph());
        var run = para.AppendChild(new Run());
        run.AppendChild(new Text(text));
        
        var paraProps = new ParagraphProperties();
        
        if (!string.IsNullOrWhiteSpace(styleId))
        {
            paraProps.AppendChild(new ParagraphStyleId { Val = styleId });
        }
        
        if (centered)
        {
            paraProps.AppendChild(new Justification { Val = JustificationValues.Center });
        }
        
        if (paraProps.HasChildren)
        {
            para.PrependChild(paraProps);
        }
        
        return para;
    }

    private void AddEmptyParagraph(Body body)
    {
        body.AppendChild(new Paragraph());
    }

    private void AddPageBreak(Body body)
    {
        var para = body.AppendChild(new Paragraph());
        var run = para.AppendChild(new Run());
        run.AppendChild(new Break { Type = BreakValues.Page });
    }

    private void MakeParagraphItalic(Paragraph para)
    {
        var run = para.GetFirstChild<Run>();
        if (run != null)
        {
            var runProps = new RunProperties(new Italic());
            run.PrependChild(runProps);
        }
    }

    private void AddPageNumbering(MainDocumentPart mainPart, ExportMetadata exportMetadata)
    {
        // Create footer part
        var footerPart = mainPart.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer();
        
        // Export metadata line
        var metaPara = footerPart.Footer.AppendChild(new Paragraph());
        var metaParaProps = new ParagraphProperties(
            new Justification { Val = JustificationValues.Center }
        );
        metaPara.AppendChild(metaParaProps);
        var metaRun = metaPara.AppendChild(new Run());
        var metaRunProps = new RunProperties(new FontSize { Val = "14" }); // 7pt (half of 14)
        metaRun.AppendChild(metaRunProps);
        metaRun.AppendChild(new Text($"Export Schema: {exportMetadata.SchemaIdentifier} (v{exportMetadata.SchemaVersion}) | Export ID: {exportMetadata.ExportId}"));
        
        // Page numbers
        var para = footerPart.Footer.AppendChild(new Paragraph());
        var paraProps = new ParagraphProperties(
            new Justification { Val = JustificationValues.Center }
        );
        para.AppendChild(paraProps);
        
        var run = para.AppendChild(new Run());
        run.AppendChild(new Text("Page "));
        
        var fieldRun = para.AppendChild(new Run());
        fieldRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Begin });
        
        var instrRun = para.AppendChild(new Run());
        instrRun.AppendChild(new FieldCode { Space = SpaceProcessingModeValues.Preserve, Text = " PAGE " });
        
        var endFieldRun = para.AppendChild(new Run());
        endFieldRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.End });
        
        var ofRun = para.AppendChild(new Run());
        ofRun.AppendChild(new Text(" / "));
        
        var totalFieldRun = para.AppendChild(new Run());
        totalFieldRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Begin });
        
        var totalInstrRun = para.AppendChild(new Run());
        totalInstrRun.AppendChild(new FieldCode { Space = SpaceProcessingModeValues.Preserve, Text = " NUMPAGES " });
        
        var totalEndRun = para.AppendChild(new Run());
        totalEndRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.End });
        
        footerPart.Footer.Save();
        
        // Reference footer in document
        var footerId = mainPart.GetIdOfPart(footerPart);
        
        var sectionProps = mainPart.Document.Body!.GetFirstChild<SectionProperties>();
        if (sectionProps == null)
        {
            sectionProps = new SectionProperties();
            mainPart.Document.Body.AppendChild(sectionProps);
        }
        
        var footerRef = new FooterReference { Type = HeaderFooterValues.Default, Id = footerId };
        sectionProps.AppendChild(footerRef);
    }

    private void AddAttachmentsAppendix(Body body, GeneratedReport report, DocxExportOptions options)
    {
        // Title
        AddParagraph(body, "Appendix: Evidence and Attachments", "Heading1");
        AddEmptyParagraph(body);
        
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
            var emptyPara = AddParagraph(body, "No evidence or attachments are associated with this report.", null);
            MakeParagraphItalic(emptyPara);
            return;
        }
        
        // Calculate total size in MB
        var totalSizeMB = totalSize / (1024.0 * 1024.0);
        var maxSizeMB = options.MaxAttachmentSizeMB;
        
        // Show warning if size exceeds limit
        if (totalSizeMB > maxSizeMB)
        {
            var warningPara = AddParagraph(body, "⚠ File Size Warning", "Heading3");
            var warningDetailPara = AddParagraph(body, 
                $"Total attachment size ({totalSizeMB:F2} MB) exceeds the recommended limit ({maxSizeMB} MB). " +
                "Only attachment metadata is included in this export. " +
                "For full attachments, consider using the audit package export (ZIP) or external file sharing.", null);
            
            // Add warning highlighting
            var warningProps = warningDetailPara.GetFirstChild<ParagraphProperties>();
            if (warningProps == null)
            {
                warningProps = new ParagraphProperties();
                warningDetailPara.PrependChild(warningProps);
            }
            warningProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = "FFE5CC" }); // Light orange
            
            AddEmptyParagraph(body);
        }
        
        // Show restriction notice if applicable
        if (restrictedCount > 0)
        {
            var restrictPara = AddParagraph(body, "🔒 Restricted Attachments", "Heading3");
            var restrictDetailPara = AddParagraph(body,
                $"{restrictedCount} attachment(s) are restricted and not accessible to the current user. " +
                "These attachments are marked with 🔒 and excluded from this export.", null);
            
            // Add restriction highlighting
            var restrictProps = restrictDetailPara.GetFirstChild<ParagraphProperties>();
            if (restrictProps == null)
            {
                restrictProps = new ParagraphProperties();
                restrictDetailPara.PrependChild(restrictProps);
            }
            restrictProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = "FFCCCC" }); // Light red
            
            AddEmptyParagraph(body);
        }
        
        // Summary
        var summaryText = $"Total Attachments: {allEvidence.Count} | Total Size: {FormatFileSize(totalSize)}";
        if (restrictedCount > 0)
        {
            summaryText += $" | Accessible: {allEvidence.Count - restrictedCount}";
        }
        AddParagraph(body, summaryText, null);
        AddEmptyParagraph(body);
        
        // Evidence table
        var table = new Table();
        
        // Table properties
        var tblProp = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        table.AppendChild(tblProp);
        
        // Header row
        var headerRow = new TableRow();
        AddTableCell(headerRow, "Section", true);
        AddTableCell(headerRow, "Title", true);
        AddTableCell(headerRow, "File Name", true);
        AddTableCell(headerRow, "Size", true);
        AddTableCell(headerRow, "Integrity", true);
        AddTableCell(headerRow, "Uploaded", true);
        table.AppendChild(headerRow);
        
        // Data rows
        foreach (var (sectionTitle, evidence) in allEvidence)
        {
            var row = new TableRow();
            
            AddTableCell(row, sectionTitle);
            
            var titleText = evidence.IsAccessible ? evidence.Title : $"🔒 {evidence.Title}";
            AddTableCell(row, titleText);
            
            AddTableCell(row, evidence.FileName ?? "-");
            AddTableCell(row, FormatFileSize(evidence.FileSize));
            
            var integrityText = evidence.IntegrityStatus switch
            {
                "valid" => "✓ Valid",
                "failed" => "✗ Failed",
                _ => "? Not Checked"
            };
            AddTableCell(row, integrityText);
            
            AddTableCell(row, FormatDate(evidence.UploadedAt));
            
            // Apply red shading for restricted items
            if (!evidence.IsAccessible)
            {
                foreach (var cell in row.Elements<TableCell>())
                {
                    var cellProps = cell.GetFirstChild<TableCellProperties>();
                    if (cellProps == null)
                    {
                        cellProps = new TableCellProperties();
                        cell.PrependChild(cellProps);
                    }
                    cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = "FFCCCC" });
                }
            }
            
            table.AppendChild(row);
        }
        
        body.AppendChild(table);
        AddEmptyParagraph(body);
        
        // Additional notes
        AddParagraph(body, "Notes:", "Heading3");
        AddParagraph(body, "• This appendix lists all evidence and attachments referenced in the report.", null);
        AddParagraph(body, "• Attachment checksums and integrity status ensure file authenticity.", null);
        AddParagraph(body, "• For access to actual files, download them from the ESG Report Studio or request an audit package.", null);
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
