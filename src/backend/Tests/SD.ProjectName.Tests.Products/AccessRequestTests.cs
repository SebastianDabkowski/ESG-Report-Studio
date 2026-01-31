using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace SD.ProjectName.Tests.Products
{
    public class AccessRequestTests
    {
        private static InMemoryReportStore CreateStoreWithTestData()
        {
            var store = new InMemoryReportStore();
            
            // Create organization
            store.CreateOrganization(new CreateOrganizationRequest
            {
                Name = "Test Organization",
                LegalForm = "LLC",
                Country = "US",
                Identifier = "12345",
                CreatedBy = "admin-1",
                CoverageType = "full",
                CoverageJustification = "Test coverage"
            });

            // Create organizational unit
            store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
            {
                Name = "Test Organization Unit",
                Description = "Default unit for testing",
                CreatedBy = "admin-1"
            });

            // Create users
            var usersField = typeof(InMemoryReportStore).GetField("_users", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var users = usersField!.GetValue(store) as List<User>;
            
            users!.Add(new User
            {
                Id = "requester-1",
                Name = "Regular User",
                Email = "user@example.com",
                Role = "contributor",
                RoleIds = new List<string> { "contributor" }
            });
            
            users.Add(new User
            {
                Id = "admin-1",
                Name = "Admin User",
                Email = "admin@example.com",
                Role = "admin",
                RoleIds = new List<string> { "admin" }
            });

            // Create reporting period
            store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
            {
                Name = "FY 2024",
                StartDate = "2024-01-01",
                EndDate = "2024-12-31",
                ReportingMode = "simplified",
                ReportScope = "single-company",
                OwnerId = "period-owner",
                OwnerName = "Period Owner"
            });

            // Create section
            var snapshot = store.GetSnapshot();
            var periodId = snapshot.Periods.First().Id;
            
            var section = new ReportSection
            {
                Id = "section-1",
                PeriodId = periodId,
                Title = "Test Section",
                Category = "environmental",
                Description = "Test section",
                OwnerId = "admin-1",
                Status = "draft",
                Completeness = "empty",
                Order = 1
            };
            
            var sectionsField = typeof(InMemoryReportStore).GetField("_sections", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sections = sectionsField!.GetValue(store) as List<ReportSection>;
            sections!.Add(section);

            return store;
        }

        [Fact]
        public void CreateAccessRequest_WithValidData_ShouldCreateRequest()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var request = new CreateAccessRequestRequest
            {
                RequestedBy = "requester-1",
                ResourceType = "section",
                ResourceId = "section-1",
                Reason = "I need to review this section for my analysis"
            };

            // Act
            var (isValid, errorMessage, accessRequest) = store.CreateAccessRequest(request);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(accessRequest);
            Assert.Equal("requester-1", accessRequest!.RequestedBy);
            Assert.Equal("Regular User", accessRequest.RequestedByName);
            Assert.Equal("section", accessRequest.ResourceType);
            Assert.Equal("section-1", accessRequest.ResourceId);
            Assert.Equal("Test Section", accessRequest.ResourceName);
            Assert.Equal("I need to review this section for my analysis", accessRequest.Reason);
            Assert.Equal("pending", accessRequest.Status);
            Assert.Null(accessRequest.ReviewedBy);
        }

        [Fact]
        public void CreateAccessRequest_WithInvalidUser_ShouldFail()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var request = new CreateAccessRequestRequest
            {
                RequestedBy = "invalid-user",
                ResourceType = "section",
                ResourceId = "section-1",
                Reason = "Need access"
            };

            // Act
            var (isValid, errorMessage, accessRequest) = store.CreateAccessRequest(request);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("not found", errorMessage);
            Assert.Null(accessRequest);
        }

        [Fact]
        public void CreateAccessRequest_WithInvalidResourceType_ShouldFail()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var request = new CreateAccessRequestRequest
            {
                RequestedBy = "requester-1",
                ResourceType = "invalid",
                ResourceId = "section-1",
                Reason = "Need access"
            };

            // Act
            var (isValid, errorMessage, accessRequest) = store.CreateAccessRequest(request);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("ResourceType must be", errorMessage);
            Assert.Null(accessRequest);
        }

        [Fact]
        public void CreateAccessRequest_ShouldCreateAuditLogEntry()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var request = new CreateAccessRequestRequest
            {
                RequestedBy = "requester-1",
                ResourceType = "section",
                ResourceId = "section-1",
                Reason = "Need access for review"
            };

            // Act
            var (isValid, errorMessage, accessRequest) = store.CreateAccessRequest(request);

            // Assert
            Assert.True(isValid);
            var auditLogs = store.GetAuditLog(null, null, null, null);
            var accessRequestLog = auditLogs.FirstOrDefault(log => 
                log.Action == "create-access-request" && 
                log.EntityId == accessRequest!.Id);
            
            Assert.NotNull(accessRequestLog);
            Assert.Equal("requester-1", accessRequestLog!.UserId);
            Assert.Equal("access-request", accessRequestLog.EntityType);
            Assert.Contains(accessRequestLog.Changes, c => c.Field == "ResourceType" && c.NewValue == "section");
            Assert.Contains(accessRequestLog.Changes, c => c.Field == "ResourceId" && c.NewValue == "section-1");
        }

        [Fact]
        public void ApproveAccessRequest_ShouldGrantSectionAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var createRequest = new CreateAccessRequestRequest
            {
                RequestedBy = "requester-1",
                ResourceType = "section",
                ResourceId = "section-1",
                Reason = "Need access for review"
            };
            var (_, _, accessRequest) = store.CreateAccessRequest(createRequest);

            var reviewRequest = new ReviewAccessRequestRequest
            {
                AccessRequestId = accessRequest!.Id,
                Decision = "approve",
                ReviewedBy = "admin-1",
                ReviewComment = "Approved for quarterly review"
            };

            // Act
            var (isValid, errorMessage, reviewedRequest) = store.ReviewAccessRequest(reviewRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(reviewedRequest);
            Assert.Equal("approved", reviewedRequest!.Status);
            Assert.Equal("admin-1", reviewedRequest.ReviewedBy);
            Assert.Equal("Admin User", reviewedRequest.ReviewedByName);
            Assert.Equal("Approved for quarterly review", reviewedRequest.ReviewComment);
            Assert.NotNull(reviewedRequest.ReviewedAt);

            // Verify section access was granted
            var grants = store.GetUserSectionAccess("requester-1");
            Assert.Single(grants);
            Assert.Equal("section-1", grants[0].SectionId);
            Assert.Equal("requester-1", grants[0].UserId);
            Assert.Equal("admin-1", grants[0].GrantedBy);
        }

        [Fact]
        public void RejectAccessRequest_ShouldNotGrantAccess()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var createRequest = new CreateAccessRequestRequest
            {
                RequestedBy = "requester-1",
                ResourceType = "section",
                ResourceId = "section-1",
                Reason = "Need access"
            };
            var (_, _, accessRequest) = store.CreateAccessRequest(createRequest);

            var reviewRequest = new ReviewAccessRequestRequest
            {
                AccessRequestId = accessRequest!.Id,
                Decision = "reject",
                ReviewedBy = "admin-1",
                ReviewComment = "Insufficient justification"
            };

            // Act
            var (isValid, errorMessage, reviewedRequest) = store.ReviewAccessRequest(reviewRequest);

            // Assert
            Assert.True(isValid);
            Assert.Null(errorMessage);
            Assert.NotNull(reviewedRequest);
            Assert.Equal("rejected", reviewedRequest!.Status);
            Assert.Equal("admin-1", reviewedRequest.ReviewedBy);
            Assert.Equal("Insufficient justification", reviewedRequest.ReviewComment);

            // Verify section access was NOT granted
            var grants = store.GetUserSectionAccess("requester-1");
            Assert.Empty(grants);
        }

        [Fact]
        public void ReviewAccessRequest_AlreadyReviewed_ShouldFail()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var createRequest = new CreateAccessRequestRequest
            {
                RequestedBy = "requester-1",
                ResourceType = "section",
                ResourceId = "section-1",
                Reason = "Need access"
            };
            var (_, _, accessRequest) = store.CreateAccessRequest(createRequest);

            // First review
            var reviewRequest = new ReviewAccessRequestRequest
            {
                AccessRequestId = accessRequest!.Id,
                Decision = "approve",
                ReviewedBy = "admin-1",
                ReviewComment = "Approved"
            };
            store.ReviewAccessRequest(reviewRequest);

            // Second review attempt
            var secondReviewRequest = new ReviewAccessRequestRequest
            {
                AccessRequestId = accessRequest.Id,
                Decision = "reject",
                ReviewedBy = "admin-1",
                ReviewComment = "Changed my mind"
            };

            // Act
            var (isValid, errorMessage, _) = store.ReviewAccessRequest(secondReviewRequest);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(errorMessage);
            Assert.Contains("already approved", errorMessage);
        }

        [Fact]
        public void ReviewAccessRequest_ShouldCreateAuditLogEntries()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            var createRequest = new CreateAccessRequestRequest
            {
                RequestedBy = "requester-1",
                ResourceType = "section",
                ResourceId = "section-1",
                Reason = "Need access"
            };
            var (_, _, accessRequest) = store.CreateAccessRequest(createRequest);

            var reviewRequest = new ReviewAccessRequestRequest
            {
                AccessRequestId = accessRequest!.Id,
                Decision = "approve",
                ReviewedBy = "admin-1",
                ReviewComment = "Approved"
            };

            // Act
            store.ReviewAccessRequest(reviewRequest);

            // Assert
            var auditLogs = store.GetAuditLog(null, null, null, null);
            
            // Should have audit log for the review
            var reviewLog = auditLogs.FirstOrDefault(log => 
                log.Action == "review-access-request" && 
                log.EntityId == accessRequest.Id);
            Assert.NotNull(reviewLog);
            Assert.Equal("admin-1", reviewLog!.UserId);
            
            // Should have audit log for the section access grant
            var grantLog = auditLogs.FirstOrDefault(log => 
                log.Action == "grant-section-access" && 
                log.UserId == "admin-1");
            Assert.NotNull(grantLog);
        }

        [Fact]
        public void GetAccessRequests_ShouldFilterByStatus()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            
            // Create multiple requests
            store.CreateAccessRequest(new CreateAccessRequestRequest
            {
                RequestedBy = "requester-1",
                ResourceType = "section",
                ResourceId = "section-1",
                Reason = "Reason 1"
            });

            var (_, _, request2) = store.CreateAccessRequest(new CreateAccessRequestRequest
            {
                RequestedBy = "requester-1",
                ResourceType = "section",
                ResourceId = "section-1",
                Reason = "Reason 2"
            });

            // Approve one
            store.ReviewAccessRequest(new ReviewAccessRequestRequest
            {
                AccessRequestId = request2!.Id,
                Decision = "approve",
                ReviewedBy = "admin-1"
            });

            // Act
            var pendingRequests = store.GetAccessRequests(status: "pending");
            var approvedRequests = store.GetAccessRequests(status: "approved");

            // Assert
            Assert.Single(pendingRequests);
            Assert.Single(approvedRequests);
        }

        [Fact]
        public void GetAccessRequests_ShouldFilterByRequester()
        {
            // Arrange
            var store = CreateStoreWithTestData();
            
            store.CreateAccessRequest(new CreateAccessRequestRequest
            {
                RequestedBy = "requester-1",
                ResourceType = "section",
                ResourceId = "section-1",
                Reason = "User 1 request"
            });

            // Act
            var userRequests = store.GetAccessRequests(requestedBy: "requester-1");
            var otherRequests = store.GetAccessRequests(requestedBy: "admin-1");

            // Assert
            Assert.Single(userRequests);
            Assert.Empty(otherRequests);
        }
    }
}
