using ARP.ESG_ReportStudio.API.Reporting;
using ARP.ESG_ReportStudio.API.Services;

namespace SD.ProjectName.Tests.Products;

public class IntegrityCheckTests
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

        // Create admin user using reflection
        var usersField = typeof(InMemoryReportStore).GetField("_users", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var users = usersField!.GetValue(store) as List<User>;
        users!.Add(new User
        {
            Id = "admin-1",
            Name = "Admin User",
            Email = "admin@test.com",
            Role = "admin"
        });
        users.Add(new User
        {
            Id = "user-1",
            Name = "Regular User",
            Email = "user@test.com",
            Role = "contributor"
        });
    }

    #region Reporting Period Integrity Tests

    [Fact]
    public void CreateReportingPeriod_ShouldCalculateAndStoreHash()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);

        // Act
        var (isValid, errorMessage, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test Owner"
        });

        // Assert
        Assert.True(isValid);
        Assert.NotNull(snapshot);
        Assert.Single(snapshot.Periods);
        
        var period = snapshot.Periods.First();
        Assert.NotNull(period.IntegrityHash);
        Assert.NotEmpty(period.IntegrityHash);
        Assert.False(period.IntegrityWarning);
    }

    [Fact]
    public void VerifyReportingPeriodIntegrity_WithValidHash_ShouldReturnTrue()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test Owner"
        });

        var periodId = snapshot!.Periods.First().Id;

        // Act
        var isValid = store.VerifyReportingPeriodIntegrity(periodId);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyReportingPeriodIntegrity_WithTamperedData_ShouldDetectAndMarkWarning()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test Owner"
        });

        var period = snapshot!.Periods.First();
        
        // Simulate tampering by changing a field without recalculating hash
        // Use reflection to modify the period directly
        var periodsField = typeof(InMemoryReportStore).GetField("_periods", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var periods = periodsField!.GetValue(store) as List<ReportingPeriod>;
        var storedPeriod = periods!.First(p => p.Id == period.Id);
        storedPeriod.Name = "Tampered Name"; // Change without updating hash

        // Act
        var isValid = store.VerifyReportingPeriodIntegrity(period.Id);

        // Assert
        Assert.False(isValid);
        Assert.True(storedPeriod.IntegrityWarning);
        Assert.NotNull(storedPeriod.IntegrityWarningDetails);
    }

    [Fact]
    public void UpdateReportingPeriod_ShouldRecalculateHash()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test Owner"
        });

        var periodId = snapshot!.Periods.First().Id;
        var originalHash = snapshot.Periods.First().IntegrityHash;

        // Act
        var (isValid, errorMessage, updatedPeriod) = store.ValidateAndUpdatePeriod(periodId, new UpdateReportingPeriodRequest
        {
            Name = "FY 2024 Updated",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company"
        });

        // Assert
        Assert.True(isValid);
        Assert.NotNull(updatedPeriod);
        Assert.NotNull(updatedPeriod.IntegrityHash);
        Assert.NotEqual(originalHash, updatedPeriod.IntegrityHash); // Hash should change
        
        // Verify new hash is valid
        var verifyResult = store.VerifyReportingPeriodIntegrity(periodId);
        Assert.True(verifyResult);
    }

    [Fact]
    public void CanPublishPeriod_WithIntegrityWarning_ShouldReturnFalse()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test Owner"
        });

        var period = snapshot!.Periods.First();
        
        // Tamper with data
        var periodsField = typeof(InMemoryReportStore).GetField("_periods", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var periods = periodsField!.GetValue(store) as List<ReportingPeriod>;
        var storedPeriod = periods!.First(p => p.Id == period.Id);
        storedPeriod.Name = "Tampered";
        
        // Trigger integrity check
        store.VerifyReportingPeriodIntegrity(period.Id);

        // Act
        var canPublish = store.CanPublishPeriod(period.Id);

        // Assert
        Assert.False(canPublish);
    }

    [Fact]
    public void OverrideIntegrityWarning_WithAdminUser_ShouldSucceed()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test Owner"
        });

        var period = snapshot!.Periods.First();
        
        // Create integrity warning
        var periodsField = typeof(InMemoryReportStore).GetField("_periods", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var periods = periodsField!.GetValue(store) as List<ReportingPeriod>;
        var storedPeriod = periods!.First(p => p.Id == period.Id);
        storedPeriod.Name = "Tampered";
        store.VerifyReportingPeriodIntegrity(period.Id);

        // Act
        var (success, errorMessage) = store.OverrideIntegrityWarning(
            period.Id, 
            "admin-1", 
            "False positive - data was intentionally updated");

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);
        Assert.False(storedPeriod.IntegrityWarning);
        Assert.True(store.CanPublishPeriod(period.Id));
    }

    [Fact]
    public void OverrideIntegrityWarning_WithNonAdminUser_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test Owner"
        });

        var period = snapshot!.Periods.First();
        
        // Create integrity warning
        var periodsField = typeof(InMemoryReportStore).GetField("_periods", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var periods = periodsField!.GetValue(store) as List<ReportingPeriod>;
        var storedPeriod = periods!.First(p => p.Id == period.Id);
        storedPeriod.Name = "Tampered";
        store.VerifyReportingPeriodIntegrity(period.Id);

        // Act
        var (success, errorMessage) = store.OverrideIntegrityWarning(
            period.Id, 
            "user-1", 
            "Attempted override");

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("admin", errorMessage.ToLower());
    }

    [Fact]
    public void OverrideIntegrityWarning_WithoutJustification_ShouldFail()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test Owner"
        });

        var period = snapshot!.Periods.First();
        
        // Create integrity warning
        var periodsField = typeof(InMemoryReportStore).GetField("_periods", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var periods = periodsField!.GetValue(store) as List<ReportingPeriod>;
        var storedPeriod = periods!.First(p => p.Id == period.Id);
        storedPeriod.Name = "Tampered";
        store.VerifyReportingPeriodIntegrity(period.Id);

        // Act
        var (success, errorMessage) = store.OverrideIntegrityWarning(
            period.Id, 
            "admin-1", 
            "");

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("justification", errorMessage.ToLower());
    }

    #endregion

    #region Decision Integrity Tests

    [Fact]
    public void CreateDecision_ShouldCalculateAndStoreHash()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);

        // Act
        var (isValid, errorMessage, decision) = store.CreateDecision(
            null,
            "Test Decision",
            "Test context",
            "Test decision text",
            "Test alternatives",
            "Test consequences",
            "user-1");

        // Assert
        Assert.True(isValid);
        Assert.NotNull(decision);
        Assert.NotNull(decision.IntegrityHash);
        Assert.NotEmpty(decision.IntegrityHash);
        Assert.Equal("valid", decision.IntegrityStatus);
    }

    [Fact]
    public void VerifyDecisionIntegrity_WithValidHash_ShouldReturnTrue()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, decision) = store.CreateDecision(
            null,
            "Test Decision",
            "Test context",
            "Test decision text",
            "Test alternatives",
            "Test consequences",
            "user-1");

        // Act
        var isValid = store.VerifyDecisionIntegrity(decision!.Id);

        // Assert
        Assert.True(isValid);
        Assert.Equal("valid", decision.IntegrityStatus);
    }

    [Fact]
    public void VerifyDecisionIntegrity_WithTamperedData_ShouldDetectAndMarkFailed()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, decision) = store.CreateDecision(
            null,
            "Test Decision",
            "Test context",
            "Test decision text",
            "Test alternatives",
            "Test consequences",
            "user-1");

        // Tamper with data
        var decisionsField = typeof(InMemoryReportStore).GetField("_decisions", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var decisions = decisionsField!.GetValue(store) as List<Decision>;
        var storedDecision = decisions!.First(d => d.Id == decision!.Id);
        storedDecision.Title = "Tampered Title";

        // Act
        var isValid = store.VerifyDecisionIntegrity(decision!.Id);

        // Assert
        Assert.False(isValid);
        Assert.Equal("failed", storedDecision.IntegrityStatus);
    }

    [Fact]
    public void UpdateDecision_ShouldRecalculateHash()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, decision) = store.CreateDecision(
            null,
            "Test Decision",
            "Test context",
            "Test decision text",
            "Test alternatives",
            "Test consequences",
            "user-1");

        var originalHash = decision!.IntegrityHash;

        // Act
        var (isValid, errorMessage, updatedDecision) = store.UpdateDecision(
            decision.Id,
            "Updated Decision",
            "Updated context",
            "Updated decision text",
            "Updated alternatives",
            "Updated consequences",
            "Updated for testing",
            "user-1");

        // Assert
        Assert.True(isValid);
        Assert.NotNull(updatedDecision);
        Assert.NotNull(updatedDecision.IntegrityHash);
        Assert.NotEqual(originalHash, updatedDecision.IntegrityHash); // Hash should change
        Assert.Equal("valid", updatedDecision.IntegrityStatus);
        Assert.Equal(2, updatedDecision.Version);
        
        // Verify new hash is valid
        var verifyResult = store.VerifyDecisionIntegrity(decision.Id);
        Assert.True(verifyResult);
    }

    [Fact]
    public void UpdateDecision_ShouldStoreHashForHistoricalVersion()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, decision) = store.CreateDecision(
            null,
            "Test Decision",
            "Test context",
            "Test decision text",
            "Test alternatives",
            "Test consequences",
            "user-1");

        var originalHash = decision!.IntegrityHash;

        // Act
        store.UpdateDecision(
            decision.Id,
            "Updated Decision",
            "Updated context",
            "Updated decision text",
            "Updated alternatives",
            "Updated consequences",
            "Updated for testing",
            "user-1");

        // Assert - check that version 1 is stored with its hash
        var versions = store.GetDecisionVersionHistory(decision.Id);
        Assert.Single(versions);
        var v1 = versions.First();
        Assert.Equal(1, v1.Version);
        Assert.Equal(originalHash, v1.IntegrityHash);
    }

    [Fact]
    public void IntegrityService_CalculateDecisionHash_ShouldBeConsistent()
    {
        // Arrange
        var decision = new Decision
        {
            Id = "test-id",
            Version = 1,
            Title = "Test",
            Context = "Context",
            DecisionText = "Text",
            Alternatives = "Alts",
            Consequences = "Cons"
        };

        // Act
        var hash1 = IntegrityService.CalculateDecisionHash(decision);
        var hash2 = IntegrityService.CalculateDecisionHash(decision);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.NotEmpty(hash1);
    }

    [Fact]
    public void IntegrityService_CalculateDecisionHash_ShouldDifferForDifferentContent()
    {
        // Arrange
        var decision1 = new Decision
        {
            Id = "test-id",
            Version = 1,
            Title = "Test",
            Context = "Context",
            DecisionText = "Text",
            Alternatives = "Alts",
            Consequences = "Cons"
        };

        var decision2 = new Decision
        {
            Id = "test-id",
            Version = 1,
            Title = "Different", // Changed title
            Context = "Context",
            DecisionText = "Text",
            Alternatives = "Alts",
            Consequences = "Cons"
        };

        // Act
        var hash1 = IntegrityService.CalculateDecisionHash(decision1);
        var hash2 = IntegrityService.CalculateDecisionHash(decision2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GetIntegrityStatus_ShouldReturnComprehensiveReport()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test Owner"
        });

        var periodId = snapshot!.Periods.First().Id;
        var sectionId = snapshot.Sections.First().Id;

        // Create decision linked to section
        var (_, _, decision) = store.CreateDecision(
            sectionId,
            "Section Decision",
            "Context",
            "Decision",
            "Alternatives",
            "Consequences",
            "user-1");

        // Act
        var status = store.GetIntegrityStatus(periodId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(periodId, status.PeriodId);
        Assert.True(status.PeriodIntegrityValid);
        Assert.False(status.PeriodIntegrityWarning);
        Assert.Empty(status.FailedDecisions);
        Assert.True(status.CanPublish);
    }

    [Fact]
    public void GetIntegrityStatus_WithTamperedDecision_ShouldIncludeInFailedList()
    {
        // Arrange
        var store = new InMemoryReportStore();
        CreateTestConfiguration(store);
        
        var (_, _, snapshot) = store.ValidateAndCreatePeriod(new CreateReportingPeriodRequest
        {
            Name = "FY 2024",
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            ReportingMode = "simplified",
            ReportScope = "single-company",
            OwnerId = "user-1",
            OwnerName = "Test Owner"
        });

        var periodId = snapshot!.Periods.First().Id;
        var sectionId = snapshot.Sections.First().Id;

        // Create decision
        var (_, _, decision) = store.CreateDecision(
            sectionId,
            "Section Decision",
            "Context",
            "Decision",
            "Alternatives",
            "Consequences",
            "user-1");

        // Tamper with decision
        var decisionsField = typeof(InMemoryReportStore).GetField("_decisions", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var decisions = decisionsField!.GetValue(store) as List<Decision>;
        var storedDecision = decisions!.First(d => d.Id == decision!.Id);
        storedDecision.Title = "Tampered";

        // Act
        var status = store.GetIntegrityStatus(periodId);

        // Assert
        Assert.NotNull(status);
        Assert.Single(status.FailedDecisions);
        Assert.Contains(decision!.Id, status.FailedDecisions);
    }

    #endregion
}
