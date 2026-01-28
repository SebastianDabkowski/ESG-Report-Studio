using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class EvidenceChainOfCustodyTests
    {
        private static void CreateTestConfiguration(InMemoryReportStore store)
        {
            // Create organization
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

            // Create organizational unit
            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Test Organization Unit",
                Description = "Default unit for testing",
                CreatedBy = "test-user"
            });

            // Create reporting period
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
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);
            
            return section.Id;
        }

        [Fact]
        public void CreateEvidence_WithMetadata_ShouldStoreChecksumAndFileSize()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var checksum = "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2";
            long fileSize = 1024 * 50; // 50KB
            var contentType = "application/pdf";

            // Act
            var (isValid, errorMessage, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                "Test description",
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                fileSize,
                checksum,
                contentType
            );

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(evidence);
            Assert.Equal(fileSize, evidence.FileSize);
            Assert.Equal(checksum, evidence.Checksum);
            Assert.Equal(contentType, evidence.ContentType);
            Assert.Equal("valid", evidence.IntegrityStatus);
        }

        [Fact]
        public void CreateEvidence_WithoutChecksum_ShouldHaveNotCheckedStatus()
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
                null, // No checksum
                "application/pdf"
            );

            // Assert
            Assert.True(isValid);
            Assert.NotNull(evidence);
            Assert.Equal("not-checked", evidence.IntegrityStatus);
        }

        [Fact]
        public void ValidateEvidenceIntegrity_WithMatchingChecksum_ShouldPass()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var checksum = "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2";
            var (_, _, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                null,
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                1024,
                checksum,
                "application/pdf"
            );

            // Act
            var (isValid, errorMessage) = store.ValidateEvidenceIntegrity(evidence!.Id, checksum);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            
            var updatedEvidence = store.GetEvidenceById(evidence.Id);
            Assert.Equal("valid", updatedEvidence!.IntegrityStatus);
        }

        [Fact]
        public void ValidateEvidenceIntegrity_WithMismatchedChecksum_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var originalChecksum = "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2";
            var wrongChecksum = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";
            
            var (_, _, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                null,
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                1024,
                originalChecksum,
                "application/pdf"
            );

            // Act
            var (isValid, errorMessage) = store.ValidateEvidenceIntegrity(evidence!.Id, wrongChecksum);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("Checksum mismatch", errorMessage);
            
            var updatedEvidence = store.GetEvidenceById(evidence.Id);
            Assert.Equal("failed", updatedEvidence!.IntegrityStatus);
        }

        [Fact]
        public void CanPublishEvidence_WithValidIntegrity_ShouldReturnTrue()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var (_, _, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                null,
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                1024,
                "checksum123",
                "application/pdf"
            );

            // Act
            var canPublish = store.CanPublishEvidence(evidence!.Id);

            // Assert
            Assert.True(canPublish);
        }

        [Fact]
        public void CanPublishEvidence_WithFailedIntegrity_ShouldReturnFalse()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var originalChecksum = "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2";
            var wrongChecksum = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";
            
            var (_, _, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                null,
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                1024,
                originalChecksum,
                "application/pdf"
            );

            // Fail the integrity check
            store.ValidateEvidenceIntegrity(evidence!.Id, wrongChecksum);

            // Act
            var canPublish = store.CanPublishEvidence(evidence.Id);

            // Assert
            Assert.False(canPublish);
        }

        [Fact]
        public void LogEvidenceAccess_ShouldCreateAccessLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var (_, _, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                null,
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                1024,
                "checksum123",
                "application/pdf"
            );

            // Act
            store.LogEvidenceAccess(
                evidence!.Id,
                "user-2",
                "Jane Doe",
                "download",
                "Audit review"
            );

            // Assert
            var accessLog = store.GetEvidenceAccessLog(evidence.Id);
            Assert.Single(accessLog);
            
            var logEntry = accessLog.First();
            Assert.Equal(evidence.Id, logEntry.EvidenceId);
            Assert.Equal("user-2", logEntry.UserId);
            Assert.Equal("Jane Doe", logEntry.UserName);
            Assert.Equal("download", logEntry.Action);
            Assert.Equal("Audit review", logEntry.Purpose);
            Assert.NotEmpty(logEntry.AccessedAt);
        }

        [Fact]
        public void GetEvidenceAccessLog_WithMultipleEntries_ShouldReturnAllForEvidence()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var (_, _, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                null,
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                1024,
                "checksum123",
                "application/pdf"
            );

            // Act - Log multiple accesses
            store.LogEvidenceAccess(evidence!.Id, "user-2", "Jane Doe", "download", "Audit");
            store.LogEvidenceAccess(evidence.Id, "user-3", "Bob Smith", "view", "Review");
            store.LogEvidenceAccess(evidence.Id, "user-2", "Jane Doe", "validate", "Verification");

            // Assert
            var accessLog = store.GetEvidenceAccessLog(evidence.Id);
            Assert.Equal(3, accessLog.Count);
            
            // Verify they're sorted by most recent first
            Assert.Equal("validate", accessLog[0].Action);
            Assert.Equal("view", accessLog[1].Action);
            Assert.Equal("download", accessLog[2].Action);
        }

        [Fact]
        public void GetEvidenceAccessLog_WithoutEvidenceId_ShouldReturnAllLogs()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var (_, _, evidence1) = store.CreateEvidence(
                sectionId, "Evidence 1", null, "file1.pdf",
                "/api/evidence/files/test-1", null, "user-1", 1024, "checksum1", "application/pdf"
            );
            
            var (_, _, evidence2) = store.CreateEvidence(
                sectionId, "Evidence 2", null, "file2.pdf",
                "/api/evidence/files/test-2", null, "user-1", 2048, "checksum2", "application/pdf"
            );

            // Log access to both
            store.LogEvidenceAccess(evidence1!.Id, "user-2", "Jane", "download", null);
            store.LogEvidenceAccess(evidence2!.Id, "user-3", "Bob", "download", null);

            // Act
            var allLogs = store.GetEvidenceAccessLog();

            // Assert
            Assert.Equal(2, allLogs.Count);
        }

        [Fact]
        public void ValidateEvidenceIntegrity_WithoutChecksum_ShouldFail()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var (_, _, evidence) = store.CreateEvidence(
                sectionId,
                "Test Evidence",
                null,
                "test-file.pdf",
                "/api/evidence/files/test-123",
                null,
                "user-1",
                1024,
                null, // No checksum
                "application/pdf"
            );

            // Act
            var (isValid, errorMessage) = store.ValidateEvidenceIntegrity(evidence!.Id, "somechecksum");

            // Assert
            Assert.False(isValid);
            Assert.Contains("does not have a checksum", errorMessage);
        }
    }
}
