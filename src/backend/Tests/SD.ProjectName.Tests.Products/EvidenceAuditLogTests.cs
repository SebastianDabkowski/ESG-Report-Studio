using ARP.ESG_ReportStudio.API.Reporting;
using System;
using System.Linq;

namespace SD.ProjectName.Tests.Products
{
    public class EvidenceAuditLogTests
    {
        private static void CreateTestConfiguration(InMemoryReportStore store)
        {
            store.CreateOrganization(new CreateOrganizationRequest
            {
                Name = "Test Organization",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "test-user",
                CoverageType = "full",
                CoverageJustification = "Test coverage"
            });

            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Test Organization Unit",
                Description = "Default unit for testing",
                CreatedBy = "test-user"
            });

            store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "user-1",
                OwnerName = "Test Owner"
            });
        }

        private static string CreateTestSection(InMemoryReportStore store)
        {
            var snapshot = store.GetSnapshot();
            var periodId = snapshot.Periods.First().Id;
            
            var section = new ReportSection
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Title = "Test Section",
                Category = "environmental",
                Description = "Test section for evidence",
                OwnerId = "user-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as System.Collections.Generic.List<ReportSection>;
            sections!.Add(section);
            
            return section.Id;
        }

        [Fact]
        public void CreateEvidence_ShouldLogAuditEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            // Act
            var (isValid, errorMessage, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                "Test description",
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                1024,
                "checksum123",
                "application/pdf"
            );

            // Assert
            Assert.True(isValid);
            Assert.NotNull(evidence);

            var auditLog = store.GetAuditLog(entityType: "Evidence", entityId: evidence!.Id);
            Assert.Single(auditLog);
            
            var entry = auditLog.First();
            Assert.Equal("create", entry.Action);
            Assert.Equal("Evidence", entry.EntityType);
            Assert.Equal(evidence.Id, entry.EntityId);
            Assert.Equal("user-1", entry.UserId);
            Assert.Contains("Created evidence", entry.ChangeNote);
        }

        [Fact]
        public void DeleteEvidence_ShouldLogAuditEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var (isValid, errorMessage, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                "Test description",
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                1024,
                "checksum123",
                "application/pdf"
            );

            // Act
            var deleted = store.DeleteEvidence(evidence!.Id, "user-2");

            // Assert
            Assert.True(deleted);

            var auditLog = store.GetAuditLog(entityType: "Evidence", entityId: evidence.Id);
            Assert.Equal(2, auditLog.Count); // create + delete
            
            // Audit log is in reverse chronological order (newest first)
            var deleteEntry = auditLog[0];
            Assert.Equal("delete", deleteEntry.Action);
            Assert.Equal("user-2", deleteEntry.UserId);
            Assert.Contains("Deleted evidence", deleteEntry.ChangeNote);
            
            var createEntry = auditLog[1];
            Assert.Equal("create", createEntry.Action);
            Assert.Equal("user-1", createEntry.UserId);
        }

        [Fact]
        public void LinkEvidenceToDataPoint_ShouldLogAuditEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var (isValid, errorMessage, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                "Test description",
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                1024,
                "checksum123",
                "application/pdf"
            );

            // Create a data point to link to
            var dataPoint = new DataPoint
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Title = "Test Data Point",
                Type = "metric",
                Unit = "kg",
                OwnerId = "user-1",
                CompletenessStatus = "empty",
                EvidenceIds = new System.Collections.Generic.List<string>(),
                CreatedAt = DateTime.UtcNow.ToString("O"),
                UpdatedAt = DateTime.UtcNow.ToString("O")
            };
            
            var dataPointsField = typeof(InMemoryReportStore).GetField("_dataPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dataPoints = dataPointsField!.GetValue(store) as System.Collections.Generic.List<DataPoint>;
            dataPoints!.Add(dataPoint);

            // Act
            var (linkValid, linkError) = store.LinkEvidenceToDataPoint(evidence!.Id, dataPoint.Id, "user-2");

            // Assert
            Assert.True(linkValid);

            var auditLog = store.GetAuditLog(entityType: "Evidence", entityId: evidence.Id);
            Assert.Equal(2, auditLog.Count); // create + link
            
            var linkEntry = auditLog[0];
            Assert.Equal("link", linkEntry.Action);
            Assert.Equal("user-2", linkEntry.UserId);
            Assert.Contains($"Linked evidence to data point {dataPoint.Id}", linkEntry.ChangeNote);
        }

        [Fact]
        public void UnlinkEvidenceFromDataPoint_ShouldLogAuditEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var (isValid, errorMessage, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                "Test description",
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                1024,
                "checksum123",
                "application/pdf"
            );

            // Create a data point to link to
            var dataPoint = new DataPoint
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Title = "Test Data Point",
                Type = "metric",
                Unit = "kg",
                OwnerId = "user-1",
                CompletenessStatus = "empty",
                EvidenceIds = new System.Collections.Generic.List<string>(),
                CreatedAt = DateTime.UtcNow.ToString("O"),
                UpdatedAt = DateTime.UtcNow.ToString("O")
            };
            
            var dataPointsField = typeof(InMemoryReportStore).GetField("_dataPoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dataPoints = dataPointsField!.GetValue(store) as System.Collections.Generic.List<DataPoint>;
            dataPoints!.Add(dataPoint);

            // Link first
            var (linkValid, linkError) = store.LinkEvidenceToDataPoint(evidence!.Id, dataPoint.Id, "user-2");
            Assert.True(linkValid);

            // Act - Unlink
            var (unlinkValid, unlinkError) = store.UnlinkEvidenceFromDataPoint(evidence.Id, dataPoint.Id, "user-3");

            // Assert
            Assert.True(unlinkValid);

            var auditLog = store.GetAuditLog(entityType: "Evidence", entityId: evidence.Id);
            Assert.Equal(3, auditLog.Count); // create + link + unlink
            
            var unlinkEntry = auditLog[0];
            Assert.Equal("unlink", unlinkEntry.Action);
            Assert.Equal("user-3", unlinkEntry.UserId);
            Assert.Contains($"Unlinked evidence from data point {dataPoint.Id}", unlinkEntry.ChangeNote);
            
            var linkEntry = auditLog[1];
            Assert.Equal("link", linkEntry.Action);
            Assert.Equal("user-2", linkEntry.UserId);
        }
    }
}
