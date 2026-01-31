using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Tests for segregation of duties enforcement in approval workflows.
/// Ensures that content authors cannot approve their own work.
/// </summary>
public class SegregationOfDutiesTests
{
    private static InMemoryReportStore CreateStoreWithTestData()
    {
        var store = new InMemoryReportStore();
        
        // Create users
        var users = store.GetUsers();
        var usersField = typeof(InMemoryReportStore).GetField("_users", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var usersList = usersField!.GetValue(store) as List<User>;
        
        usersList!.Clear();
        usersList.Add(new User { Id = "author-1", Name = "Alice Author", Email = "alice@company.com", Role = "report-owner" });
        usersList.Add(new User { Id = "approver-1", Name = "Bob Approver", Email = "bob@company.com", Role = "admin" });
        usersList.Add(new User { Id = "approver-2", Name = "Charlie Approver", Email = "charlie@company.com", Role = "admin" });
        
        // Create organization
        store.CreateOrganization(new CreateOrganizationRequest
        {
            Name = "Test Organization",
            LegalForm = "LLC",
            Country = "US",
            Identifier = "12345",
            CreatedBy = "author-1",
            CoverageType = "full",
            CoverageJustification = "Test coverage"
        });

        // Create organizational unit (required before creating period)
        store.CreateOrganizationalUnit(new CreateOrganizationalUnitRequest
        {
            Name = "Test Unit",
            Description = "Default organizational unit",
            CreatedBy = "author-1"
        });

        // Create reporting period
        var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "author-1",
            OwnerName = "Alice Author"
        });

        if (!isValid)
        {
            throw new InvalidOperationException($"Failed to create test period: {errorMessage}");
        }

        return store;
    }

    [Fact]
    public void CreateApprovalRequest_WhenAuthorIsInApproverList_ShouldRejectWithSoDError()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        
        var request = new CreateApprovalRequestRequest
        {
            PeriodId = periodId,
            RequestedBy = "author-1",
            ApproverIds = new List<string> { "author-1", "approver-1" }, // Author is in approver list
            RequestMessage = "Please review and approve"
        };

        // Act
        var (isValid, errorMessage, approvalRequest) = store.CreateApprovalRequest(request);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Segregation of duties", errorMessage);
        Assert.Contains("author cannot be an approver", errorMessage);
        Assert.Null(approvalRequest);
    }

    [Fact]
    public void CreateApprovalRequest_WhenAuthorIsInApproverList_ShouldLogSoDViolation()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        
        var initialAuditLogCount = store.GetAuditLog().Count;
        
        var request = new CreateApprovalRequestRequest
        {
            PeriodId = periodId,
            RequestedBy = "author-1",
            ApproverIds = new List<string> { "author-1", "approver-1" },
            RequestMessage = "Please review and approve"
        };

        // Act
        var (isValid, errorMessage, approvalRequest) = store.CreateApprovalRequest(request);

        // Assert - check audit log was created
        var auditLog = store.GetAuditLog();
        Assert.Equal(initialAuditLogCount + 1, auditLog.Count);
        
        var sodAuditEntry = auditLog
            .OrderByDescending(e => e.Timestamp)
            .First();
        
        Assert.Equal("ApprovalRequest", sodAuditEntry.EntityType);
        Assert.Equal("segregation-of-duties-violation", sodAuditEntry.Action);
        Assert.Equal("author-1", sodAuditEntry.UserId);
        Assert.Equal("Alice Author", sodAuditEntry.UserName);
        Assert.Contains("segregation of duties", sodAuditEntry.ChangeNote);
    }

    [Fact]
    public void CreateApprovalRequest_WhenAuthorNotInApproverList_ShouldSucceed()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        
        var request = new CreateApprovalRequestRequest
        {
            PeriodId = periodId,
            RequestedBy = "author-1",
            ApproverIds = new List<string> { "approver-1", "approver-2" }, // Author not in list
            RequestMessage = "Please review and approve"
        };

        // Act
        var (isValid, errorMessage, approvalRequest) = store.CreateApprovalRequest(request);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(approvalRequest);
        Assert.Equal(2, approvalRequest.Approvals.Count);
        Assert.Equal("pending", approvalRequest.Status);
    }

    [Fact]
    public void SubmitApprovalDecision_WhenAuthorTriesToApprove_ShouldRejectWithSoDError()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        
        // Create approval request with different approvers (not the author)
        var createRequest = new CreateApprovalRequestRequest
        {
            PeriodId = periodId,
            RequestedBy = "author-1",
            ApproverIds = new List<string> { "approver-1" },
            RequestMessage = "Please review and approve"
        };
        
        var (_, _, approvalRequest) = store.CreateApprovalRequest(createRequest);
        Assert.NotNull(approvalRequest);
        
        // Manually add author as an approver (simulating a configuration error or testing edge case)
        var approvalRecordsField = typeof(InMemoryReportStore).GetField("_approvalRecords", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var approvalRecordsList = approvalRecordsField!.GetValue(store) as List<ApprovalRecord>;
        
        var authorRecord = new ApprovalRecord
        {
            Id = Guid.NewGuid().ToString(),
            ApprovalRequestId = approvalRequest.Id,
            ApproverId = "author-1",
            ApproverName = "Alice Author",
            Status = "pending"
        };
        approvalRecordsList!.Add(authorRecord);
        approvalRequest.Approvals.Add(authorRecord);

        // Act - Author tries to approve
        var decisionRequest = new SubmitApprovalDecisionRequest
        {
            ApprovalRecordId = authorRecord.Id,
            Decision = "approve",
            Comment = "Looks good to me",
            DecidedBy = "author-1"
        };
        
        var (isValid, errorMessage, approvalRecord) = store.SubmitApprovalDecision(decisionRequest);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Segregation of duties", errorMessage);
        Assert.Contains("cannot approve content that you authored", errorMessage);
        Assert.Null(approvalRecord);
    }

    [Fact]
    public void SubmitApprovalDecision_WhenAuthorTriesToApprove_ShouldLogSoDViolation()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        
        // Create approval request
        var createRequest = new CreateApprovalRequestRequest
        {
            PeriodId = periodId,
            RequestedBy = "author-1",
            ApproverIds = new List<string> { "approver-1" },
            RequestMessage = "Please review and approve"
        };
        
        var (_, _, approvalRequest) = store.CreateApprovalRequest(createRequest);
        
        // Manually add author as an approver
        var approvalRecordsField = typeof(InMemoryReportStore).GetField("_approvalRecords", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var approvalRecordsList = approvalRecordsField!.GetValue(store) as List<ApprovalRecord>;
        
        var authorRecord = new ApprovalRecord
        {
            Id = Guid.NewGuid().ToString(),
            ApprovalRequestId = approvalRequest!.Id,
            ApproverId = "author-1",
            ApproverName = "Alice Author",
            Status = "pending"
        };
        approvalRecordsList!.Add(authorRecord);
        approvalRequest.Approvals.Add(authorRecord);
        
        var initialAuditLogCount = store.GetAuditLog().Count;

        // Act - Author tries to approve
        var decisionRequest = new SubmitApprovalDecisionRequest
        {
            ApprovalRecordId = authorRecord.Id,
            Decision = "approve",
            DecidedBy = "author-1"
        };
        
        var (isValid, errorMessage, approvalRecord) = store.SubmitApprovalDecision(decisionRequest);

        // Assert - check audit log was created
        var auditLog = store.GetAuditLog();
        Assert.Equal(initialAuditLogCount + 1, auditLog.Count);
        
        var sodAuditEntry = auditLog
            .OrderByDescending(e => e.Timestamp)
            .First();
        
        Assert.Equal("ApprovalRecord", sodAuditEntry.EntityType);
        Assert.Equal("segregation-of-duties-violation", sodAuditEntry.Action);
        Assert.Equal("author-1", sodAuditEntry.UserId);
        Assert.Equal("Alice Author", sodAuditEntry.UserName);
        Assert.Contains("Attempted to approve own content", sodAuditEntry.ChangeNote);
    }

    [Fact]
    public void SubmitApprovalDecision_WhenNonAuthorApproves_ShouldSucceed()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        
        // Create approval request
        var createRequest = new CreateApprovalRequestRequest
        {
            PeriodId = periodId,
            RequestedBy = "author-1",
            ApproverIds = new List<string> { "approver-1" },
            RequestMessage = "Please review and approve"
        };
        
        var (_, _, approvalRequest) = store.CreateApprovalRequest(createRequest);
        Assert.NotNull(approvalRequest);
        
        var approvalRecord = approvalRequest.Approvals.First();

        // Act - Different user (not author) approves
        var decisionRequest = new SubmitApprovalDecisionRequest
        {
            ApprovalRecordId = approvalRecord.Id,
            Decision = "approve",
            Comment = "Approved",
            DecidedBy = "approver-1"
        };
        
        var (isValid, errorMessage, updatedRecord) = store.SubmitApprovalDecision(decisionRequest);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(updatedRecord);
        Assert.Equal("approved", updatedRecord.Status);
        Assert.Equal("approve", updatedRecord.Decision);
    }

    [Fact]
    public void SubmitApprovalDecision_WhenNonAuthorRejects_ShouldSucceed()
    {
        // Arrange
        var store = CreateStoreWithTestData();
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        
        // Create approval request
        var createRequest = new CreateApprovalRequestRequest
        {
            PeriodId = periodId,
            RequestedBy = "author-1",
            ApproverIds = new List<string> { "approver-1" },
            RequestMessage = "Please review and approve"
        };
        
        var (_, _, approvalRequest) = store.CreateApprovalRequest(createRequest);
        Assert.NotNull(approvalRequest);
        
        var approvalRecord = approvalRequest.Approvals.First();

        // Act - Different user (not author) rejects
        var decisionRequest = new SubmitApprovalDecisionRequest
        {
            ApprovalRecordId = approvalRecord.Id,
            Decision = "reject",
            Comment = "Needs revision",
            DecidedBy = "approver-1"
        };
        
        var (isValid, errorMessage, updatedRecord) = store.SubmitApprovalDecision(decisionRequest);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.NotNull(updatedRecord);
        Assert.Equal("rejected", updatedRecord.Status);
        Assert.Equal("reject", updatedRecord.Decision);
    }

    [Fact]
    public void CreateApprovalRequest_WhenOnlyAuthorAvailable_ShouldRejectWithSoDError()
    {
        // Arrange - scenario where author tries to select themselves as only approver
        var store = CreateStoreWithTestData();
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        
        var request = new CreateApprovalRequestRequest
        {
            PeriodId = periodId,
            RequestedBy = "author-1",
            ApproverIds = new List<string> { "author-1" }, // Only author as approver
            RequestMessage = "Self-approval attempt"
        };

        // Act
        var (isValid, errorMessage, approvalRequest) = store.CreateApprovalRequest(request);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Segregation of duties", errorMessage);
        Assert.Null(approvalRequest);
    }

    [Fact]
    public void CreateApprovalRequest_WithMultipleApproversIncludingAuthor_ShouldRejectWithSoDError()
    {
        // Arrange - author is one among many approvers
        var store = CreateStoreWithTestData();
        var snapshot = store.GetSnapshot();
        var periodId = snapshot.Periods.First().Id;
        
        var request = new CreateApprovalRequestRequest
        {
            PeriodId = periodId,
            RequestedBy = "author-1",
            ApproverIds = new List<string> { "approver-1", "author-1", "approver-2" }, // Author in the middle
            RequestMessage = "Please review and approve"
        };

        // Act
        var (isValid, errorMessage, approvalRequest) = store.CreateApprovalRequest(request);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Segregation of duties", errorMessage);
        Assert.Null(approvalRequest);
    }
}
