using Xunit;
using ARP.ESG_ReportStudio.API.Reporting;

namespace SD.ProjectName.Tests.Products;

/// <summary>
/// Unit tests for periodic access review functionality.
/// Tests review initiation, decision recording, access revocation, and export.
/// </summary>
public sealed class AccessReviewTests
{
    [Fact]
    public void StartAccessReview_ShouldCreateReviewWithAllUsers()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var request = new StartAccessReviewRequest
        {
            Title = "Q1 2024 Access Review",
            Description = "Quarterly access review",
            StartedBy = "admin-user",
            StartedByName = "Admin User"
        };

        // Act
        var review = store.StartAccessReview(request);

        // Assert
        Assert.NotNull(review);
        Assert.Equal("Q1 2024 Access Review", review.Title);
        Assert.Equal("in-progress", review.Status);
        Assert.NotNull(review.Entries);
        Assert.True(review.Entries.Count > 0); // Should have at least the default users
        Assert.NotNull(review.Summary);
    }

    [Fact]
    public void StartAccessReview_ShouldIncludeUserRolesAndScopes()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        var request = new StartAccessReviewRequest
        {
            Title = "Test Review",
            StartedBy = "admin-user",
            StartedByName = "Admin User"
        };

        // Act
        var review = store.StartAccessReview(request);

        // Assert
        var userEntry = review.Entries.FirstOrDefault(e => e.UserId == "user-1");
        Assert.NotNull(userEntry);
        Assert.NotEmpty(userEntry.RoleIds);
        Assert.NotEmpty(userEntry.RoleNames);
    }

    [Fact]
    public void RecordReviewDecision_WithRetain_ShouldUpdateEntry()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var review = store.StartAccessReview(new StartAccessReviewRequest
        {
            Title = "Test Review",
            StartedBy = "admin-user",
            StartedByName = "Admin User"
        });
        
        var entry = review.Entries.First();
        var request = new RecordReviewDecisionRequest
        {
            EntryId = entry.Id,
            Decision = "retain",
            DecisionNote = "Access still required",
            DecisionBy = "compliance-officer",
            DecisionByName = "Compliance Officer"
        };

        // Act
        var (success, errorMessage) = store.RecordReviewDecision(review.Id, request);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);
        
        var updatedReview = store.GetAccessReview(review.Id);
        var updatedEntry = updatedReview!.Entries.First(e => e.Id == entry.Id);
        Assert.Equal("retain", updatedEntry.Decision);
        Assert.NotNull(updatedEntry.DecisionAt);
        Assert.Equal("compliance-officer", updatedEntry.DecisionBy);
        Assert.Equal("Access still required", updatedEntry.DecisionNote);
    }

    [Fact]
    public void RecordReviewDecision_WithRevoke_ShouldRemoveAccessAndDeactivateUser()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        // Get a user and verify they have roles
        var users = store.GetUsers();
        var testUser = users.First(u => u.RoleIds.Count > 0);
        var originalRoleCount = testUser.RoleIds.Count;
        
        var review = store.StartAccessReview(new StartAccessReviewRequest
        {
            Title = "Test Review",
            StartedBy = "admin-user",
            StartedByName = "Admin User"
        });
        
        var entry = review.Entries.First(e => e.UserId == testUser.Id);
        var request = new RecordReviewDecisionRequest
        {
            EntryId = entry.Id,
            Decision = "revoke",
            DecisionNote = "User no longer requires access",
            DecisionBy = "compliance-officer",
            DecisionByName = "Compliance Officer"
        };

        // Act
        var (success, errorMessage) = store.RecordReviewDecision(review.Id, request);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);
        
        // Verify decision was recorded
        var updatedReview = store.GetAccessReview(review.Id);
        var updatedEntry = updatedReview!.Entries.First(e => e.Id == entry.Id);
        Assert.Equal("revoke", updatedEntry.Decision);
        
        // Verify user access was revoked
        var updatedUser = store.GetUser(testUser.Id);
        Assert.NotNull(updatedUser);
        Assert.Empty(updatedUser.RoleIds); // All roles removed
        Assert.False(updatedUser.IsActive); // User deactivated
        
        // Verify summary updated
        Assert.True(updatedReview.Summary!.AccessesRevoked > 0);
    }

    [Fact]
    public void RecordReviewDecision_WithInvalidDecision_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var review = store.StartAccessReview(new StartAccessReviewRequest
        {
            Title = "Test Review",
            StartedBy = "admin-user",
            StartedByName = "Admin User"
        });
        
        var entry = review.Entries.First();
        var request = new RecordReviewDecisionRequest
        {
            EntryId = entry.Id,
            Decision = "invalid",
            DecisionBy = "compliance-officer",
            DecisionByName = "Compliance Officer"
        };

        // Act
        var (success, errorMessage) = store.RecordReviewDecision(review.Id, request);

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("'retain' or 'revoke'", errorMessage);
    }

    [Fact]
    public void CompleteAccessReview_ShouldMarkAsCompleted()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var review = store.StartAccessReview(new StartAccessReviewRequest
        {
            Title = "Test Review",
            StartedBy = "admin-user",
            StartedByName = "Admin User"
        });
        
        var request = new CompleteAccessReviewRequest
        {
            CompletedBy = "compliance-officer",
            CompletedByName = "Compliance Officer"
        };

        // Act
        var (success, errorMessage) = store.CompleteAccessReview(review.Id, request);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);
        
        var completedReview = store.GetAccessReview(review.Id);
        Assert.NotNull(completedReview);
        Assert.Equal("completed", completedReview.Status);
        Assert.NotNull(completedReview.CompletedAt);
        Assert.Equal("compliance-officer", completedReview.CompletedBy);
    }

    [Fact]
    public void CompleteAccessReview_WhenAlreadyCompleted_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var review = store.StartAccessReview(new StartAccessReviewRequest
        {
            Title = "Test Review",
            StartedBy = "admin-user",
            StartedByName = "Admin User"
        });
        
        // Complete the review first time
        store.CompleteAccessReview(review.Id, new CompleteAccessReviewRequest
        {
            CompletedBy = "compliance-officer",
            CompletedByName = "Compliance Officer"
        });
        
        // Try to complete again
        var request = new CompleteAccessReviewRequest
        {
            CompletedBy = "admin-user",
            CompletedByName = "Admin User"
        };

        // Act
        var (success, errorMessage) = store.CompleteAccessReview(review.Id, request);

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("already completed", errorMessage);
    }

    [Fact]
    public void RecordReviewDecision_WhenReviewCompleted_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var review = store.StartAccessReview(new StartAccessReviewRequest
        {
            Title = "Test Review",
            StartedBy = "admin-user",
            StartedByName = "Admin User"
        });
        
        // Complete the review
        store.CompleteAccessReview(review.Id, new CompleteAccessReviewRequest
        {
            CompletedBy = "compliance-officer",
            CompletedByName = "Compliance Officer"
        });
        
        var entry = review.Entries.First();
        var request = new RecordReviewDecisionRequest
        {
            EntryId = entry.Id,
            Decision = "retain",
            DecisionBy = "admin-user",
            DecisionByName = "Admin User"
        };

        // Act
        var (success, errorMessage) = store.RecordReviewDecision(review.Id, request);

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("completed review", errorMessage);
    }

    [Fact]
    public void GetAccessReviewLog_ShouldReturnAllActions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var review = store.StartAccessReview(new StartAccessReviewRequest
        {
            Title = "Test Review",
            StartedBy = "admin-user",
            StartedByName = "Admin User"
        });
        
        var entry = review.Entries.First();
        store.RecordReviewDecision(review.Id, new RecordReviewDecisionRequest
        {
            EntryId = entry.Id,
            Decision = "retain",
            DecisionBy = "compliance-officer",
            DecisionByName = "Compliance Officer"
        });
        
        store.CompleteAccessReview(review.Id, new CompleteAccessReviewRequest
        {
            CompletedBy = "compliance-officer",
            CompletedByName = "Compliance Officer"
        });

        // Act
        var log = store.GetAccessReviewLog(review.Id);

        // Assert
        Assert.NotEmpty(log);
        Assert.Contains(log, l => l.Action == "review-started");
        Assert.Contains(log, l => l.Action == "decision-recorded");
        Assert.Contains(log, l => l.Action == "review-completed");
    }

    [Fact]
    public void GetAccessReviewLog_WithRevocation_ShouldIncludeRevokeAction()
    {
        // Arrange
        var store = new InMemoryReportStore();
        
        var users = store.GetUsers();
        var testUser = users.First(u => u.RoleIds.Count > 0);
        
        var review = store.StartAccessReview(new StartAccessReviewRequest
        {
            Title = "Test Review",
            StartedBy = "admin-user",
            StartedByName = "Admin User"
        });
        
        var entry = review.Entries.First(e => e.UserId == testUser.Id);
        store.RecordReviewDecision(review.Id, new RecordReviewDecisionRequest
        {
            EntryId = entry.Id,
            Decision = "revoke",
            DecisionNote = "No longer needed",
            DecisionBy = "compliance-officer",
            DecisionByName = "Compliance Officer"
        });

        // Act
        var log = store.GetAccessReviewLog(review.Id);

        // Assert
        Assert.Contains(log, l => l.Action == "access-revoked");
        var revokeEntry = log.First(l => l.Action == "access-revoked");
        Assert.Equal(testUser.Id, revokeEntry.RelatedUserId);
        Assert.Contains("Revoked", revokeEntry.Details);
    }

    [Fact]
    public void ReviewSummary_ShouldReflectDecisions()
    {
        // Arrange
        var store = new InMemoryReportStore();
        var review = store.StartAccessReview(new StartAccessReviewRequest
        {
            Title = "Test Review",
            StartedBy = "admin-user",
            StartedByName = "Admin User"
        });
        
        var entries = review.Entries.Take(3).ToList();
        
        // Record different decisions
        store.RecordReviewDecision(review.Id, new RecordReviewDecisionRequest
        {
            EntryId = entries[0].Id,
            Decision = "retain",
            DecisionBy = "compliance-officer",
            DecisionByName = "Compliance Officer"
        });
        
        if (entries.Count > 1)
        {
            store.RecordReviewDecision(review.Id, new RecordReviewDecisionRequest
            {
                EntryId = entries[1].Id,
                Decision = "revoke",
                DecisionBy = "compliance-officer",
                DecisionByName = "Compliance Officer"
            });
        }

        // Act
        var updatedReview = store.GetAccessReview(review.Id);

        // Assert
        Assert.NotNull(updatedReview);
        Assert.NotNull(updatedReview.Summary);
        Assert.Equal(1, updatedReview.Summary.RetainDecisions);
        if (entries.Count > 1)
        {
            Assert.Equal(1, updatedReview.Summary.RevokeDecisions);
        }
        Assert.True(updatedReview.Summary.PendingDecisions < updatedReview.Summary.TotalUsers);
    }
}
