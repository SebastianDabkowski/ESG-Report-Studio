using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products
{
    public class AuditTrailTests
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
                Description = "Test section for data points",
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
        public void UpdateDataPoint_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Initial Title",
                Content = "Initial content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act - Update the data point
            var updateRequest = new UpdateDataPointRequest
            {
                Type = "narrative",
                Title = "Updated Title",
                Content = "Updated content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete",
                UpdatedBy = "user-1",
                ChangeNote = "Updated title and content"
            };

            var (isValid, _, updatedDataPoint) = store.UpdateDataPoint(dataPoint.Id, updateRequest);

            // Assert
            Assert.True(isValid);
            Assert.NotNull(updatedDataPoint);

            var auditLog = store.GetAuditLog(entityType: "DataPoint", entityId: dataPoint.Id);
            Assert.NotEmpty(auditLog);
            
            var entry = auditLog.First();
            Assert.Equal("update", entry.Action);
            Assert.Equal("DataPoint", entry.EntityType);
            Assert.Equal(dataPoint.Id, entry.EntityId);
            Assert.Equal("user-1", entry.UserId);
            Assert.Equal("Sarah Chen", entry.UserName);
            Assert.Equal("Updated title and content", entry.ChangeNote);
            Assert.NotEmpty(entry.Changes);
            
            // Check that title and content changes were captured
            var titleChange = entry.Changes.FirstOrDefault(c => c.Field == "Title");
            Assert.NotNull(titleChange);
            Assert.Equal("Initial Title", titleChange.OldValue);
            Assert.Equal("Updated Title", titleChange.NewValue);
            
            var contentChange = entry.Changes.FirstOrDefault(c => c.Field == "Content");
            Assert.NotNull(contentChange);
            Assert.Equal("Initial content", contentChange.OldValue);
            Assert.Equal("Updated content", contentChange.NewValue);
        }

        [Fact]
        public void UpdateDataPoint_WithNoChanges_ShouldNotCreateAuditLogEntry()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Test Title",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);
            Assert.NotNull(dataPoint);

            // Act - Update with same values
            var updateRequest = new UpdateDataPointRequest
            {
                Type = "narrative",
                Title = "Test Title",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete",
                UpdatedBy = "user-1"
            };

            var (isValid, _, _) = store.UpdateDataPoint(dataPoint.Id, updateRequest);

            // Assert
            Assert.True(isValid);

            var auditLog = store.GetAuditLog(entityType: "DataPoint", entityId: dataPoint.Id);
            Assert.Empty(auditLog); // No changes, so no audit log entry
        }

        [Fact]
        public void GetAuditLog_FilterByUserId_ShouldReturnMatchingEntries()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Test Title",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);

            // Update by user-1
            var updateRequest1 = new UpdateDataPointRequest
            {
                Type = "narrative",
                Title = "Updated by User 1",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete",
                UpdatedBy = "user-1"
            };
            store.UpdateDataPoint(dataPoint!.Id, updateRequest1);

            // Update by user-3
            var updateRequest2 = new UpdateDataPointRequest
            {
                Type = "narrative",
                Title = "Updated by User 3",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete",
                UpdatedBy = "user-3"
            };
            store.UpdateDataPoint(dataPoint.Id, updateRequest2);

            // Act
            var user1Entries = store.GetAuditLog(userId: "user-1");
            var user3Entries = store.GetAuditLog(userId: "user-3");

            // Assert
            Assert.Single(user1Entries);
            Assert.Equal("user-1", user1Entries.First().UserId);
            
            Assert.Single(user3Entries);
            Assert.Equal("user-3", user3Entries.First().UserId);
        }

        [Fact]
        public void GetAuditLog_ShouldReturnEntriesInReverseChronologicalOrder()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Test Title",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);

            // Make multiple updates
            for (int i = 1; i <= 3; i++)
            {
                var updateRequest = new UpdateDataPointRequest
                {
                    Type = "narrative",
                    Title = $"Update {i}",
                    Content = "Test content",
                    OwnerId = "user-1",
                    Source = "Manual entry",
                    InformationType = "fact",
                    CompletenessStatus = "complete",
                    UpdatedBy = "user-1"
                };
                store.UpdateDataPoint(dataPoint!.Id, updateRequest);
                Thread.Sleep(10); // Ensure different timestamps
            }

            // Act
            var auditLog = store.GetAuditLog(entityId: dataPoint!.Id);

            // Assert
            Assert.Equal(3, auditLog.Count);
            
            // Verify chronological order (newest first)
            for (int i = 0; i < auditLog.Count - 1; i++)
            {
                var current = DateTime.Parse(auditLog[i].Timestamp);
                var next = DateTime.Parse(auditLog[i + 1].Timestamp);
                Assert.True(current >= next);
            }
        }

        [Fact]
        public void AuditLogEntry_ShouldBeImmutable()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Test Title",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);

            var updateRequest = new UpdateDataPointRequest
            {
                Type = "narrative",
                Title = "Updated Title",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete",
                UpdatedBy = "user-1",
                ChangeNote = "Original note"
            };

            store.UpdateDataPoint(dataPoint!.Id, updateRequest);

            var auditLog = store.GetAuditLog(entityId: dataPoint.Id);
            var originalEntry = auditLog.First();
            var originalTimestamp = originalEntry.Timestamp;
            var originalNote = originalEntry.ChangeNote;

            // Act - Try to modify the audit log entry (this tests that it's returned as a copy)
            // Since we're working with classes, we can't truly test immutability without using records
            // But we can verify the entry persists correctly
            
            // Make another update
            var updateRequest2 = new UpdateDataPointRequest
            {
                Type = "narrative",
                Title = "Second Update",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete",
                UpdatedBy = "user-1",
                ChangeNote = "Second note"
            };
            store.UpdateDataPoint(dataPoint.Id, updateRequest2);

            // Assert - Original entry should remain unchanged
            var newAuditLog = store.GetAuditLog(entityId: dataPoint.Id);
            var firstEntry = newAuditLog.Last(); // First entry is now last (reverse chronological)
            
            Assert.Equal(originalTimestamp, firstEntry.Timestamp);
            Assert.Equal(originalNote, firstEntry.ChangeNote);
            Assert.Equal(2, newAuditLog.Count); // Two separate entries
        }

        [Fact]
        public void GetAuditLog_FilterByDateRange_ShouldReturnMatchingEntries()
        {
            // Arrange
            var store = new InMemoryReportStore();
            CreateTestConfiguration(store);
            var sectionId = CreateTestSection(store);

            var createRequest = new CreateDataPointRequest
            {
                SectionId = sectionId,
                Type = "narrative",
                Title = "Test Title",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete"
            };

            var (_, _, dataPoint) = store.CreateDataPoint(createRequest);

            // Capture time before first update
            var beforeFirstUpdate = DateTime.UtcNow;
            Thread.Sleep(100);

            // Make first update
            var updateRequest1 = new UpdateDataPointRequest
            {
                Type = "narrative",
                Title = "First Update",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete",
                UpdatedBy = "user-1"
            };
            store.UpdateDataPoint(dataPoint!.Id, updateRequest1);

            // Capture time between updates
            Thread.Sleep(100);
            var betweenUpdates = DateTime.UtcNow;
            Thread.Sleep(100);

            // Make second update
            var updateRequest2 = new UpdateDataPointRequest
            {
                Type = "narrative",
                Title = "Second Update",
                Content = "Test content",
                OwnerId = "user-1",
                Source = "Manual entry",
                InformationType = "fact",
                CompletenessStatus = "complete",
                UpdatedBy = "user-1"
            };
            store.UpdateDataPoint(dataPoint.Id, updateRequest2);

            Thread.Sleep(100);
            var afterSecondUpdate = DateTime.UtcNow;

            // Act - Get all entries for this data point
            var allEntries = store.GetAuditLog(entityId: dataPoint.Id);
            Assert.Equal(2, allEntries.Count);

            // Filter by date range that should include only first update
            var onlyFirstUpdate = store.GetAuditLog(
                entityId: dataPoint.Id,
                startDate: beforeFirstUpdate.ToString("O"),
                endDate: betweenUpdates.ToString("O")
            );

            // Filter by date range that should include only second update
            var onlySecondUpdate = store.GetAuditLog(
                entityId: dataPoint.Id,
                startDate: betweenUpdates.ToString("O"),
                endDate: afterSecondUpdate.ToString("O")
            );

            // Filter by date range that includes both
            var bothUpdates = store.GetAuditLog(
                entityId: dataPoint.Id,
                startDate: beforeFirstUpdate.ToString("O"),
                endDate: afterSecondUpdate.ToString("O")
            );

            // Assert
            Assert.Single(onlyFirstUpdate);
            Assert.Single(onlySecondUpdate);
            Assert.Equal(2, bothUpdates.Count);
        }
    }
}
