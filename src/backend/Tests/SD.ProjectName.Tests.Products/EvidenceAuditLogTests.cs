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
    }
}
