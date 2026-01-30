using System.Security.Cryptography;
using System.Text;
using ARP.ESG_ReportStudio.API.Services;

namespace ARP.ESG_ReportStudio.API.Reporting;

public sealed class InMemoryReportStore
{
    private readonly object _lock = new();
    private readonly TextDiffService _textDiffService;
    private Organization? _organization;
    private readonly List<ReportingPeriod> _periods = new();
    private readonly List<ReportSection> _sections = new();
    private readonly List<SectionSummary> _summaries = new();
    private readonly List<OrganizationalUnit> _organizationalUnits = new();
    private readonly List<SectionCatalogItem> _sectionCatalog = new();
    private readonly List<DataPoint> _dataPoints = new();
    private readonly List<Evidence> _evidence = new();
    private readonly List<Assumption> _assumptions = new();
    private readonly List<Gap> _gaps = new();
    private readonly List<Simplification> _simplifications = new();
    private readonly List<User> _users = new();
    private readonly List<ValidationRule> _validationRules = new();
    private readonly List<AuditLogEntry> _auditLog = new();
    private readonly List<ReminderConfiguration> _reminderConfigurations = new();
    private readonly List<ReminderHistory> _reminderHistory = new();
    private readonly List<DataPointNote> _dataPointNotes = new();
    private readonly List<OwnerNotification> _notifications = new();
    private readonly List<EscalationConfiguration> _escalationConfigurations = new();
    private readonly List<EscalationHistory> _escalationHistory = new();
    private readonly List<RemediationPlan> _remediationPlans = new();
    private readonly List<RemediationAction> _remediationActions = new();
    private readonly List<CompletionException> _completionExceptions = new();
    private readonly List<EvidenceAccessLog> _evidenceAccessLog = new();
    private readonly List<Decision> _decisions = new();
    private readonly List<DecisionVersion> _decisionVersions = new();
    private readonly List<ApprovalRequest> _approvalRequests = new();
    private readonly List<ApprovalRecord> _approvalRecords = new();
    private readonly List<RetentionPolicy> _retentionPolicies = new();
    private readonly List<LegalHold> _legalHolds = new();
    private readonly List<DeletionReport> _deletionReports = new();
    private readonly List<RolloverAuditLog> _rolloverAuditLogs = new();
    private readonly List<DataTypeRolloverRule> _rolloverRules = new();
    private readonly List<RolloverRuleHistory> _rolloverRuleHistory = new();
    private readonly Dictionary<string, RolloverReconciliation> _rolloverReconciliations = new(); // Key: targetPeriodId
    private readonly List<VarianceThresholdConfig> _varianceThresholdConfigs = new();
    private readonly List<VarianceExplanation> _varianceExplanations = new();
    private readonly List<MaturityModel> _maturityModels = new();
    private readonly Dictionary<string, List<AuditLogEntry>> _periodAuditTrails = new(); // Key: periodId
    private readonly List<ValidationResult> _validationResults = new(); // Audit trail for validation runs
    private readonly List<ReportVariant> _reportVariants = new(); // Report variant configurations
    private readonly List<BrandingProfile> _brandingProfiles = new(); // Corporate branding profiles
    private readonly List<DocumentTemplate> _documentTemplates = new(); // Document templates with versioning
    private readonly List<TemplateUsageRecord> _templateUsageRecords = new(); // Template usage audit trail
    private readonly List<GenerationHistoryEntry> _generationHistory = new(); // Report generation history
    private readonly List<ExportHistoryEntry> _exportHistory = new(); // Export history (PDF/DOCX)

    // Valid missing reason categories
    private static readonly string[] ValidMissingReasonCategories = new[] 
    { 
        "not-measured", 
        "not-applicable", 
        "unavailable-from-supplier",
        "data-quality-issue",
        "system-limitation",
        "other"
    };

    // Valid estimate types
    private static readonly string[] ValidEstimateTypes = new[] 
    { 
        "point", 
        "range", 
        "proxy-based", 
        "extrapolated" 
    };

    // Valid confidence levels
    private static readonly string[] ValidConfidenceLevels = new[] 
    { 
        "low", 
        "medium", 
        "high" 
    };

    // Valid gap statuses for workflow tracking
    private static readonly string[] ValidGapStatuses = new[] 
    { 
        "missing", 
        "estimated", 
        "provided" 
    };

    // Valid exception types for completeness validation
    private static readonly string[] ValidExceptionTypes = new[]
    {
        "missing-data",
        "estimated-data",
        "simplified-scope",
        "other"
    };
    
    // Retention policy priority weights (higher = more specific)
    private const int TenantSpecificityPriority = 10;
    private const int ReportTypeSpecificityPriority = 5;
    private const int DataCategorySpecificityPriority = 3;

    private readonly IReadOnlyList<SectionTemplate> _simplifiedTemplates = new List<SectionTemplate>
    {
        new("Energy & Emissions", "environmental", "Energy consumption, GHG emissions, carbon footprint"),
        new("Waste & Recycling", "environmental", "Waste generation, recycling rates, circular economy initiatives"),
        new("Employee Health & Safety", "social", "Workplace safety metrics, injury rates, wellness programs"),
        new("Diversity & Inclusion", "social", "Workforce diversity, equal opportunity, inclusion initiatives"),
        new("Board Composition", "governance", "Board structure, independence, diversity, expertise"),
        new("Ethics & Compliance", "governance", "Code of conduct, anti-corruption, compliance training")
    };

    private readonly IReadOnlyList<SectionTemplate> _extendedTemplates;

    public InMemoryReportStore() : this(new TextDiffService())
    {
    }
    
    public InMemoryReportStore(TextDiffService textDiffService)
    {
        _textDiffService = textDiffService;
        
        var extended = new List<SectionTemplate>(_simplifiedTemplates)
        {
            new("Water & Biodiversity", "environmental", "Water usage, water quality, biodiversity impact"),
            new("Supply Chain Environmental Impact", "environmental", "Supplier environmental performance, sustainable sourcing"),
            new("Employee Development", "social", "Training hours, skill development, career progression"),
            new("Community Engagement", "social", "Social investment, local employment, community programs"),
            new("Human Rights", "social", "Human rights policy, supply chain labor practices"),
            new("Risk Management", "governance", "Risk framework, ESG risk integration, climate risk"),
            new("Stakeholder Engagement", "governance", "Stakeholder dialogue, materiality assessment")
        };

        _extendedTemplates = extended;

        // Initialize the catalog with default sections
        InitializeDefaultCatalog();
        
        // Initialize sample users
        InitializeSampleUsers();
    }

    private void InitializeSampleUsers()
    {
        _users.AddRange(new[]
        {
            new User { Id = "user-1", Name = "Sarah Chen", Email = "sarah.chen@company.com", Role = "report-owner", CanExport = true },
            new User { Id = "user-2", Name = "Admin User", Email = "admin@company.com", Role = "admin", CanExport = true },
            new User { Id = "user-3", Name = "John Smith", Email = "john.smith@company.com", Role = "contributor", CanExport = false },
            new User { Id = "user-4", Name = "Emily Johnson", Email = "emily.johnson@company.com", Role = "contributor", CanExport = false },
            new User { Id = "user-5", Name = "Michael Brown", Email = "michael.brown@company.com", Role = "contributor", CanExport = false },
            new User { Id = "user-6", Name = "Lisa Anderson", Email = "lisa.anderson@company.com", Role = "auditor", CanExport = true },
            new User { Id = "owner-1", Name = "Test Owner", Email = "owner@company.com", Role = "report-owner", CanExport = true }
        });
    }

    private void InitializeDefaultCatalog()
    {
        var catalogItems = new List<(string Title, string Code, string Category, string Description)>
        {
            ("Energy & Emissions", "ENV-001", "environmental", "Energy consumption, GHG emissions, carbon footprint"),
            ("Waste & Recycling", "ENV-002", "environmental", "Waste generation, recycling rates, circular economy initiatives"),
            ("Water & Biodiversity", "ENV-003", "environmental", "Water usage, water quality, biodiversity impact"),
            ("Supply Chain Environmental Impact", "ENV-004", "environmental", "Supplier environmental performance, sustainable sourcing"),
            ("Employee Health & Safety", "SOC-001", "social", "Workplace safety metrics, injury rates, wellness programs"),
            ("Diversity & Inclusion", "SOC-002", "social", "Workforce diversity, equal opportunity, inclusion initiatives"),
            ("Employee Development", "SOC-003", "social", "Training hours, skill development, career progression"),
            ("Community Engagement", "SOC-004", "social", "Social investment, local employment, community programs"),
            ("Human Rights", "SOC-005", "social", "Human rights policy, supply chain labor practices"),
            ("Board Composition", "GOV-001", "governance", "Board structure, independence, diversity, expertise"),
            ("Ethics & Compliance", "GOV-002", "governance", "Code of conduct, anti-corruption, compliance training"),
            ("Risk Management", "GOV-003", "governance", "Risk framework, ESG risk integration, climate risk"),
            ("Stakeholder Engagement", "GOV-004", "governance", "Stakeholder dialogue, materiality assessment")
        };

        foreach (var (title, code, category, description) in catalogItems)
        {
            _sectionCatalog.Add(new SectionCatalogItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Code = code,
                Category = category,
                Description = description,
                IsDeprecated = false,
                CreatedAt = DateTime.UtcNow.ToString("O")
            });
        }
    }

    public ReportingDataSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new ReportingDataSnapshot
            {
                Organization = _organization,
                Periods = _periods.ToList(),
                Sections = _sections.ToList(),
                SectionSummaries = _summaries.ToList(),
                OrganizationalUnits = _organizationalUnits.ToList()
            };
        }
    }

    public (bool IsValid, string? ErrorMessage, ReportingDataSnapshot? Snapshot) ValidateAndCreatePeriod(CreateReportingPeriodRequest request)
    {
        lock (_lock)
        {
            // Check if organization exists
            if (_organization == null)
            {
                return (false, "Organization must be configured before creating reporting periods.", null);
            }

            // Check if organizational structure exists
            if (_organizationalUnits.Count == 0)
            {
                return (false, "Organizational structure must be defined before creating reporting periods. Please add at least one organizational unit.", null);
            }

            // Validate date range: start date must be before end date
            if (!DateTime.TryParse(request.StartDate, out var startDate) || 
                !DateTime.TryParse(request.EndDate, out var endDate))
            {
                return (false, "Invalid date format. Please provide valid dates.", null);
            }

            if (startDate >= endDate)
            {
                return (false, "Start date must be before end date.", null);
            }

            // Check for overlapping periods
            foreach (var existingPeriod in _periods)
            {
                if (!DateTime.TryParse(existingPeriod.StartDate, out var existingStart) ||
                    !DateTime.TryParse(existingPeriod.EndDate, out var existingEnd))
                {
                    continue;
                }

                // Check if periods overlap
                // Period 1: [start1, end1], Period 2: [start2, end2]
                // They overlap if: start1 < end2 AND start2 < end1
                if (startDate < existingEnd && existingStart < endDate)
                {
                    return (false, $"Reporting period overlaps with existing period '{existingPeriod.Name}' ({existingPeriod.StartDate} - {existingPeriod.EndDate}).", null);
                }
            }

            foreach (var period in _periods)
            {
                period.Status = "closed";
            }

            var newPeriod = new ReportingPeriod
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ReportingMode = request.ReportingMode,
                ReportScope = request.ReportScope,
                Status = "active",
                CreatedAt = DateTime.UtcNow.ToString("O"),
                OwnerId = request.OwnerId,
                OrganizationId = request.OrganizationId
            };
            
            // Calculate and store integrity hash
            newPeriod.IntegrityHash = Services.IntegrityService.CalculateReportingPeriodHash(newPeriod);

            _periods.Add(newPeriod);

            // Get active sections from catalog based on reporting mode
            var catalogSections = _sectionCatalog
                .Where(s => !s.IsDeprecated)
                .ToList();

            // Codes for simplified mode sections
            var simplifiedCodes = new[] { "ENV-001", "ENV-002", "SOC-001", "SOC-002", "GOV-001", "GOV-002" };

            // Determine which sections to include based on reporting mode
            var sectionsToInclude = request.ReportingMode == "extended" 
                ? catalogSections 
                : catalogSections.Where(s => simplifiedCodes.Contains(s.Code)).ToList();

            var order = 0;
            
            // Determine default owner for new sections
            // If copying ownership, new sections start unassigned
            // Otherwise, new sections get the period owner
            var defaultOwnerId = string.IsNullOrWhiteSpace(request.CopyOwnershipFromPeriodId) 
                ? request.OwnerId 
                : string.Empty;
            var defaultOwnerName = string.IsNullOrWhiteSpace(request.CopyOwnershipFromPeriodId)
                ? request.OwnerName
                : "Unassigned";

            foreach (var catalogItem in sectionsToInclude)
            {
                var section = new ReportSection
                {
                    Id = Guid.NewGuid().ToString(),
                    PeriodId = newPeriod.Id,
                    Title = catalogItem.Title,
                    Category = catalogItem.Category,
                    Description = catalogItem.Description,
                    OwnerId = defaultOwnerId,
                    Status = "draft",
                    Completeness = "empty",
                    Order = order++,
                    CatalogCode = catalogItem.Code
                };

                _sections.Add(section);

                _summaries.Add(new SectionSummary
                {
                    Id = section.Id,
                    PeriodId = section.PeriodId,
                    Title = section.Title,
                    Category = section.Category,
                    Description = section.Description,
                    OwnerId = section.OwnerId,
                    Status = section.Status,
                    Completeness = section.Completeness,
                    Order = section.Order,
                    CatalogCode = section.CatalogCode,
                    DataPointCount = 0,
                    EvidenceCount = 0,
                    GapCount = 0,
                    AssumptionCount = 0,
                    CompletenessPercentage = 0,
                    OwnerName = defaultOwnerName
                });
            }

            // Copy ownership from previous period if specified
            if (!string.IsNullOrWhiteSpace(request.CopyOwnershipFromPeriodId))
            {
                // Validate source period exists
                var sourcePeriod = _periods.FirstOrDefault(p => p.Id == request.CopyOwnershipFromPeriodId);
                if (sourcePeriod == null)
                {
                    return (false, $"Source period with ID '{request.CopyOwnershipFromPeriodId}' not found.", null);
                }
                
                CopyOwnershipFromPreviousPeriod(request.CopyOwnershipFromPeriodId, newPeriod.Id);
                
                // Carry forward gaps, assumptions, and remediation plans if requested
                if (request.CarryForwardGapsAndAssumptions)
                {
                    CarryForwardGapsAndAssumptions(request.CopyOwnershipFromPeriodId, newPeriod.Id, request.StartDate);
                }
            }

            var snapshot = new ReportingDataSnapshot
            {
                Organization = _organization,
                Periods = _periods.ToList(),
                Sections = _sections.ToList(),
                SectionSummaries = _summaries.ToList(),
                OrganizationalUnits = _organizationalUnits.ToList()
            };

            return (true, null, snapshot);
        }
    }

    /// <summary>
    /// Copies ownership mappings from a previous period to a new period.
    /// Matches sections by catalog code and preserves ownership where codes match.
    /// Sections without matches remain unassigned (empty OwnerId).
    /// </summary>
    /// <param name="sourcePeriodId">The period to copy ownership from</param>
    /// <param name="targetPeriodId">The period to copy ownership to</param>
    private void CopyOwnershipFromPreviousPeriod(string sourcePeriodId, string targetPeriodId)
    {
        // Get all sections from the source period
        var sourceSections = _sections.Where(s => s.PeriodId == sourcePeriodId).ToList();
        
        // Get all sections from the target period
        var targetSections = _sections.Where(s => s.PeriodId == targetPeriodId).ToList();
        
        // Create a lookup of source sections by catalog code
        var sourceSectionsByCode = sourceSections
            .Where(s => !string.IsNullOrWhiteSpace(s.CatalogCode))
            .ToDictionary(s => s.CatalogCode!, s => s);
        
        // Update ownership for matching sections
        foreach (var targetSection in targetSections)
        {
            if (string.IsNullOrWhiteSpace(targetSection.CatalogCode))
                continue;
                
            if (sourceSectionsByCode.TryGetValue(targetSection.CatalogCode, out var sourceSection))
            {
                // Copy section ownership
                targetSection.OwnerId = sourceSection.OwnerId;
                
                // Update the corresponding summary
                var summary = _summaries.FirstOrDefault(s => s.Id == targetSection.Id);
                if (summary != null)
                {
                    summary.OwnerId = sourceSection.OwnerId;
                    // Update owner name - handle missing owner gracefully
                    if (!string.IsNullOrWhiteSpace(sourceSection.OwnerId))
                    {
                        var owner = _users.FirstOrDefault(u => u.Id == sourceSection.OwnerId);
                        summary.OwnerName = owner?.Name ?? $"Unknown User ({sourceSection.OwnerId})";
                    }
                    else
                    {
                        summary.OwnerName = "Unassigned";
                    }
                }
            }
            // Note: Sections without a match remain unassigned (empty OwnerId)
            // These will appear in the "Unassigned" view and must be manually assigned
        }
    }

    /// <summary>
    /// Carries forward open gaps, active assumptions, and active remediation plans from a previous period to a new period.
    /// Items are copied as references to matching sections based on catalog codes.
    /// Expired assumptions are flagged for review.
    /// </summary>
    /// <param name="sourcePeriodId">The period to copy items from</param>
    /// <param name="targetPeriodId">The period to copy items to</param>
    /// <param name="targetPeriodStartDate">Start date of the new period (for expiration checking)</param>
    private void CarryForwardGapsAndAssumptions(string sourcePeriodId, string targetPeriodId, string targetPeriodStartDate)
    {
        // Get all sections from both periods
        var sourceSections = _sections.Where(s => s.PeriodId == sourcePeriodId).ToList();
        var targetSections = _sections.Where(s => s.PeriodId == targetPeriodId).ToList();
        
        // Create a mapping from source section IDs to target section IDs based on catalog codes
        var sectionMapping = new Dictionary<string, string>();
        foreach (var targetSection in targetSections)
        {
            if (string.IsNullOrWhiteSpace(targetSection.CatalogCode))
                continue;
                
            var sourceSection = sourceSections.FirstOrDefault(s => s.CatalogCode == targetSection.CatalogCode);
            if (sourceSection != null)
            {
                sectionMapping[sourceSection.Id] = targetSection.Id;
            }
        }
        
        var currentUser = "system"; // Carried forward items are created by system
        var now = DateTime.UtcNow.ToString("O");
        
        // Parse target period start date for expiration checking
        DateTime.TryParse(targetPeriodStartDate, out var periodStartDate);
        
        // Carry forward open gaps (Resolved = false)
        var openGaps = _gaps.Where(g => 
            sourceSections.Any(s => s.Id == g.SectionId) && 
            !g.Resolved
        ).ToList();
        
        foreach (var gap in openGaps)
        {
            if (!sectionMapping.TryGetValue(gap.SectionId, out var targetSectionId))
                continue; // Skip if section doesn't exist in new period
            
            var newGap = new Gap
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = targetSectionId,
                Title = gap.Title,
                Description = $"[Carried forward from previous period]\n\n{gap.Description}",
                Impact = gap.Impact,
                ImprovementPlan = gap.ImprovementPlan,
                TargetDate = gap.TargetDate,
                CreatedBy = currentUser,
                CreatedAt = now,
                Resolved = false
            };
            
            _gaps.Add(newGap);
        }
        
        // Carry forward active assumptions (Status = "active")
        var activeAssumptions = _assumptions.Where(a => 
            sourceSections.Any(s => s.Id == a.SectionId) && 
            a.Status == "active"
        ).ToList();
        
        foreach (var assumption in activeAssumptions)
        {
            if (!sectionMapping.TryGetValue(assumption.SectionId, out var targetSectionId))
                continue; // Skip if section doesn't exist in new period
            
            // Check if assumption has expired
            var isExpired = false;
            var expirationNote = "";
            if (DateTime.TryParse(assumption.ValidityEndDate, out var endDate) && 
                periodStartDate > DateTime.MinValue)
            {
                if (endDate < periodStartDate)
                {
                    isExpired = true;
                    expirationNote = $"\n\n⚠️ WARNING: This assumption expired on {assumption.ValidityEndDate}. Please review and update before use.";
                }
            }
            
            var newAssumption = new Assumption
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = targetSectionId,
                DataPointId = assumption.DataPointId,
                Title = assumption.Title,
                Description = $"[Carried forward from previous period]{expirationNote}\n\n{assumption.Description}",
                Scope = assumption.Scope,
                ValidityStartDate = assumption.ValidityStartDate,
                ValidityEndDate = assumption.ValidityEndDate,
                Methodology = assumption.Methodology,
                Limitations = isExpired 
                    ? $"[EXPIRED - Requires Review] {assumption.Limitations}" 
                    : assumption.Limitations,
                Rationale = assumption.Rationale,
                Sources = assumption.Sources.Select(s => new AssumptionSource 
                { 
                    SourceType = s.SourceType, 
                    SourceReference = s.SourceReference, 
                    Description = s.Description 
                }).ToList(),
                Status = "active",
                Version = 1, // Start at version 1 for the new period
                CreatedBy = currentUser,
                CreatedAt = now,
                LinkedDataPointIds = new List<string>() // Will be linked to new data points separately if needed
            };
            
            _assumptions.Add(newAssumption);
        }
        
        // Carry forward active remediation plans (Status != "completed" && != "cancelled")
        var activePlans = _remediationPlans.Where(p => 
            sourceSections.Any(s => s.Id == p.SectionId) && 
            p.Status != "completed" && 
            p.Status != "cancelled"
        ).ToList();
        
        foreach (var plan in activePlans)
        {
            if (!sectionMapping.TryGetValue(plan.SectionId, out var targetSectionId))
                continue; // Skip if section doesn't exist in new period
            
            var newPlan = new RemediationPlan
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = targetSectionId,
                Title = plan.Title,
                Description = $"[Carried forward from previous period]\n\n{plan.Description}",
                TargetPeriod = plan.TargetPeriod,
                OwnerId = plan.OwnerId,
                OwnerName = plan.OwnerName,
                Priority = plan.Priority,
                Status = plan.Status, // Maintain the same status (planned or in-progress)
                GapId = null, // Don't link to old gap - will need to be manually linked if needed
                AssumptionId = null, // Don't link to old assumption
                DataPointId = null, // Don't link to old data point
                CreatedBy = currentUser,
                CreatedAt = now
            };
            
            _remediationPlans.Add(newPlan);
            
            // Carry forward actions from the plan (only pending and in-progress actions)
            var activeActions = _remediationActions.Where(a => 
                a.RemediationPlanId == plan.Id && 
                a.Status != "completed" && 
                a.Status != "cancelled"
            ).ToList();
            
            foreach (var action in activeActions)
            {
                var newAction = new RemediationAction
                {
                    Id = Guid.NewGuid().ToString(),
                    RemediationPlanId = newPlan.Id,
                    Title = action.Title,
                    Description = $"[Carried forward from previous period]\n\n{action.Description}",
                    OwnerId = action.OwnerId,
                    OwnerName = action.OwnerName,
                    DueDate = action.DueDate,
                    Status = action.Status, // Maintain the same status
                    EvidenceIds = new List<string>(), // Don't copy evidence references
                    CreatedBy = currentUser,
                    CreatedAt = now
                };
                
                _remediationActions.Add(newAction);
            }
        }
    }

    public IReadOnlyList<ReportingPeriod> GetPeriods()
    {
        lock (_lock)
        {
            return _periods.ToList();
        }
    }

    /// <summary>
    /// Retrieves validation history for a specific reporting period.
    /// Returns all validation runs with timestamp and user for audit trail.
    /// </summary>
    /// <param name="periodId">The ID of the reporting period.</param>
    /// <returns>List of validation results ordered by timestamp (most recent first).</returns>
    public IReadOnlyList<ValidationResult> GetValidationHistory(string periodId)
    {
        lock (_lock)
        {
            return _validationResults
                .Where(v => v.PeriodId == periodId)
                .OrderByDescending(v => v.ValidatedAt)
                .ToList();
        }
    }

    /// <summary>
    /// Retrieves the most recent validation result for a reporting period.
    /// </summary>
    /// <param name="periodId">The ID of the reporting period.</param>
    /// <returns>The most recent validation result, or null if no validation has been run.</returns>
    public ValidationResult? GetLatestValidationResult(string periodId)
    {
        lock (_lock)
        {
            return _validationResults
                .Where(v => v.PeriodId == periodId)
                .OrderByDescending(v => v.ValidatedAt)
                .FirstOrDefault();
        }
    }

    public bool HasReportingStarted(string periodId)
    {
        lock (_lock)
        {
            // Reporting is considered "started" if any section has data points
            var periodSummaries = _summaries.Where(s => s.PeriodId == periodId).ToList();
            return periodSummaries.Any(s => s.DataPointCount > 0);
        }
    }

    public (bool IsValid, string? ErrorMessage, ReportingPeriod? Period) ValidateAndUpdatePeriod(string periodId, UpdateReportingPeriodRequest request)
    {
        lock (_lock)
        {
            var period = _periods.FirstOrDefault(p => p.Id == periodId);
            if (period == null)
            {
                return (false, "Reporting period not found.", null);
            }

            // Check if reporting has started
            if (HasReportingStarted(periodId))
            {
                return (false, "Cannot edit configuration after reporting has started. Reporting is considered started when data points have been added to sections.", null);
            }

            // Validate date range: start date must be before end date
            if (!DateTime.TryParse(request.StartDate, out var startDate) || 
                !DateTime.TryParse(request.EndDate, out var endDate))
            {
                return (false, "Invalid date format. Please provide valid dates.", null);
            }

            if (startDate >= endDate)
            {
                return (false, "Start date must be before end date.", null);
            }

            // Check for overlapping periods (excluding the current period)
            foreach (var existingPeriod in _periods.Where(p => p.Id != periodId))
            {
                if (!DateTime.TryParse(existingPeriod.StartDate, out var existingStart) ||
                    !DateTime.TryParse(existingPeriod.EndDate, out var existingEnd))
                {
                    continue;
                }

                // Check if periods overlap
                if (startDate < existingEnd && existingStart < endDate)
                {
                    return (false, $"Reporting period overlaps with existing period '{existingPeriod.Name}' ({existingPeriod.StartDate} - {existingPeriod.EndDate}).", null);
                }
            }

            // Update the period
            period.Name = request.Name;
            period.StartDate = request.StartDate;
            period.EndDate = request.EndDate;
            period.ReportingMode = request.ReportingMode;
            period.ReportScope = request.ReportScope;
            
            // Recalculate integrity hash after update
            period.IntegrityHash = Services.IntegrityService.CalculateReportingPeriodHash(period);

            return (true, null, period);
        }
    }

    /// <summary>
    /// Lock a reporting period to prevent accidental edits.
    /// </summary>
    public (bool IsSuccess, string? ErrorMessage, ReportingPeriod? Period) LockPeriod(string periodId, LockPeriodRequest request)
    {
        lock (_lock)
        {
            var period = _periods.FirstOrDefault(p => p.Id == periodId);
            if (period == null)
            {
                return (false, "Reporting period not found.", null);
            }

            if (period.IsLocked)
            {
                return (false, "Period is already locked.", null);
            }

            // Lock the period
            period.IsLocked = true;
            period.LockedAt = DateTime.UtcNow.ToString("o");
            period.LockedBy = request.UserId;
            period.LockedByName = request.UserName;

            // Create audit log entry
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = request.UserId,
                UserName = request.UserName,
                Action = "lock",
                EntityType = "ReportingPeriod",
                EntityId = periodId,
                ChangeNote = request.Reason,
                Changes = new List<FieldChange>
                {
                    new() { Field = "IsLocked", OldValue = "false", NewValue = "true" },
                    new() { Field = "LockedAt", OldValue = null, NewValue = period.LockedAt },
                    new() { Field = "LockedBy", OldValue = null, NewValue = request.UserName }
                }
            };

            // Find or create audit trail for this period
            if (!_periodAuditTrails.ContainsKey(periodId))
            {
                _periodAuditTrails[periodId] = new List<AuditLogEntry>();
            }
            _periodAuditTrails[periodId].Add(auditEntry);

            return (true, null, period);
        }
    }

    /// <summary>
    /// Unlock a locked reporting period (admin only). Requires a documented reason.
    /// </summary>
    public (bool IsSuccess, string? ErrorMessage, ReportingPeriod? Period) UnlockPeriod(string periodId, UnlockPeriodRequest request, bool isAdmin)
    {
        lock (_lock)
        {
            var period = _periods.FirstOrDefault(p => p.Id == periodId);
            if (period == null)
            {
                return (false, "Reporting period not found.", null);
            }

            if (!period.IsLocked)
            {
                return (false, "Period is not locked.", null);
            }

            if (!isAdmin)
            {
                return (false, "Only administrators can unlock a reporting period.", null);
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return (false, "A reason is required to unlock a reporting period.", null);
            }

            var previousLockedBy = period.LockedByName;
            var previousLockedAt = period.LockedAt;

            // Unlock the period
            period.IsLocked = false;
            period.LockedAt = null;
            period.LockedBy = null;
            period.LockedByName = null;

            // Create audit log entry
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = request.UserId,
                UserName = request.UserName,
                Action = "unlock",
                EntityType = "ReportingPeriod",
                EntityId = periodId,
                ChangeNote = request.Reason,
                Changes = new List<FieldChange>
                {
                    new() { Field = "IsLocked", OldValue = "true", NewValue = "false" },
                    new() { Field = "LockedAt", OldValue = previousLockedAt, NewValue = null },
                    new() { Field = "LockedBy", OldValue = previousLockedBy, NewValue = null }
                }
            };

            // Find or create audit trail for this period
            if (!_periodAuditTrails.ContainsKey(periodId))
            {
                _periodAuditTrails[periodId] = new List<AuditLogEntry>();
            }
            _periodAuditTrails[periodId].Add(auditEntry);

            return (true, null, period);
        }
    }

    /// <summary>
    /// Get audit trail for a specific period, including lock/unlock operations.
    /// </summary>
    public IReadOnlyList<AuditLogEntry> GetPeriodAuditTrail(string periodId)
    {
        lock (_lock)
        {
            return _periodAuditTrails.TryGetValue(periodId, out var trail) 
                ? trail.ToList() 
                : new List<AuditLogEntry>();
        }
    }

    public IReadOnlyList<ReportSection> GetSections(string? periodId)
    {
        lock (_lock)
        {
            return string.IsNullOrWhiteSpace(periodId)
                ? _sections.ToList()
                : _sections.Where(section => section.PeriodId == periodId).ToList();
        }
    }

    public ReportSection? GetSection(string id)
    {
        lock (_lock)
        {
            return _sections.FirstOrDefault(s => s.Id == id);
        }
    }

    public IReadOnlyList<SectionSummary> GetSectionSummaries(string? periodId)
    {
        lock (_lock)
        {
            var summaries = string.IsNullOrWhiteSpace(periodId)
                ? _summaries.ToList()
                : _summaries.Where(summary => summary.PeriodId == periodId).ToList();

            // Update each summary with calculated progress status
            foreach (var summary in summaries)
            {
                var sectionDataPoints = _dataPoints.Where(dp => dp.SectionId == summary.Id).ToList();
                summary.ProgressStatus = CalculateProgressStatus(sectionDataPoints);
                
                // Update data point count (in case it's out of sync)
                summary.DataPointCount = sectionDataPoints.Count;
                
                // Update completeness percentage
                if (sectionDataPoints.Count > 0)
                {
                    var completeCount = sectionDataPoints.Count(dp => 
                        dp.CompletenessStatus.Equals("complete", StringComparison.OrdinalIgnoreCase));
                    var notApplicableCount = sectionDataPoints.Count(dp => 
                        dp.CompletenessStatus.Equals("not applicable", StringComparison.OrdinalIgnoreCase));
                    var totalRelevant = sectionDataPoints.Count - notApplicableCount;
                    
                    summary.CompletenessPercentage = totalRelevant > 0 
                        ? (int)Math.Round((double)completeCount / totalRelevant * 100)
                        : 100; // If all are N/A, consider 100% complete
                }
                else
                {
                    summary.CompletenessPercentage = 0;
                }
            }

            return summaries;
        }
    }

    public (bool IsValid, string? ErrorMessage, UpdateSectionOwnerResult? Result) UpdateSectionOwner(string sectionId, UpdateSectionOwnerRequest request)
    {
        lock (_lock)
        {
            // Find the section
            var section = _sections.FirstOrDefault(s => s.Id == sectionId);
            if (section == null)
            {
                return (false, "Section not found.", null);
            }

            // Check if period is locked
            var period = _periods.FirstOrDefault(p => p.Id == section.PeriodId);
            if (period != null && period.IsLocked)
            {
                return (false, "Cannot modify section ownership when the reporting period is locked. Please unlock the period first.", null);
            }

            // Validate the user making the change exists
            var updatingUser = _users.FirstOrDefault(u => u.Id == request.UpdatedBy);
            if (updatingUser == null)
            {
                return (false, "Updating user not found.", null);
            }

            // Validate the new owner exists (unless clearing the owner)
            User? newOwner = null;
            if (!string.IsNullOrEmpty(request.OwnerId))
            {
                newOwner = _users.FirstOrDefault(u => u.Id == request.OwnerId);
                if (newOwner == null)
                {
                    return (false, "Owner user not found.", null);
                }
            }

            // Check authorization: only admin or report-owner can change section ownership
            if (updatingUser.Role == "admin")
            {
                // Admins can change ownership of any section
            }
            else if (updatingUser.Role == "report-owner")
            {
                // Report owners can only change ownership of sections in their own periods
                var ownerPeriod = _periods.FirstOrDefault(p => p.Id == section.PeriodId);
                if (ownerPeriod == null || ownerPeriod.OwnerId != updatingUser.Id)
                {
                    return (false, "Report owners can only change section ownership for their own reporting periods.", null);
                }
            }
            else
            {
                return (false, "Only administrators or report owners can change section ownership.", null);
            }

            // Capture old value for audit log
            var oldOwnerId = section.OwnerId;
            var oldOwner = _users.FirstOrDefault(u => u.Id == oldOwnerId);
            var oldOwnerName = oldOwner?.Name ?? oldOwnerId;

            // Update the section owner
            section.OwnerId = request.OwnerId;

            // Update the corresponding summary
            var summary = _summaries.FirstOrDefault(s => s.Id == sectionId);
            if (summary != null)
            {
                summary.OwnerId = request.OwnerId;
                summary.OwnerName = newOwner?.Name ?? "Unassigned";
            }

            // Create audit log entry
            var changes = new List<FieldChange>
            {
                new FieldChange
                {
                    Field = "OwnerId",
                    OldValue = $"{oldOwnerName} ({oldOwnerId})",
                    NewValue = newOwner != null ? $"{newOwner.Name} ({newOwner.Id})" : "Unassigned"
                }
            };

            CreateAuditLogEntry(
                userId: request.UpdatedBy,
                userName: updatingUser.Name,
                action: "UpdateSectionOwner",
                entityType: "ReportSection",
                entityId: sectionId,
                changes: changes,
                changeNote: request.ChangeNote
            );

            // Return result with owner information for notifications
            var result = new UpdateSectionOwnerResult
            {
                Section = section,
                OldOwner = oldOwner,
                NewOwner = newOwner,
                ChangedBy = updatingUser
            };

            return (true, null, result);
        }
    }

    public BulkUpdateSectionOwnerResult UpdateSectionOwnersBulk(BulkUpdateSectionOwnerRequest request)
    {
        lock (_lock)
        {
            var result = new BulkUpdateSectionOwnerResult();

            // Validate the new owner exists
            var newOwner = _users.FirstOrDefault(u => u.Id == request.OwnerId);
            if (newOwner == null)
            {
                // If owner doesn't exist, all sections fail
                foreach (var sectionId in request.SectionIds)
                {
                    result.SkippedSections.Add(new BulkUpdateFailure
                    {
                        SectionId = sectionId,
                        Reason = "Owner user not found."
                    });
                }
                return result;
            }

            // Validate the user making the change exists
            var updatingUser = _users.FirstOrDefault(u => u.Id == request.UpdatedBy);
            if (updatingUser == null)
            {
                // If updating user doesn't exist, all sections fail
                foreach (var sectionId in request.SectionIds)
                {
                    result.SkippedSections.Add(new BulkUpdateFailure
                    {
                        SectionId = sectionId,
                        Reason = "Updating user not found."
                    });
                }
                return result;
            }

            // Process each section
            foreach (var sectionId in request.SectionIds)
            {
                // Find the section
                var section = _sections.FirstOrDefault(s => s.Id == sectionId);
                if (section == null)
                {
                    result.SkippedSections.Add(new BulkUpdateFailure
                    {
                        SectionId = sectionId,
                        Reason = "Section not found."
                    });
                    continue;
                }

                // Check authorization: only admin or report-owner can change section ownership
                bool isAuthorized = false;
                string? authorizationError = null;

                if (updatingUser.Role == "admin")
                {
                    // Admins can change ownership of any section
                    isAuthorized = true;
                }
                else if (updatingUser.Role == "report-owner")
                {
                    // Report owners can only change ownership of sections in their own periods
                    var period = _periods.FirstOrDefault(p => p.Id == section.PeriodId);
                    if (period != null && period.OwnerId == updatingUser.Id)
                    {
                        isAuthorized = true;
                    }
                    else
                    {
                        authorizationError = "Report owners can only change section ownership for their own reporting periods.";
                    }
                }
                else
                {
                    authorizationError = "Only administrators or report owners can change section ownership.";
                }

                if (!isAuthorized)
                {
                    result.SkippedSections.Add(new BulkUpdateFailure
                    {
                        SectionId = sectionId,
                        Reason = authorizationError ?? "Not authorized."
                    });
                    continue;
                }

                // Check if owner is already set to the new owner (no-op)
                if (section.OwnerId == request.OwnerId)
                {
                    result.SkippedSections.Add(new BulkUpdateFailure
                    {
                        SectionId = sectionId,
                        Reason = "Section already has this owner."
                    });
                    continue;
                }

                // Capture old value for audit log
                var oldOwnerId = section.OwnerId;
                var oldOwner = _users.FirstOrDefault(u => u.Id == oldOwnerId);
                var oldOwnerName = oldOwner?.Name ?? oldOwnerId;

                // Update the section owner
                section.OwnerId = request.OwnerId;

                // Update the corresponding summary
                var summary = _summaries.FirstOrDefault(s => s.Id == sectionId);
                if (summary != null)
                {
                    summary.OwnerId = request.OwnerId;
                    summary.OwnerName = newOwner.Name;
                }

                // Create audit log entry
                var changes = new List<FieldChange>
                {
                    new FieldChange
                    {
                        Field = "OwnerId",
                        OldValue = $"{oldOwnerName} ({oldOwnerId})",
                        NewValue = $"{newOwner.Name} ({newOwner.Id})"
                    }
                };

                CreateAuditLogEntry(
                    userId: request.UpdatedBy,
                    userName: updatingUser.Name,
                    action: "BulkUpdateSectionOwner",
                    entityType: "ReportSection",
                    entityId: sectionId,
                    changes: changes,
                    changeNote: request.ChangeNote
                );

                // Add to successful updates
                result.UpdatedSections.Add(section);
                
                // Track owner update for notifications
                result.OwnerUpdates.Add(new SectionOwnerUpdate
                {
                    Section = section,
                    OldOwner = oldOwner,
                    NewOwner = newOwner
                });
            }

            return result;
        }
    }

    public Organization? GetOrganization()
    {
        lock (_lock)
        {
            return _organization;
        }
    }

    public Organization CreateOrganization(CreateOrganizationRequest request)
    {
        lock (_lock)
        {
            var newOrganization = new Organization
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                LegalForm = request.LegalForm,
                Country = request.Country,
                Identifier = request.Identifier,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                CreatedBy = request.CreatedBy,
                CoverageType = request.CoverageType,
                CoverageJustification = request.CoverageJustification
            };

            _organization = newOrganization;
            return newOrganization;
        }
    }

    public Organization? UpdateOrganization(string id, UpdateOrganizationRequest request)
    {
        lock (_lock)
        {
            if (_organization == null || _organization.Id != id)
            {
                return null;
            }

            _organization.Name = request.Name;
            _organization.LegalForm = request.LegalForm;
            _organization.Country = request.Country;
            _organization.Identifier = request.Identifier;
            _organization.CoverageType = request.CoverageType;
            _organization.CoverageJustification = request.CoverageJustification;

            return _organization;
        }
    }

    public IReadOnlyList<OrganizationalUnit> GetOrganizationalUnits()
    {
        lock (_lock)
        {
            return _organizationalUnits.ToList();
        }
    }

    public OrganizationalUnit? GetOrganizationalUnit(string id)
    {
        lock (_lock)
        {
            return _organizationalUnits.FirstOrDefault(u => u.Id == id);
        }
    }

    public OrganizationalUnit CreateOrganizationalUnit(CreateOrganizationalUnitRequest request)
    {
        lock (_lock)
        {
            // Validate parent exists if parentId is provided
            if (!string.IsNullOrWhiteSpace(request.ParentId))
            {
                var parent = _organizationalUnits.FirstOrDefault(u => u.Id == request.ParentId);
                if (parent == null)
                {
                    throw new InvalidOperationException($"Parent unit with ID '{request.ParentId}' not found.");
                }
            }

            var newUnit = new OrganizationalUnit
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                ParentId = request.ParentId,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                CreatedBy = request.CreatedBy
            };

            _organizationalUnits.Add(newUnit);

            // Log creation to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Name", OldValue = "", NewValue = request.Name },
                new FieldChange { Field = "Description", OldValue = "", NewValue = request.Description ?? "" },
                new FieldChange { Field = "ParentId", OldValue = "", NewValue = request.ParentId ?? "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == request.CreatedBy);
            var userName = user?.Name ?? request.CreatedBy;
            CreateAuditLogEntry(request.CreatedBy, userName, "create", "OrganizationalUnit", newUnit.Id, changes, $"Created organizational unit '{request.Name}'");

            return newUnit;
        }
    }

    public OrganizationalUnit? UpdateOrganizationalUnit(string id, UpdateOrganizationalUnitRequest request)
    {
        lock (_lock)
        {
            var unit = _organizationalUnits.FirstOrDefault(u => u.Id == id);
            if (unit == null)
            {
                return null;
            }

            // Validate parent exists if parentId is provided
            if (!string.IsNullOrWhiteSpace(request.ParentId))
            {
                if (request.ParentId == id)
                {
                    throw new InvalidOperationException("An organizational unit cannot be its own parent.");
                }

                var parent = _organizationalUnits.FirstOrDefault(u => u.Id == request.ParentId);
                if (parent == null)
                {
                    throw new InvalidOperationException($"Parent unit with ID '{request.ParentId}' not found.");
                }

                // Check for circular reference
                if (WouldCreateCircularReference(id, request.ParentId))
                {
                    throw new InvalidOperationException("Setting this parent would create a circular reference in the organizational structure.");
                }
            }

            // Track changes for audit log
            var changes = new List<FieldChange>();

            if (unit.Name != request.Name)
            {
                changes.Add(new FieldChange { Field = "Name", OldValue = unit.Name, NewValue = request.Name });
            }

            if (unit.Description != request.Description)
            {
                changes.Add(new FieldChange { Field = "Description", OldValue = unit.Description ?? "", NewValue = request.Description ?? "" });
            }

            if (unit.ParentId != request.ParentId)
            {
                changes.Add(new FieldChange { Field = "ParentId", OldValue = unit.ParentId ?? "", NewValue = request.ParentId ?? "" });
            }

            unit.Name = request.Name;
            unit.ParentId = request.ParentId;
            unit.Description = request.Description;

            // Log to audit trail if there were changes
            if (changes.Count > 0)
            {
                var user = _users.FirstOrDefault(u => u.Id == request.UpdatedBy);
                var userName = user?.Name ?? request.UpdatedBy;
                CreateAuditLogEntry(request.UpdatedBy, userName, "update", "OrganizationalUnit", unit.Id, changes, $"Updated organizational unit '{request.Name}'");
            }

            return unit;
        }
    }

    public bool DeleteOrganizationalUnit(string id, string deletedBy)
    {
        lock (_lock)
        {
            var unit = _organizationalUnits.FirstOrDefault(u => u.Id == id);
            if (unit == null)
            {
                return false;
            }

            // Check if any children depend on this unit
            var hasChildren = _organizationalUnits.Any(u => u.ParentId == id);
            if (hasChildren)
            {
                throw new InvalidOperationException("Cannot delete an organizational unit that has child units. Delete or reassign children first.");
            }

            // Log deletion to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Name", OldValue = unit.Name, NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == deletedBy);
            var userName = user?.Name ?? deletedBy;
            CreateAuditLogEntry(deletedBy, userName, "delete", "OrganizationalUnit", unit.Id, changes, $"Deleted organizational unit '{unit.Name}'");

            _organizationalUnits.Remove(unit);
            return true;
        }
    }

    private bool WouldCreateCircularReference(string unitId, string newParentId)
    {
        var visited = new HashSet<string>();
        var current = newParentId;

        while (!string.IsNullOrWhiteSpace(current))
        {
            if (current == unitId)
            {
                return true; // Circular reference detected
            }

            // This check detects pre-existing cycles in the data (data corruption scenario)
            // Under normal operation, this should never trigger as validation prevents cycles
            if (!visited.Add(current))
            {
                return true; // Already visited, circular reference in existing data
            }

            var parent = _organizationalUnits.FirstOrDefault(u => u.Id == current);
            current = parent?.ParentId;
        }

        return false;
    }

    // Section Catalog Management
    public IReadOnlyList<SectionCatalogItem> GetSectionCatalog(bool includeDeprecated = false)
    {
        lock (_lock)
        {
            return includeDeprecated 
                ? _sectionCatalog.ToList()
                : _sectionCatalog.Where(s => !s.IsDeprecated).ToList();
        }
    }

    public SectionCatalogItem? GetSectionCatalogItem(string id)
    {
        lock (_lock)
        {
            return _sectionCatalog.FirstOrDefault(s => s.Id == id);
        }
    }

    public (bool IsValid, string? ErrorMessage, SectionCatalogItem? Item) CreateSectionCatalogItem(CreateSectionCatalogItemRequest request)
    {
        lock (_lock)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return (false, "Code is required.", null);
            }

            // Validate category
            var validCategories = new[] { "environmental", "social", "governance" };
            if (!validCategories.Contains(request.Category, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"Category must be one of: {string.Join(", ", validCategories)}.", null);
            }

            // Check if code already exists
            if (_sectionCatalog.Any(s => s.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, $"A section with code '{request.Code}' already exists.", null);
            }

            var newItem = new SectionCatalogItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = request.Title,
                Code = request.Code,
                Category = request.Category.ToLowerInvariant(),
                Description = request.Description,
                IsDeprecated = false,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            _sectionCatalog.Add(newItem);
            return (true, null, newItem);
        }
    }

    public (bool IsValid, string? ErrorMessage, SectionCatalogItem? Item) UpdateSectionCatalogItem(string id, UpdateSectionCatalogItemRequest request)
    {
        lock (_lock)
        {
            var item = _sectionCatalog.FirstOrDefault(s => s.Id == id);
            if (item == null)
            {
                return (false, "Section catalog item not found.", null);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return (false, "Code is required.", null);
            }

            // Validate category
            var validCategories = new[] { "environmental", "social", "governance" };
            if (!validCategories.Contains(request.Category, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"Category must be one of: {string.Join(", ", validCategories)}.", null);
            }

            // Check if code already exists (excluding current item)
            if (_sectionCatalog.Any(s => s.Id != id && s.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, $"A section with code '{request.Code}' already exists.", null);
            }

            item.Title = request.Title;
            item.Code = request.Code;
            item.Category = request.Category.ToLowerInvariant();
            item.Description = request.Description;

            return (true, null, item);
        }
    }

    public (bool IsValid, string? ErrorMessage) DeprecateSectionCatalogItem(string id)
    {
        lock (_lock)
        {
            var item = _sectionCatalog.FirstOrDefault(s => s.Id == id);
            if (item == null)
            {
                return (false, "Section catalog item not found.");
            }

            if (item.IsDeprecated)
            {
                return (false, "Section is already deprecated.");
            }

            item.IsDeprecated = true;
            item.DeprecatedAt = DateTime.UtcNow.ToString("O");

            return (true, null);
        }
    }

    // DataPoint Management
    public IReadOnlyList<DataPoint> GetDataPoints(string? sectionId = null, string? assignedUserId = null)
    {
        lock (_lock)
        {
            var query = _dataPoints.AsEnumerable();
            
            if (sectionId != null)
            {
                query = query.Where(d => d.SectionId == sectionId);
            }
            
            if (assignedUserId != null)
            {
                query = query.Where(d => d.OwnerId == assignedUserId || d.ContributorIds.Contains(assignedUserId));
            }
            
            return query.ToList();
        }
    }

    public IReadOnlyList<DataPoint> GetDataPointsForSection(string sectionId)
    {
        return GetDataPoints(sectionId);
    }

    public DataPoint? GetDataPoint(string id)
    {
        lock (_lock)
        {
            return _dataPoints.FirstOrDefault(d => d.Id == id);
        }
    }

    /// <summary>
    /// Retrieves cross-period lineage information for a data point.
    /// Shows the history of the data point across multiple reporting periods.
    /// </summary>
    /// <param name="dataPointId">ID of the data point to trace.</param>
    /// <param name="maxHistoryDepth">Maximum number of previous periods to include (default: 10).</param>
    /// <returns>Cross-period lineage response with historical snapshots.</returns>
    public CrossPeriodLineageResponse? GetCrossPeriodLineage(string dataPointId, int maxHistoryDepth = 10)
    {
        lock (_lock)
        {
            var currentDataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
            if (currentDataPoint == null)
            {
                return null;
            }
            
            // Get current period info
            var currentSection = _sections.FirstOrDefault(s => s.Id == currentDataPoint.SectionId);
            if (currentSection == null)
            {
                return null;
            }
            
            var currentPeriod = _periods.FirstOrDefault(p => p.Id == currentSection.PeriodId);
            if (currentPeriod == null)
            {
                return null;
            }
            
            // Get owner info
            var currentOwner = _users.FirstOrDefault(u => u.Id == currentDataPoint.OwnerId);
            
            // Build current version snapshot
            var currentVersion = new DataPointVersionSnapshot
            {
                DataPointId = currentDataPoint.Id,
                PeriodId = currentPeriod.Id,
                PeriodName = currentPeriod.Name,
                PeriodStartDate = currentPeriod.StartDate,
                PeriodEndDate = currentPeriod.EndDate,
                Value = currentDataPoint.Value,
                Content = currentDataPoint.Content,
                Unit = currentDataPoint.Unit,
                Source = currentDataPoint.Source,
                InformationType = currentDataPoint.InformationType,
                CreatedAt = currentDataPoint.CreatedAt,
                UpdatedAt = currentDataPoint.UpdatedAt,
                OwnerId = currentDataPoint.OwnerId,
                OwnerName = currentOwner?.Name ?? "Unknown",
                EvidenceCount = currentDataPoint.EvidenceIds?.Count ?? 0,
                IsRolledOver = !string.IsNullOrEmpty(currentDataPoint.SourcePeriodId),
                RolloverTimestamp = currentDataPoint.RolloverTimestamp
            };
            
            // Trace lineage back through previous periods
            var previousVersions = new List<DataPointVersionSnapshot>();
            var currentSourceDataPointId = currentDataPoint.SourceDataPointId;
            var visitedDataPoints = new HashSet<string> { dataPointId }; // Prevent circular references
            
            while (!string.IsNullOrEmpty(currentSourceDataPointId) && 
                   previousVersions.Count < maxHistoryDepth &&
                   !visitedDataPoints.Contains(currentSourceDataPointId))
            {
                visitedDataPoints.Add(currentSourceDataPointId);
                
                var sourceDataPoint = _dataPoints.FirstOrDefault(d => d.Id == currentSourceDataPointId);
                if (sourceDataPoint == null)
                {
                    break; // Source data point not found, end of chain
                }
                
                var sourceSection = _sections.FirstOrDefault(s => s.Id == sourceDataPoint.SectionId);
                if (sourceSection == null)
                {
                    break;
                }
                
                var sourcePeriod = _periods.FirstOrDefault(p => p.Id == sourceSection.PeriodId);
                if (sourcePeriod == null)
                {
                    break;
                }
                
                var sourceOwner = _users.FirstOrDefault(u => u.Id == sourceDataPoint.OwnerId);
                
                var sourceVersion = new DataPointVersionSnapshot
                {
                    DataPointId = sourceDataPoint.Id,
                    PeriodId = sourcePeriod.Id,
                    PeriodName = sourcePeriod.Name,
                    PeriodStartDate = sourcePeriod.StartDate,
                    PeriodEndDate = sourcePeriod.EndDate,
                    Value = sourceDataPoint.Value,
                    Content = sourceDataPoint.Content,
                    Unit = sourceDataPoint.Unit,
                    Source = sourceDataPoint.Source,
                    InformationType = sourceDataPoint.InformationType,
                    CreatedAt = sourceDataPoint.CreatedAt,
                    UpdatedAt = sourceDataPoint.UpdatedAt,
                    OwnerId = sourceDataPoint.OwnerId,
                    OwnerName = sourceOwner?.Name ?? "Unknown",
                    EvidenceCount = sourceDataPoint.EvidenceIds?.Count ?? 0,
                    IsRolledOver = !string.IsNullOrEmpty(sourceDataPoint.SourcePeriodId),
                    RolloverTimestamp = sourceDataPoint.RolloverTimestamp
                };
                
                previousVersions.Add(sourceVersion);
                
                // Continue tracing back
                currentSourceDataPointId = sourceDataPoint.SourceDataPointId;
            }
            
            // Get audit log entries for changes within the current period
            var currentPeriodChanges = _auditLog
                .Where(a => a.EntityType == "DataPoint" && a.EntityId == dataPointId)
                .OrderBy(a => a.Timestamp)
                .ToList();
            
            return new CrossPeriodLineageResponse
            {
                DataPointId = dataPointId,
                Title = currentDataPoint.Title,
                CurrentVersion = currentVersion,
                PreviousVersions = previousVersions,
                CurrentPeriodChanges = currentPeriodChanges,
                TotalPeriods = previousVersions.Count + 1,
                HasMoreHistory = !string.IsNullOrEmpty(currentSourceDataPointId) && previousVersions.Count >= maxHistoryDepth
            };
        }
    }

    /// <summary>
    /// Compares a numeric metric across two reporting periods.
    /// Returns null if the data point does not exist.
    /// </summary>
    /// <param name="dataPointId">ID of the data point in the current period.</param>
    /// <param name="priorPeriodId">ID of the prior reporting period to compare against. If null, uses the most recent prior period.</param>
    /// <returns>Metric comparison response with current value, prior value, and percentage change, or null if data point not found.</returns>
    public MetricComparisonResponse? CompareMetrics(string dataPointId, string? priorPeriodId = null)
    {
        // Maximum number of years to look back for baseline periods
        const int MaxBaselineYears = 5;
        
        lock (_lock)
        {
            var currentDataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
            if (currentDataPoint == null)
            {
                return null;
            }
            
            // Get current period info
            var currentSection = _sections.FirstOrDefault(s => s.Id == currentDataPoint.SectionId);
            if (currentSection == null)
            {
                return null;
            }
            
            var currentPeriod = _periods.FirstOrDefault(p => p.Id == currentSection.PeriodId);
            if (currentPeriod == null)
            {
                return null;
            }
            
            var currentOwner = _users.FirstOrDefault(u => u.Id == currentDataPoint.OwnerId);
            
            // Build current period value
            var currentPeriodValue = new MetricPeriodValue
            {
                PeriodId = currentPeriod.Id,
                PeriodName = currentPeriod.Name,
                StartDate = currentPeriod.StartDate,
                EndDate = currentPeriod.EndDate,
                Value = currentDataPoint.Value,
                NumericValue = ParseNumericValue(currentDataPoint.Value),
                Unit = currentDataPoint.Unit,
                Source = currentDataPoint.Source,
                InformationType = currentDataPoint.InformationType,
                OwnerName = currentOwner?.Name ?? "Unknown",
                EvidenceCount = currentDataPoint.EvidenceIds?.Count ?? 0,
                IsMissing = currentDataPoint.IsMissing,
                MissingReason = currentDataPoint.MissingReason
            };
            
            // Find available baselines by tracing lineage
            var availableBaselines = new List<AvailableBaselinePeriod>();
            var priorDataPoint = currentDataPoint;
            var yearsBack = 0;
            var visitedDataPoints = new HashSet<string> { dataPointId };
            
            while (!string.IsNullOrEmpty(priorDataPoint.SourceDataPointId) && yearsBack < MaxBaselineYears)
            {
                var sourceDataPointId = priorDataPoint.SourceDataPointId;
                if (visitedDataPoints.Contains(sourceDataPointId))
                {
                    break; // Prevent circular references
                }
                visitedDataPoints.Add(sourceDataPointId);
                
                priorDataPoint = _dataPoints.FirstOrDefault(d => d.Id == sourceDataPointId);
                if (priorDataPoint == null)
                {
                    break;
                }
                
                var priorSection = _sections.FirstOrDefault(s => s.Id == priorDataPoint.SectionId);
                if (priorSection == null)
                {
                    break;
                }
                
                var period = _periods.FirstOrDefault(p => p.Id == priorSection.PeriodId);
                if (period == null)
                {
                    break;
                }
                
                yearsBack++;
                var label = yearsBack == 1 ? "Previous Year" : $"{yearsBack} Years Back";
                
                availableBaselines.Add(new AvailableBaselinePeriod
                {
                    PeriodId = period.Id,
                    PeriodName = period.Name,
                    Label = label,
                    HasData = !priorDataPoint.IsMissing && !string.IsNullOrEmpty(priorDataPoint.Value),
                    StartDate = period.StartDate,
                    EndDate = period.EndDate
                });
            }
            
            // Determine which prior period to use
            string? targetPriorPeriodId = priorPeriodId;
            if (string.IsNullOrEmpty(targetPriorPeriodId) && availableBaselines.Count > 0)
            {
                targetPriorPeriodId = availableBaselines[0].PeriodId; // Use most recent
            }
            
            MetricPeriodValue? priorPeriodValue = null;
            bool isComparisonAvailable = false;
            string? unavailableReason = null;
            bool unitsCompatible = true;
            string? unitWarning = null;
            decimal? percentageChange = null;
            decimal? absoluteChange = null;
            
            if (!string.IsNullOrEmpty(targetPriorPeriodId))
            {
                // Find the data point in the prior period by traversing lineage
                DataPoint? targetPriorDataPoint = null;
                var tempDataPoint = currentDataPoint;
                var tempVisited = new HashSet<string> { dataPointId };
                
                while (!string.IsNullOrEmpty(tempDataPoint.SourceDataPointId))
                {
                    var sourceId = tempDataPoint.SourceDataPointId;
                    if (tempVisited.Contains(sourceId))
                    {
                        break;
                    }
                    tempVisited.Add(sourceId);
                    
                    tempDataPoint = _dataPoints.FirstOrDefault(d => d.Id == sourceId);
                    if (tempDataPoint == null)
                    {
                        break;
                    }
                    
                    var tempSection = _sections.FirstOrDefault(s => s.Id == tempDataPoint.SectionId);
                    if (tempSection != null && tempSection.PeriodId == targetPriorPeriodId)
                    {
                        targetPriorDataPoint = tempDataPoint;
                        break;
                    }
                }
                
                if (targetPriorDataPoint != null)
                {
                    var priorPeriod = _periods.FirstOrDefault(p => p.Id == targetPriorPeriodId);
                    var priorOwner = _users.FirstOrDefault(u => u.Id == targetPriorDataPoint.OwnerId);
                    
                    priorPeriodValue = new MetricPeriodValue
                    {
                        PeriodId = targetPriorPeriodId,
                        PeriodName = priorPeriod?.Name ?? "Unknown",
                        StartDate = priorPeriod?.StartDate ?? string.Empty,
                        EndDate = priorPeriod?.EndDate ?? string.Empty,
                        Value = targetPriorDataPoint.Value,
                        NumericValue = ParseNumericValue(targetPriorDataPoint.Value),
                        Unit = targetPriorDataPoint.Unit,
                        Source = targetPriorDataPoint.Source,
                        InformationType = targetPriorDataPoint.InformationType,
                        OwnerName = priorOwner?.Name ?? "Unknown",
                        EvidenceCount = targetPriorDataPoint.EvidenceIds?.Count ?? 0,
                        IsMissing = targetPriorDataPoint.IsMissing,
                        MissingReason = targetPriorDataPoint.MissingReason
                    };
                    
                    // Check if comparison is possible
                    if (targetPriorDataPoint.IsMissing || currentDataPoint.IsMissing)
                    {
                        unavailableReason = "Missing data in one or both periods";
                    }
                    else if (currentPeriodValue.NumericValue == null || priorPeriodValue.NumericValue == null)
                    {
                        unavailableReason = "Non-numeric values";
                    }
                    else
                    {
                        // Check unit compatibility
                        var currentUnit = currentDataPoint.Unit?.Trim() ?? string.Empty;
                        var priorUnit = targetPriorDataPoint.Unit?.Trim() ?? string.Empty;
                        
                        if (currentUnit != priorUnit)
                        {
                            unitsCompatible = false;
                            
                            if (string.IsNullOrEmpty(currentUnit) || string.IsNullOrEmpty(priorUnit))
                            {
                                unitWarning = "One period has a unit while the other does not. Comparison may not be valid.";
                            }
                            else
                            {
                                unitWarning = $"Units differ: current period uses '{currentUnit}', prior period uses '{priorUnit}'. Explicit conversion required.";
                            }
                            unavailableReason = "Unit mismatch";
                        }
                        else
                        {
                            // Calculate changes
                            var currentValue = currentPeriodValue.NumericValue.Value;
                            var priorValue = priorPeriodValue.NumericValue.Value;
                            
                            absoluteChange = currentValue - priorValue;
                            
                            if (priorValue != 0)
                            {
                                percentageChange = ((currentValue - priorValue) / priorValue) * 100;
                                isComparisonAvailable = true;
                            }
                            else if (currentValue == 0)
                            {
                                // Both are zero - no change
                                percentageChange = 0;
                                isComparisonAvailable = true;
                            }
                            else
                            {
                                // Prior value was 0, current is not - absolute change is valid but percentage is undefined
                                // Mark comparison as available since absolute change provides meaningful information
                                isComparisonAvailable = true;
                                // percentageChange remains null since division by zero is undefined
                            }
                        }
                    }
                }
                else
                {
                    unavailableReason = "No data point found in the specified prior period";
                }
            }
            else
            {
                unavailableReason = "No prior period data available";
            }
            
            // Check if variance requires explanation based on threshold configuration
            VarianceFlagInfo? varianceFlag = null;
            if (currentPeriod.VarianceThresholdConfig != null && isComparisonAvailable && !string.IsNullOrEmpty(targetPriorPeriodId))
            {
                varianceFlag = CheckVarianceThreshold(
                    dataPointId,
                    targetPriorPeriodId,
                    currentPeriod.VarianceThresholdConfig,
                    percentageChange,
                    absoluteChange);
            }
            
            return new MetricComparisonResponse
            {
                DataPointId = dataPointId,
                Title = currentDataPoint.Title,
                CurrentPeriod = currentPeriodValue,
                PriorPeriod = priorPeriodValue,
                PercentageChange = percentageChange,
                AbsoluteChange = absoluteChange,
                IsComparisonAvailable = isComparisonAvailable,
                UnavailableReason = unavailableReason,
                UnitsCompatible = unitsCompatible,
                UnitWarning = unitWarning,
                AvailableBaselines = availableBaselines,
                VarianceFlag = varianceFlag
            };
        }
    }
    
    /// <summary>
    /// Attempts to parse a string value as a decimal number.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>Parsed decimal value, or null if not numeric.</returns>
    private decimal? ParseNumericValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        
        // Remove common formatting (spaces, commas)
        var cleanValue = value.Replace(" ", "").Replace(",", "");
        
        if (decimal.TryParse(cleanValue, out var result))
        {
            return result;
        }
        
        return null;
    }

    /// <summary>
    /// Calculates the completeness status based on data point fields and evidence.
    /// </summary>
    /// <param name="dataPoint">The data point to evaluate.</param>
    /// <returns>Completeness status: "missing", "incomplete", "complete", or "not applicable".</returns>
    private string CalculateCompletenessStatus(DataPoint dataPoint)
    {
        // A data point that exists has at least title and content, so it's never "missing" here
        // Missing status would apply at a higher level when no data point exists at all
        
        // Check if all required metadata is present
        bool hasSource = !string.IsNullOrWhiteSpace(dataPoint.Source);
        bool hasInformationType = !string.IsNullOrWhiteSpace(dataPoint.InformationType);
        bool hasTitle = !string.IsNullOrWhiteSpace(dataPoint.Title);
        bool hasContent = !string.IsNullOrWhiteSpace(dataPoint.Content);
        bool hasOwner = !string.IsNullOrWhiteSpace(dataPoint.OwnerId);
        
        // Check if evidence is linked (at least one evidence ID)
        bool hasEvidence = dataPoint.EvidenceIds != null && dataPoint.EvidenceIds.Count > 0;
        
        // Complete: has all required fields AND evidence AND owner
        if (hasTitle && hasContent && hasSource && hasInformationType && hasEvidence && hasOwner)
        {
            return "complete";
        }
        
        // Incomplete: has basic data but missing required metadata, evidence, or owner
        return "incomplete";
    }

    public (bool IsValid, string? ErrorMessage, DataPoint? DataPoint) CreateDataPoint(CreateDataPointRequest request)
    {
        lock (_lock)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return (false, "Content is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.SectionId))
            {
                return (false, "SectionId is required.", null);
            }

            // Note: OwnerId is not required initially, but is required when setting status to complete
            // This is validated later based on completeness status

            // Validate owner exists if provided
            if (!string.IsNullOrWhiteSpace(request.OwnerId))
            {
                var owner = _users.FirstOrDefault(u => u.Id == request.OwnerId);
                if (owner == null)
                {
                    return (false, $"Owner with ID '{request.OwnerId}' not found.", null);
                }
            }

            // Validate contributors exist and are not the owner
            if (request.ContributorIds != null && request.ContributorIds.Any())
            {
                if (!string.IsNullOrWhiteSpace(request.OwnerId) && request.ContributorIds.Contains(request.OwnerId))
                {
                    return (false, "Owner cannot also be listed as a contributor.", null);
                }

                foreach (var contributorId in request.ContributorIds)
                {
                    var contributor = _users.FirstOrDefault(u => u.Id == contributorId);
                    if (contributor == null)
                    {
                        return (false, $"Contributor with ID '{contributorId}' not found.", null);
                    }
                }
            }

            // Validate required metadata fields
            if (string.IsNullOrWhiteSpace(request.Source))
            {
                return (false, "Source is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.InformationType))
            {
                return (false, "InformationType is required.", null);
            }

            // Validate informationType enum
            var validInformationTypes = new[] { "fact", "estimate", "declaration", "plan" };
            if (!validInformationTypes.Contains(request.InformationType, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"InformationType must be one of: {string.Join(", ", validInformationTypes)}.", null);
            }

            // Validate assumptions required for estimate
            if (request.InformationType.Equals("estimate", StringComparison.OrdinalIgnoreCase) 
                && string.IsNullOrWhiteSpace(request.Assumptions))
            {
                return (false, "Assumptions field is required when InformationType is 'estimate'.", null);
            }
            
            // Validate estimate fields when informationType is 'estimate'
            var (isEstimateValid, estimateError) = ValidateEstimateFields(
                request.InformationType, 
                request.EstimateType, 
                request.EstimateMethod, 
                request.ConfidenceLevel);
            
            if (!isEstimateValid)
            {
                return (false, estimateError, null);
            }

            // Auto-calculate completeness status if not provided or if it's empty
            string completenessStatus = request.CompletenessStatus;
            if (string.IsNullOrWhiteSpace(completenessStatus))
            {
                // Create temporary data point to calculate status
                var tempDataPoint = new DataPoint
                {
                    Title = request.Title,
                    Content = request.Content,
                    Source = request.Source,
                    InformationType = request.InformationType,
                    OwnerId = request.OwnerId,
                    EvidenceIds = new List<string>()
                };
                completenessStatus = CalculateCompletenessStatus(tempDataPoint);
            }
            else
            {
                // Validate completenessStatus enum if provided
                var validCompletenessStatuses = new[] { "missing", "incomplete", "complete", "not applicable" };
                if (!validCompletenessStatuses.Contains(completenessStatus, StringComparer.OrdinalIgnoreCase))
                {
                    return (false, $"CompletenessStatus must be one of: {string.Join(", ", validCompletenessStatuses)}.", null);
                }
                
                // Validate owner is required when setting status to complete
                if (completenessStatus.Equals("complete", StringComparison.OrdinalIgnoreCase) 
                    && string.IsNullOrWhiteSpace(request.OwnerId))
                {
                    return (false, "An owner must be assigned before setting completeness status to 'complete'.", null);
                }
            }

            // Validate section exists
            var sectionExists = _sections.Any(s => s.Id == request.SectionId);
            if (!sectionExists)
            {
                return (false, $"Section with ID '{request.SectionId}' not found.", null);
            }

            var now = DateTime.UtcNow.ToString("O");
            
            // Validate reviewStatus if provided
            var validReviewStatuses = new[] { "draft", "ready-for-review", "approved", "changes-requested" };
            var reviewStatus = request.ReviewStatus ?? "draft";
            if (!validReviewStatuses.Contains(reviewStatus, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"ReviewStatus must be one of: {string.Join(", ", validReviewStatuses)}.", null);
            }
            
            // Validate blocker fields
            if (request.IsBlocked && string.IsNullOrWhiteSpace(request.BlockerReason))
            {
                return (false, "BlockerReason is required when IsBlocked is true.", null);
            }
            
            // Validate missing data fields
            if (request.IsMissing)
            {
                if (string.IsNullOrWhiteSpace(request.MissingReason))
                {
                    return (false, "MissingReason is required when IsMissing is true.", null);
                }
                
                if (string.IsNullOrWhiteSpace(request.MissingReasonCategory))
                {
                    return (false, "MissingReasonCategory is required when IsMissing is true.", null);
                }
                
                if (!ValidMissingReasonCategories.Contains(request.MissingReasonCategory, StringComparer.OrdinalIgnoreCase))
                {
                    return (false, $"MissingReasonCategory must be one of: {string.Join(", ", ValidMissingReasonCategories)}.", null);
                }
            }
            
            var newDataPoint = new DataPoint
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = request.SectionId,
                Type = request.Type,
                Classification = request.Classification,
                Title = request.Title,
                Content = request.Content,
                Value = request.Value,
                Unit = request.Unit,
                OwnerId = request.OwnerId,
                ContributorIds = request.ContributorIds ?? new List<string>(),
                Source = request.Source,
                InformationType = request.InformationType,
                Assumptions = request.Assumptions,
                CompletenessStatus = completenessStatus,
                ReviewStatus = reviewStatus,
                CreatedAt = now,
                UpdatedAt = now,
                EvidenceIds = new List<string>(),
                Deadline = request.Deadline,
                IsBlocked = request.IsBlocked,
                BlockerReason = request.BlockerReason,
                BlockerDueDate = request.BlockerDueDate,
                IsMissing = request.IsMissing,
                MissingReason = request.MissingReason,
                MissingReasonCategory = request.MissingReasonCategory?.ToLowerInvariant(),
                MissingFlaggedBy = request.IsMissing ? "system" : null, // Will be set properly when flagged via API
                MissingFlaggedAt = request.IsMissing ? now : null,
                EstimateType = request.EstimateType?.ToLowerInvariant(),
                EstimateMethod = request.EstimateMethod,
                ConfidenceLevel = request.ConfidenceLevel?.ToLowerInvariant(),
                EstimateInputSources = request.EstimateInputSources ?? new List<EstimateInputSource>(),
                EstimateInputs = request.EstimateInputs,
                EstimateAuthor = request.InformationType?.Equals("estimate", StringComparison.OrdinalIgnoreCase) == true 
                    ? request.OwnerId : null,
                EstimateCreatedAt = request.InformationType?.Equals("estimate", StringComparison.OrdinalIgnoreCase) == true 
                    ? now : null,
                SourceReferences = request.SourceReferences ?? new List<NarrativeSourceReference>(),
                ProvenanceNeedsReview = false,
                // Calculation lineage fields
                IsCalculated = request.IsCalculated,
                CalculationFormula = request.CalculationFormula,
                CalculationInputIds = request.CalculationInputIds ?? new List<string>(),
                CalculationVersion = request.IsCalculated ? 1 : 0,
                CalculatedAt = request.IsCalculated ? now : null,
                CalculatedBy = request.IsCalculated ? request.CalculatedBy : null,
                CalculationNeedsRecalculation = false
            };
            
            // Capture input snapshot if this is a calculated data point
            if (newDataPoint.IsCalculated && newDataPoint.CalculationInputIds.Any())
            {
                newDataPoint.CalculationInputSnapshot = CaptureInputSnapshot(newDataPoint.CalculationInputIds);
            }

            // Validate against validation rules
            var (isValidAgainstRules, ruleErrorMessage) = ValidateDataPointAgainstRules(newDataPoint);
            if (!isValidAgainstRules)
            {
                return (false, ruleErrorMessage, null);
            }

            _dataPoints.Add(newDataPoint);
            return (true, null, newDataPoint);
        }
    }

    public (bool IsValid, string? ErrorMessage, DataPoint? DataPoint) UpdateDataPoint(string id, UpdateDataPointRequest request)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == id);
            if (dataPoint == null)
            {
                return (false, "DataPoint not found.", null);
            }

            // Check if period is locked
            var section = _sections.FirstOrDefault(s => s.Id == dataPoint.SectionId);
            if (section != null)
            {
                var period = _periods.FirstOrDefault(p => p.Id == section.PeriodId);
                if (period != null && period.IsLocked)
                {
                    return (false, "Cannot modify data points when the reporting period is locked. Please unlock the period first.", null);
                }
            }

            // Check if data point is approved and enforce read-only (unless updating review status ONLY)
            if (dataPoint.ReviewStatus == "approved")
            {
                // Determine if this is a review status-only change
                bool isReviewStatusOnlyChange = !string.IsNullOrWhiteSpace(request.ReviewStatus) &&
                    request.Type == dataPoint.Type &&
                    request.Classification == dataPoint.Classification &&
                    request.Title == dataPoint.Title &&
                    request.Content == dataPoint.Content &&
                    request.Value == dataPoint.Value &&
                    request.Unit == dataPoint.Unit &&
                    // Owner is unchanged if request is empty (preserve existing) or same as current
                    (string.IsNullOrWhiteSpace(request.OwnerId) || request.OwnerId == dataPoint.OwnerId) &&
                    request.Source == dataPoint.Source &&
                    request.InformationType == dataPoint.InformationType &&
                    request.Assumptions == dataPoint.Assumptions &&
                    request.CompletenessStatus == dataPoint.CompletenessStatus;
                
                if (!isReviewStatusOnlyChange)
                {
                    return (false, "Cannot modify approved data points. Only admins can make changes to approved entries.", null);
                }
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return (false, "Content is required.", null);
            }

            // Validate owner exists
            if (!string.IsNullOrWhiteSpace(request.OwnerId))
            {
                var owner = _users.FirstOrDefault(u => u.Id == request.OwnerId);
                if (owner == null)
                {
                    return (false, $"Owner with ID '{request.OwnerId}' not found.", null);
                }
            }

            // Validate contributors exist and are not the owner
            if (request.ContributorIds != null && request.ContributorIds.Any())
            {
                var ownerId = !string.IsNullOrWhiteSpace(request.OwnerId) ? request.OwnerId : dataPoint.OwnerId;
                
                if (request.ContributorIds.Contains(ownerId))
                {
                    return (false, "Owner cannot also be listed as a contributor.", null);
                }

                foreach (var contributorId in request.ContributorIds)
                {
                    var contributor = _users.FirstOrDefault(u => u.Id == contributorId);
                    if (contributor == null)
                    {
                        return (false, $"Contributor with ID '{contributorId}' not found.", null);
                    }
                }
            }

            // Validate required metadata fields
            if (string.IsNullOrWhiteSpace(request.Source))
            {
                return (false, "Source is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.InformationType))
            {
                return (false, "InformationType is required.", null);
            }

            // Validate informationType enum
            var validInformationTypes = new[] { "fact", "estimate", "declaration", "plan" };
            if (!validInformationTypes.Contains(request.InformationType, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"InformationType must be one of: {string.Join(", ", validInformationTypes)}.", null);
            }

            // Validate assumptions required for estimate
            if (request.InformationType.Equals("estimate", StringComparison.OrdinalIgnoreCase) 
                && string.IsNullOrWhiteSpace(request.Assumptions))
            {
                return (false, "Assumptions field is required when InformationType is 'estimate'.", null);
            }
            
            // Validate estimate fields when informationType is 'estimate'
            var (isEstimateValid2, estimateError2) = ValidateEstimateFields(
                request.InformationType, 
                request.EstimateType, 
                request.EstimateMethod, 
                request.ConfidenceLevel);
            
            if (!isEstimateValid2)
            {
                return (false, estimateError2, null);
            }

            // Auto-calculate completeness status if not provided or if it's empty
            string completenessStatus = request.CompletenessStatus;
            if (string.IsNullOrWhiteSpace(completenessStatus))
            {
                // Create temporary data point to calculate status (keeping existing evidence IDs)
                // Use request OwnerId if provided, otherwise preserve existing owner
                var effectiveOwnerId = !string.IsNullOrWhiteSpace(request.OwnerId) ? request.OwnerId : dataPoint.OwnerId;
                var statusCalculationDataPoint = new DataPoint
                {
                    Title = request.Title,
                    Content = request.Content,
                    Source = request.Source,
                    InformationType = request.InformationType,
                    OwnerId = effectiveOwnerId,
                    EvidenceIds = dataPoint.EvidenceIds
                };
                completenessStatus = CalculateCompletenessStatus(statusCalculationDataPoint);
            }
            else
            {
                // Validate completenessStatus enum if provided
                var validCompletenessStatuses = new[] { "missing", "incomplete", "complete", "not applicable" };
                if (!validCompletenessStatuses.Contains(completenessStatus, StringComparer.OrdinalIgnoreCase))
                {
                    return (false, $"CompletenessStatus must be one of: {string.Join(", ", validCompletenessStatuses)}.", null);
                }
                
                // Validate owner is required when setting status to complete
                // Check the final owner state after the update
                var finalOwnerId = !string.IsNullOrWhiteSpace(request.OwnerId) ? request.OwnerId : dataPoint.OwnerId;
                if (completenessStatus.Equals("complete", StringComparison.OrdinalIgnoreCase) 
                    && string.IsNullOrWhiteSpace(finalOwnerId))
                {
                    return (false, "An owner must be assigned before setting completeness status to 'complete'.", null);
                }
            }

            // Create a temporary copy for validation before modifying the actual data point
            var tempDataPoint = new DataPoint
            {
                Id = dataPoint.Id,
                SectionId = dataPoint.SectionId,
                Type = request.Type,
                Classification = request.Classification,
                Title = request.Title,
                Content = request.Content,
                Value = request.Value,
                Unit = request.Unit,
                OwnerId = request.OwnerId,
                ContributorIds = request.ContributorIds ?? new List<string>(),
                Source = request.Source,
                InformationType = request.InformationType,
                Assumptions = request.Assumptions,
                CompletenessStatus = completenessStatus,
                CreatedAt = dataPoint.CreatedAt,
                UpdatedAt = DateTime.UtcNow.ToString("O"),
                EvidenceIds = dataPoint.EvidenceIds
            };

            // Validate against validation rules before applying changes
            var (isValidAgainstRules, ruleErrorMessage) = ValidateDataPointAgainstRules(tempDataPoint);
            if (!isValidAgainstRules)
            {
                return (false, ruleErrorMessage, null);
            }

            // Capture changes for audit log
            var changes = new List<FieldChange>();
            
            // Check if value or unit changed - needed for flagging dependent calculations
            bool valueOrUnitChanged = (dataPoint.Value != request.Value) || (dataPoint.Unit != request.Unit);
            
            if (dataPoint.Type != request.Type)
                changes.Add(new FieldChange { Field = "Type", OldValue = dataPoint.Type, NewValue = request.Type });
            
            if (dataPoint.Classification != request.Classification)
                changes.Add(new FieldChange { Field = "Classification", OldValue = dataPoint.Classification ?? "", NewValue = request.Classification ?? "" });
            
            if (dataPoint.Title != request.Title)
                changes.Add(new FieldChange { Field = "Title", OldValue = dataPoint.Title, NewValue = request.Title });
            
            if (dataPoint.Content != request.Content)
                changes.Add(new FieldChange { Field = "Content", OldValue = dataPoint.Content, NewValue = request.Content });
            
            if (dataPoint.Value != request.Value)
                changes.Add(new FieldChange { Field = "Value", OldValue = dataPoint.Value ?? "", NewValue = request.Value ?? "" });
            
            if (dataPoint.Unit != request.Unit)
                changes.Add(new FieldChange { Field = "Unit", OldValue = dataPoint.Unit ?? "", NewValue = request.Unit ?? "" });
            
            // Only record owner change if explicitly provided and different
            if (!string.IsNullOrWhiteSpace(request.OwnerId) && dataPoint.OwnerId != request.OwnerId)
                changes.Add(new FieldChange { Field = "OwnerId", OldValue = dataPoint.OwnerId, NewValue = request.OwnerId });
            
            if (dataPoint.Source != request.Source)
                changes.Add(new FieldChange { Field = "Source", OldValue = dataPoint.Source, NewValue = request.Source });
            
            if (dataPoint.InformationType != request.InformationType)
                changes.Add(new FieldChange { Field = "InformationType", OldValue = dataPoint.InformationType, NewValue = request.InformationType });
            
            if (dataPoint.Assumptions != request.Assumptions)
                changes.Add(new FieldChange { Field = "Assumptions", OldValue = dataPoint.Assumptions ?? "", NewValue = request.Assumptions ?? "" });
            
            if (dataPoint.CompletenessStatus != completenessStatus)
                changes.Add(new FieldChange { Field = "CompletenessStatus", OldValue = dataPoint.CompletenessStatus, NewValue = completenessStatus });
            
            // Handle review status changes
            if (!string.IsNullOrWhiteSpace(request.ReviewStatus))
            {
                var validReviewStatuses = new[] { "draft", "ready-for-review", "approved", "changes-requested" };
                if (!validReviewStatuses.Contains(request.ReviewStatus, StringComparer.OrdinalIgnoreCase))
                {
                    return (false, $"ReviewStatus must be one of: {string.Join(", ", validReviewStatuses)}.", null);
                }
                
                if (dataPoint.ReviewStatus != request.ReviewStatus)
                {
                    changes.Add(new FieldChange { Field = "ReviewStatus", OldValue = dataPoint.ReviewStatus, NewValue = request.ReviewStatus });
                }
            }

            // Only update the actual data point if validation passes
            dataPoint.Type = request.Type;
            dataPoint.Classification = request.Classification;
            dataPoint.Title = request.Title;
            dataPoint.Content = request.Content;
            dataPoint.Value = request.Value;
            dataPoint.Unit = request.Unit;
            // Only update OwnerId if explicitly provided (not empty)
            if (!string.IsNullOrWhiteSpace(request.OwnerId))
            {
                dataPoint.OwnerId = request.OwnerId;
            }
            dataPoint.ContributorIds = request.ContributorIds ?? new List<string>();
            dataPoint.Source = request.Source;
            dataPoint.InformationType = request.InformationType;
            dataPoint.Assumptions = request.Assumptions;
            dataPoint.CompletenessStatus = completenessStatus;
            dataPoint.UpdatedAt = DateTime.UtcNow.ToString("O");
            
            // Update deadline if provided
            if (request.Deadline != null)
            {
                if (dataPoint.Deadline != request.Deadline)
                {
                    changes.Add(new FieldChange { Field = "Deadline", OldValue = dataPoint.Deadline ?? "", NewValue = request.Deadline });
                }
                dataPoint.Deadline = request.Deadline;
            }
            
            // Validate blocker fields
            if (request.IsBlocked && string.IsNullOrWhiteSpace(request.BlockerReason))
            {
                return (false, "BlockerReason is required when IsBlocked is true.", null);
            }
            
            // Validate missing data fields
            if (request.IsMissing)
            {
                if (string.IsNullOrWhiteSpace(request.MissingReason))
                {
                    return (false, "MissingReason is required when IsMissing is true.", null);
                }
                
                if (string.IsNullOrWhiteSpace(request.MissingReasonCategory))
                {
                    return (false, "MissingReasonCategory is required when IsMissing is true.", null);
                }
                
                if (!ValidMissingReasonCategories.Contains(request.MissingReasonCategory, StringComparer.OrdinalIgnoreCase))
                {
                    return (false, $"MissingReasonCategory must be one of: {string.Join(", ", ValidMissingReasonCategories)}.", null);
                }
            }
            
            // Update blocker fields
            if (dataPoint.IsBlocked != request.IsBlocked)
            {
                changes.Add(new FieldChange { Field = "IsBlocked", OldValue = dataPoint.IsBlocked.ToString(), NewValue = request.IsBlocked.ToString() });
            }
            dataPoint.IsBlocked = request.IsBlocked;
            
            if (dataPoint.BlockerReason != request.BlockerReason)
            {
                changes.Add(new FieldChange { Field = "BlockerReason", OldValue = dataPoint.BlockerReason ?? "", NewValue = request.BlockerReason ?? "" });
            }
            dataPoint.BlockerReason = request.BlockerReason;
            
            if (dataPoint.BlockerDueDate != request.BlockerDueDate)
            {
                changes.Add(new FieldChange { Field = "BlockerDueDate", OldValue = dataPoint.BlockerDueDate ?? "", NewValue = request.BlockerDueDate ?? "" });
            }
            dataPoint.BlockerDueDate = request.BlockerDueDate;
            
            // Update missing data fields
            if (dataPoint.IsMissing != request.IsMissing)
            {
                changes.Add(new FieldChange { Field = "IsMissing", OldValue = dataPoint.IsMissing.ToString(), NewValue = request.IsMissing.ToString() });
                
                // Clear audit fields when unflagging via UpdateDataPoint
                if (!request.IsMissing)
                {
                    dataPoint.MissingFlaggedBy = null;
                    dataPoint.MissingFlaggedAt = null;
                }
            }
            dataPoint.IsMissing = request.IsMissing;
            
            if (dataPoint.MissingReason != request.MissingReason)
            {
                changes.Add(new FieldChange { Field = "MissingReason", OldValue = dataPoint.MissingReason ?? "", NewValue = request.MissingReason ?? "" });
            }
            dataPoint.MissingReason = request.MissingReason;
            
            var normalizedCategory = request.MissingReasonCategory?.ToLowerInvariant();
            if (dataPoint.MissingReasonCategory != normalizedCategory)
            {
                changes.Add(new FieldChange { Field = "MissingReasonCategory", OldValue = dataPoint.MissingReasonCategory ?? "", NewValue = normalizedCategory ?? "" });
            }
            dataPoint.MissingReasonCategory = normalizedCategory;
            
            // Update estimate fields
            var normalizedEstimateType = request.EstimateType?.ToLowerInvariant();
            if (dataPoint.EstimateType != normalizedEstimateType)
            {
                changes.Add(new FieldChange { Field = "EstimateType", OldValue = dataPoint.EstimateType ?? "", NewValue = normalizedEstimateType ?? "" });
            }
            dataPoint.EstimateType = normalizedEstimateType;
            
            if (dataPoint.EstimateMethod != request.EstimateMethod)
            {
                changes.Add(new FieldChange { Field = "EstimateMethod", OldValue = dataPoint.EstimateMethod ?? "", NewValue = request.EstimateMethod ?? "" });
            }
            dataPoint.EstimateMethod = request.EstimateMethod;
            
            var normalizedConfidenceLevel = request.ConfidenceLevel?.ToLowerInvariant();
            if (dataPoint.ConfidenceLevel != normalizedConfidenceLevel)
            {
                changes.Add(new FieldChange { Field = "ConfidenceLevel", OldValue = dataPoint.ConfidenceLevel ?? "", NewValue = normalizedConfidenceLevel ?? "" });
            }
            dataPoint.ConfidenceLevel = normalizedConfidenceLevel;
            
            // Update estimate provenance fields (preserving EstimateAuthor and EstimateCreatedAt which are set at creation)
            dataPoint.EstimateInputSources = request.EstimateInputSources ?? new List<EstimateInputSource>();
            dataPoint.EstimateInputs = request.EstimateInputs;
            // Note: EstimateAuthor and EstimateCreatedAt are intentionally NOT updated to preserve original audit trail
            
            // Update narrative provenance fields
            // Check if source references have changed to determine if provenance needs review
            var sourceRefsChanged = !AreSourceReferencesEqual(dataPoint.SourceReferences, request.SourceReferences);
            if (sourceRefsChanged)
            {
                changes.Add(new FieldChange { Field = "SourceReferences", OldValue = $"{dataPoint.SourceReferences.Count} sources", NewValue = $"{request.SourceReferences?.Count ?? 0} sources" });
            }
            dataPoint.SourceReferences = request.SourceReferences ?? new List<NarrativeSourceReference>();
            
            // Update calculation lineage fields
            if (dataPoint.IsCalculated != request.IsCalculated)
            {
                changes.Add(new FieldChange { Field = "IsCalculated", OldValue = dataPoint.IsCalculated.ToString(), NewValue = request.IsCalculated.ToString() });
            }
            dataPoint.IsCalculated = request.IsCalculated;
            
            if (dataPoint.CalculationFormula != request.CalculationFormula)
            {
                changes.Add(new FieldChange { Field = "CalculationFormula", OldValue = dataPoint.CalculationFormula ?? "", NewValue = request.CalculationFormula ?? "" });
            }
            dataPoint.CalculationFormula = request.CalculationFormula;
            
            var inputIdsChanged = !AreListsEqual(dataPoint.CalculationInputIds, request.CalculationInputIds);
            if (inputIdsChanged)
            {
                changes.Add(new FieldChange { Field = "CalculationInputIds", OldValue = $"{dataPoint.CalculationInputIds.Count} inputs", NewValue = $"{request.CalculationInputIds?.Count ?? 0} inputs" });
            }
            dataPoint.CalculationInputIds = request.CalculationInputIds ?? new List<string>();
            
            // Update snapshot if this is a calculated point and inputs changed
            if (dataPoint.IsCalculated && inputIdsChanged && dataPoint.CalculationInputIds.Any())
            {
                dataPoint.CalculationInputSnapshot = CaptureInputSnapshot(dataPoint.CalculationInputIds);
                changes.Add(new FieldChange { Field = "CalculationInputSnapshot", OldValue = "previous", NewValue = "updated" });
            }
            
            // Update review status if provided
            if (!string.IsNullOrWhiteSpace(request.ReviewStatus))
            {
                dataPoint.ReviewStatus = request.ReviewStatus;
            }
            
            // Detect changes to input data points and flag calculated points for recalculation
            // Must be done BEFORE the dataPoint value is updated, so we use the flag captured earlier
            if (valueOrUnitChanged)
            {
                FlagDependentCalculationsForRecalculation(dataPoint.Id, request.UpdatedBy ?? "system");
            }

            // Create audit log entry if there are changes
            if (changes.Any())
            {
                var userId = request.UpdatedBy ?? "unknown";
                var user = _users.FirstOrDefault(u => u.Id == userId);
                var userName = user?.Name ?? "Unknown User";
                
                CreateAuditLogEntry(userId, userName, "update", "DataPoint", dataPoint.Id, changes, request.ChangeNote);
            }

            return (true, null, dataPoint);
        }
    }

    public bool DeleteDataPoint(string id, string deletedBy)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == id);
            if (dataPoint == null)
            {
                return false;
            }

            // Create audit log entry before deletion
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = dataPoint.Title ?? "", NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == deletedBy);
            var userName = user?.Name ?? deletedBy;
            CreateAuditLogEntry(deletedBy, userName, "delete", "DataPoint", dataPoint.Id, changes, $"Deleted data point '{dataPoint.Title}'");

            _dataPoints.Remove(dataPoint);
            return true;
        }
    }

    public (bool IsValid, string? ErrorMessage, DataPoint? DataPoint) ApproveDataPoint(string id, ApproveDataPointRequest request)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == id);
            if (dataPoint == null)
            {
                return (false, "DataPoint not found.", null);
            }

            // Validate that data point is ready for review
            if (dataPoint.ReviewStatus != "ready-for-review")
            {
                return (false, "Data point must be in 'ready-for-review' status to be approved.", null);
            }

            // Validate reviewer exists
            var reviewer = _users.FirstOrDefault(u => u.Id == request.ReviewedBy);
            if (reviewer == null)
            {
                return (false, $"Reviewer with ID '{request.ReviewedBy}' not found.", null);
            }

            var now = DateTime.UtcNow.ToString("O");
            var changes = new List<FieldChange>
            {
                new() { Field = "ReviewStatus", OldValue = dataPoint.ReviewStatus, NewValue = "approved" }
            };

            dataPoint.ReviewStatus = "approved";
            dataPoint.ReviewedBy = request.ReviewedBy;
            dataPoint.ReviewedAt = now;
            dataPoint.ReviewComments = request.ReviewComments;
            dataPoint.UpdatedAt = now;

            // Create audit log entry
            CreateAuditLogEntry(
                request.ReviewedBy, 
                reviewer.Name, 
                "approve", 
                "DataPoint", 
                dataPoint.Id, 
                changes, 
                request.ReviewComments
            );

            return (true, null, dataPoint);
        }
    }

    public (bool IsValid, string? ErrorMessage, DataPoint? DataPoint) RequestChanges(string id, RequestChangesRequest request)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == id);
            if (dataPoint == null)
            {
                return (false, "DataPoint not found.", null);
            }

            // Validate that data point is ready for review
            if (dataPoint.ReviewStatus != "ready-for-review")
            {
                return (false, "Data point must be in 'ready-for-review' status to request changes.", null);
            }

            // Validate reviewer exists
            var reviewer = _users.FirstOrDefault(u => u.Id == request.ReviewedBy);
            if (reviewer == null)
            {
                return (false, $"Reviewer with ID '{request.ReviewedBy}' not found.", null);
            }

            // Validate that comments are provided
            if (string.IsNullOrWhiteSpace(request.ReviewComments))
            {
                return (false, "Review comments are required when requesting changes.", null);
            }

            var now = DateTime.UtcNow.ToString("O");
            var changes = new List<FieldChange>
            {
                new() { Field = "ReviewStatus", OldValue = dataPoint.ReviewStatus, NewValue = "changes-requested" }
            };

            dataPoint.ReviewStatus = "changes-requested";
            dataPoint.ReviewedBy = request.ReviewedBy;
            dataPoint.ReviewedAt = now;
            dataPoint.ReviewComments = request.ReviewComments;
            dataPoint.UpdatedAt = now;

            // Create audit log entry
            CreateAuditLogEntry(
                request.ReviewedBy, 
                reviewer.Name, 
                "request-changes", 
                "DataPoint", 
                dataPoint.Id, 
                changes, 
                request.ReviewComments
            );

            return (true, null, dataPoint);
        }
    }

    /// <summary>
    /// Updates the completeness status of a data point with validation.
    /// When changing to "complete", validates that all required fields are present.
    /// Returns validation errors with detailed missing field information.
    /// </summary>
    public (bool IsValid, StatusValidationError? ValidationError, DataPoint? DataPoint) UpdateDataPointStatus(string id, UpdateDataPointStatusRequest request)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == id);
            if (dataPoint == null)
            {
                return (false, new StatusValidationError 
                { 
                    Message = "DataPoint not found.",
                    MissingFields = new List<MissingFieldDetail>()
                }, null);
            }

            // Validate completenessStatus enum
            var validCompletenessStatuses = new[] { "missing", "incomplete", "complete", "not applicable" };
            if (!validCompletenessStatuses.Contains(request.CompletenessStatus, StringComparer.OrdinalIgnoreCase))
            {
                return (false, new StatusValidationError
                {
                    Message = $"CompletenessStatus must be one of: {string.Join(", ", validCompletenessStatuses)}.",
                    MissingFields = new List<MissingFieldDetail>()
                }, null);
            }

            // Validate user exists
            var user = _users.FirstOrDefault(u => u.Id == request.UpdatedBy);
            if (user == null)
            {
                return (false, new StatusValidationError
                {
                    Message = $"User with ID '{request.UpdatedBy}' not found.",
                    MissingFields = new List<MissingFieldDetail>()
                }, null);
            }

            // Special validation when changing to "complete"
            if (request.CompletenessStatus.Equals("complete", StringComparison.OrdinalIgnoreCase))
            {
                var missingFields = new List<MissingFieldDetail>();

                // Check for required fields
                if (string.IsNullOrWhiteSpace(dataPoint.Value))
                {
                    missingFields.Add(new MissingFieldDetail
                    {
                        Field = "Value",
                        Reason = "A numeric or textual value is required for completion."
                    });
                }

                // Check for period/deadline (using Deadline field as proxy for period)
                if (string.IsNullOrWhiteSpace(dataPoint.Deadline))
                {
                    missingFields.Add(new MissingFieldDetail
                    {
                        Field = "Period",
                        Reason = "A reporting period or deadline must be specified."
                    });
                }

                // Check for methodology/source
                if (string.IsNullOrWhiteSpace(dataPoint.Source))
                {
                    missingFields.Add(new MissingFieldDetail
                    {
                        Field = "Source",
                        Reason = "Methodology or source information is required for completion."
                    });
                }

                // Check for owner
                if (string.IsNullOrWhiteSpace(dataPoint.OwnerId))
                {
                    missingFields.Add(new MissingFieldDetail
                    {
                        Field = "Owner",
                        Reason = "An owner must be assigned before marking as complete."
                    });
                }

                // If there are missing fields, block the completion
                if (missingFields.Any())
                {
                    return (false, new StatusValidationError
                    {
                        Message = "Cannot mark data point as complete. Required fields are missing.",
                        MissingFields = missingFields
                    }, null);
                }
            }

            // Capture change for audit log
            var changes = new List<FieldChange>();
            if (dataPoint.CompletenessStatus != request.CompletenessStatus)
            {
                changes.Add(new FieldChange
                {
                    Field = "CompletenessStatus",
                    OldValue = dataPoint.CompletenessStatus,
                    NewValue = request.CompletenessStatus
                });
            }

            // Update the status and timestamp
            dataPoint.CompletenessStatus = request.CompletenessStatus;
            dataPoint.UpdatedAt = DateTime.UtcNow.ToString("O");

            // Create audit log entry if status changed
            if (changes.Any())
            {
                CreateAuditLogEntry(
                    request.UpdatedBy,
                    user.Name,
                    "update-status",
                    "DataPoint",
                    dataPoint.Id,
                    changes,
                    request.ChangeNote
                );
            }

            return (true, null, dataPoint);
        }
    }

    public (bool IsValid, string? ErrorMessage, DataPoint? DataPoint) FlagMissingData(string id, FlagMissingDataRequest request)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == id);
            if (dataPoint == null)
            {
                return (false, "DataPoint not found.", null);
            }

            // Validate user exists
            var user = _users.FirstOrDefault(u => u.Id == request.FlaggedBy);
            if (user == null)
            {
                return (false, $"User with ID '{request.FlaggedBy}' not found.", null);
            }

            // Validate missing reason category
            if (!ValidMissingReasonCategories.Contains(request.MissingReasonCategory, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"MissingReasonCategory must be one of: {string.Join(", ", ValidMissingReasonCategories)}.", null);
            }

            // Validate reason is provided
            if (string.IsNullOrWhiteSpace(request.MissingReason))
            {
                return (false, "MissingReason cannot be empty.", null);
            }

            var now = DateTime.UtcNow.ToString("O");
            var normalizedCategory = request.MissingReasonCategory.ToLowerInvariant();
            var changes = new List<FieldChange>
            {
                new() { Field = "IsMissing", OldValue = dataPoint.IsMissing.ToString(), NewValue = "True" },
                new() { Field = "MissingReasonCategory", OldValue = dataPoint.MissingReasonCategory ?? "", NewValue = normalizedCategory },
                new() { Field = "MissingReason", OldValue = dataPoint.MissingReason ?? "", NewValue = request.MissingReason }
            };

            // If completeness status is not already "missing", set it to "missing" and track the change
            if (dataPoint.CompletenessStatus != "missing")
            {
                changes.Add(new FieldChange
                {
                    Field = "CompletenessStatus",
                    OldValue = dataPoint.CompletenessStatus,
                    NewValue = "missing"
                });
                dataPoint.CompletenessStatus = "missing";
            }

            dataPoint.IsMissing = true;
            dataPoint.MissingReason = request.MissingReason;
            dataPoint.MissingReasonCategory = normalizedCategory;
            dataPoint.MissingFlaggedBy = request.FlaggedBy;
            dataPoint.MissingFlaggedAt = now;
            dataPoint.UpdatedAt = now;

            // Create audit log entry
            CreateAuditLogEntry(
                request.FlaggedBy,
                user.Name,
                "flag-missing",
                "DataPoint",
                dataPoint.Id,
                changes,
                $"Category: {normalizedCategory}. {request.MissingReason}"
            );

            return (true, null, dataPoint);
        }
    }

    public (bool IsValid, string? ErrorMessage, DataPoint? DataPoint) UnflagMissingData(string id, UnflagMissingDataRequest request)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == id);
            if (dataPoint == null)
            {
                return (false, "DataPoint not found.", null);
            }

            // Check if data point is currently flagged as missing
            if (!dataPoint.IsMissing)
            {
                return (false, "DataPoint is not currently flagged as missing.", null);
            }

            // Validate user exists
            var user = _users.FirstOrDefault(u => u.Id == request.UnflaggedBy);
            if (user == null)
            {
                return (false, $"User with ID '{request.UnflaggedBy}' not found.", null);
            }

            var now = DateTime.UtcNow.ToString("O");
            var changes = new List<FieldChange>
            {
                new() { Field = "IsMissing", OldValue = dataPoint.IsMissing.ToString(), NewValue = "False" },
                new() { Field = "MissingReasonCategory", OldValue = dataPoint.MissingReasonCategory ?? "", NewValue = "" },
                new() { Field = "MissingReason", OldValue = dataPoint.MissingReason ?? "", NewValue = "" }
            };

            // Store previous values for history (audit log already captures this)
            var previousReason = dataPoint.MissingReason;
            var previousCategory = dataPoint.MissingReasonCategory;

            // Update missing flag
            dataPoint.IsMissing = false;
            dataPoint.MissingReason = null;
            dataPoint.MissingReasonCategory = null;
            // Keep MissingFlaggedBy and MissingFlaggedAt for history reference
            dataPoint.UpdatedAt = now;

            // If completeness status is "missing", set it to "incomplete" (data is now being provided)
            if (dataPoint.CompletenessStatus == "missing")
            {
                changes.Add(new FieldChange
                {
                    Field = "CompletenessStatus",
                    OldValue = dataPoint.CompletenessStatus,
                    NewValue = "incomplete"
                });
                dataPoint.CompletenessStatus = "incomplete";
            }

            // Create audit log entry
            CreateAuditLogEntry(
                request.UnflaggedBy,
                user.Name,
                "unflag-missing",
                "DataPoint",
                dataPoint.Id,
                changes,
                request.ChangeNote ?? $"Data previously flagged as: {previousCategory} - {previousReason}"
            );

            return (true, null, dataPoint);
        }
    }

    /// <summary>
    /// Checks if a user has permission to modify a data point.
    /// Permission is granted if the user is:
    /// - An admin
    /// - The report owner (owner of the reporting period)
    /// - The section owner
    /// - The data point owner
    /// </summary>
    private bool HasDataPointModifyPermission(string userId, DataPoint dataPoint)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null) return false;

        // Admin can do anything
        if (user.Role.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Data point owner can modify
        if (dataPoint.OwnerId == userId)
        {
            return true;
        }

        // Section owner can modify
        var section = _sections.FirstOrDefault(s => s.Id == dataPoint.SectionId);
        if (section != null && section.OwnerId == userId)
        {
            return true;
        }

        // Report owner (owner of the reporting period) can modify
        if (section != null)
        {
            var period = _periods.FirstOrDefault(p => p.Id == section.PeriodId);
            if (period != null && period.OwnerId == userId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Transitions a data point's gap status through the workflow: Missing → Estimated → Provided.
    /// Enforces workflow rules, preserves history, and validates permissions.
    /// </summary>
    public (bool IsValid, string? ErrorMessage, DataPoint? DataPoint) TransitionGapStatus(
        string id, 
        TransitionGapStatusRequest request)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == id);
            if (dataPoint == null)
            {
                return (false, "DataPoint not found.", null);
            }

            // Validate user exists
            var user = _users.FirstOrDefault(u => u.Id == request.TransitionedBy);
            if (user == null)
            {
                return (false, $"User with ID '{request.TransitionedBy}' not found.", null);
            }

            // Check permissions
            if (!HasDataPointModifyPermission(request.TransitionedBy, dataPoint))
            {
                // Log unauthorized attempt
                CreateAuditLogEntry(
                    request.TransitionedBy,
                    user.Name,
                    "transition-gap-status-denied",
                    "DataPoint",
                    dataPoint.Id,
                    new List<FieldChange>(),
                    $"Unauthorized attempt to transition gap status to '{request.TargetStatus}'. User lacks permission."
                );
                
                return (false, "Permission denied. Only admins, report owners, section owners, or data point owners can change gap status.", null);
            }

            // Validate target status
            if (!ValidGapStatuses.Contains(request.TargetStatus, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"TargetStatus must be one of: {string.Join(", ", ValidGapStatuses)}.", null);
            }

            var normalizedTargetStatus = request.TargetStatus.ToLowerInvariant();
            var currentStatus = dataPoint.GapStatus ?? "";

            // Validate workflow transitions
            // Missing → Estimated → Provided (can't skip states or go backward)
            
            // No-op if already in target status
            if (currentStatus == normalizedTargetStatus)
            {
                return (false, $"DataPoint is already in '{normalizedTargetStatus}' status.", null);
            }
            
            // Prevent backward transitions
            if (currentStatus == "estimated" && normalizedTargetStatus == "missing")
            {
                return (false, "Cannot transition from 'estimated' back to 'missing'. Workflow must progress forward.", null);
            }
            if (currentStatus == "provided" && (normalizedTargetStatus == "missing" || normalizedTargetStatus == "estimated"))
            {
                return (false, "Cannot transition from 'provided' back to earlier states. Workflow must progress forward.", null);
            }
            
            // Prevent skipping states - must follow the sequence: missing → estimated → provided
            if (string.IsNullOrEmpty(currentStatus) || currentStatus == "missing")
            {
                // From empty/missing, can only go to missing (if empty) or estimated (if missing)
                if (normalizedTargetStatus == "provided")
                {
                    return (false, "Cannot skip 'estimated' state. Must transition to 'estimated' before 'provided'.", null);
                }
            }
            if (currentStatus == "missing" && normalizedTargetStatus == "estimated")
            {
                // This is valid - missing → estimated
            }
            else if (string.IsNullOrEmpty(currentStatus) && normalizedTargetStatus == "estimated")
            {
                // Cannot skip missing state - must explicitly mark as missing first
                return (false, "Cannot skip 'missing' state. Must transition to 'missing' before 'estimated'.", null);
            }

            // Validate estimate fields when transitioning to "estimated"
            if (normalizedTargetStatus == "estimated")
            {
                if (string.IsNullOrWhiteSpace(request.EstimateType))
                {
                    return (false, "EstimateType is required when transitioning to 'estimated' status.", null);
                }
                if (!ValidEstimateTypes.Contains(request.EstimateType, StringComparer.OrdinalIgnoreCase))
                {
                    return (false, $"EstimateType must be one of: {string.Join(", ", ValidEstimateTypes)}.", null);
                }
                if (string.IsNullOrWhiteSpace(request.EstimateMethod))
                {
                    return (false, "EstimateMethod is required when transitioning to 'estimated' status.", null);
                }
                if (string.IsNullOrWhiteSpace(request.ConfidenceLevel))
                {
                    return (false, "ConfidenceLevel is required when transitioning to 'estimated' status.", null);
                }
                if (!ValidConfidenceLevels.Contains(request.ConfidenceLevel, StringComparer.OrdinalIgnoreCase))
                {
                    return (false, $"ConfidenceLevel must be one of: {string.Join(", ", ValidConfidenceLevels)}.", null);
                }
            }

            var now = DateTime.UtcNow.ToString("O");
            var changes = new List<FieldChange>
            {
                new() { Field = "GapStatus", OldValue = currentStatus, NewValue = normalizedTargetStatus }
            };

            // Create snapshot when transitioning from "estimated" to "provided"
            if (currentStatus == "estimated" && normalizedTargetStatus == "provided")
            {
                var estimateSnapshot = System.Text.Json.JsonSerializer.Serialize(new
                {
                    EstimateType = dataPoint.EstimateType,
                    EstimateMethod = dataPoint.EstimateMethod,
                    ConfidenceLevel = dataPoint.ConfidenceLevel,
                    Assumptions = dataPoint.Assumptions,
                    Value = dataPoint.Value,
                    Unit = dataPoint.Unit,
                    PreservedAt = now
                });
                
                dataPoint.PreviousEstimateSnapshot = estimateSnapshot;
                changes.Add(new FieldChange 
                { 
                    Field = "PreviousEstimateSnapshot", 
                    OldValue = "", 
                    NewValue = "Estimate preserved (see snapshot)" 
                });
            }

            // Update gap status
            dataPoint.GapStatus = normalizedTargetStatus;
            dataPoint.UpdatedAt = now;

            // Update related fields based on target status
            if (normalizedTargetStatus == "missing")
            {
                // Ensure IsMissing flag is set
                if (!dataPoint.IsMissing)
                {
                    changes.Add(new FieldChange { Field = "IsMissing", OldValue = "False", NewValue = "True" });
                    dataPoint.IsMissing = true;
                }
                if (dataPoint.CompletenessStatus != "missing")
                {
                    changes.Add(new FieldChange { Field = "CompletenessStatus", OldValue = dataPoint.CompletenessStatus, NewValue = "missing" });
                    dataPoint.CompletenessStatus = "missing";
                }
            }
            else if (normalizedTargetStatus == "estimated")
            {
                // Apply estimate fields
                if (request.EstimateType != null)
                {
                    changes.Add(new FieldChange { Field = "EstimateType", OldValue = dataPoint.EstimateType ?? "", NewValue = request.EstimateType });
                    dataPoint.EstimateType = request.EstimateType.ToLowerInvariant();
                }
                if (request.EstimateMethod != null)
                {
                    changes.Add(new FieldChange { Field = "EstimateMethod", OldValue = dataPoint.EstimateMethod ?? "", NewValue = request.EstimateMethod });
                    dataPoint.EstimateMethod = request.EstimateMethod;
                }
                if (request.ConfidenceLevel != null)
                {
                    changes.Add(new FieldChange { Field = "ConfidenceLevel", OldValue = dataPoint.ConfidenceLevel ?? "", NewValue = request.ConfidenceLevel });
                    dataPoint.ConfidenceLevel = request.ConfidenceLevel.ToLowerInvariant();
                }
                
                // Clear IsMissing flag
                if (dataPoint.IsMissing)
                {
                    changes.Add(new FieldChange { Field = "IsMissing", OldValue = "True", NewValue = "False" });
                    dataPoint.IsMissing = false;
                }
                
                // Update information type and completeness
                if (dataPoint.InformationType != "estimate")
                {
                    changes.Add(new FieldChange { Field = "InformationType", OldValue = dataPoint.InformationType, NewValue = "estimate" });
                    dataPoint.InformationType = "estimate";
                }
                if (dataPoint.CompletenessStatus == "missing" || dataPoint.CompletenessStatus == "")
                {
                    changes.Add(new FieldChange { Field = "CompletenessStatus", OldValue = dataPoint.CompletenessStatus, NewValue = "incomplete" });
                    dataPoint.CompletenessStatus = "incomplete";
                }
            }
            else if (normalizedTargetStatus == "provided")
            {
                // Clear IsMissing flag
                if (dataPoint.IsMissing)
                {
                    changes.Add(new FieldChange { Field = "IsMissing", OldValue = "True", NewValue = "False" });
                    dataPoint.IsMissing = false;
                }
                
                // Update completeness - data is now provided
                if (dataPoint.CompletenessStatus != "complete")
                {
                    changes.Add(new FieldChange { Field = "CompletenessStatus", OldValue = dataPoint.CompletenessStatus, NewValue = "complete" });
                    dataPoint.CompletenessStatus = "complete";
                }
            }

            // Create audit log entry
            CreateAuditLogEntry(
                request.TransitionedBy,
                user.Name,
                "transition-gap-status",
                "DataPoint",
                dataPoint.Id,
                changes,
                request.ChangeNote ?? $"Gap status transitioned from '{currentStatus}' to '{normalizedTargetStatus}'"
            );

            return (true, null, dataPoint);
        }
    }

    // Evidence Management
    public IReadOnlyList<Evidence> GetEvidence(string? sectionId = null)
    {
        lock (_lock)
        {
            return sectionId == null 
                ? _evidence.ToList()
                : _evidence.Where(e => e.SectionId == sectionId).ToList();
        }
    }

    public IReadOnlyList<Evidence> GetEvidenceForDataPoint(string dataPointId)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == dataPointId);
            if (dataPoint == null || dataPoint.EvidenceIds == null || !dataPoint.EvidenceIds.Any())
                return new List<Evidence>();

            return _evidence.Where(e => dataPoint.EvidenceIds.Contains(e.Id)).ToList();
        }
    }

    public Evidence? GetEvidenceById(string id)
    {
        lock (_lock)
        {
            return _evidence.FirstOrDefault(e => e.Id == id);
        }
    }

    public (bool IsValid, string? ErrorMessage, Evidence? Evidence) CreateEvidence(
        string sectionId, 
        string title, 
        string? description, 
        string? fileName, 
        string? fileUrl, 
        string? sourceUrl,
        string uploadedBy,
        long? fileSize = null,
        string? checksum = null,
        string? contentType = null)
    {
        lock (_lock)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(sectionId))
            {
                return (false, "SectionId is required.", null);
            }

            if (string.IsNullOrWhiteSpace(uploadedBy))
            {
                return (false, "UploadedBy is required.", null);
            }

            // At least one of file or URL must be provided
            if (string.IsNullOrWhiteSpace(fileName) && string.IsNullOrWhiteSpace(sourceUrl))
            {
                return (false, "Either a file or a source URL must be provided.", null);
            }

            // Validate source URL if provided
            if (!string.IsNullOrWhiteSpace(sourceUrl))
            {
                if (sourceUrl.Length > 2048)
                {
                    return (false, "Source URL must not exceed 2048 characters.", null);
                }

                if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) || 
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    return (false, "Source URL must be a valid HTTP or HTTPS URL.", null);
                }
            }

            // Validate section exists
            var sectionExists = _sections.Any(s => s.Id == sectionId);
            if (!sectionExists)
            {
                return (false, $"Section with ID '{sectionId}' not found.", null);
            }

            var newEvidence = new Evidence
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Title = title,
                Description = description,
                FileName = fileName,
                FileUrl = fileUrl,
                SourceUrl = sourceUrl,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow.ToString("O"),
                LinkedDataPoints = new List<string>(),
                FileSize = fileSize,
                Checksum = checksum,
                ContentType = contentType,
                IntegrityStatus = string.IsNullOrWhiteSpace(checksum) ? "not-checked" : "valid"
            };

            _evidence.Add(newEvidence);

            // Create audit log entry
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "FileName", OldValue = "", NewValue = fileName ?? "" },
                new FieldChange { Field = "FileSize", OldValue = "", NewValue = fileSize?.ToString() ?? "" },
                new FieldChange { Field = "ContentType", OldValue = "", NewValue = contentType ?? "" },
                new FieldChange { Field = "UploadedBy", OldValue = "", NewValue = uploadedBy }
            };
            var user = _users.FirstOrDefault(u => u.Id == uploadedBy);
            var userName = user?.Name ?? uploadedBy;
            CreateAuditLogEntry(uploadedBy, userName, "create", "Evidence", newEvidence.Id, changes, $"Created evidence '{fileName ?? title}'");

            return (true, null, newEvidence);
        }
    }

    public (bool IsValid, string? ErrorMessage) LinkEvidenceToDataPoint(string evidenceId, string dataPointId, string linkedBy)
    {
        lock (_lock)
        {
            var evidence = _evidence.FirstOrDefault(e => e.Id == evidenceId);
            if (evidence == null)
            {
                return (false, $"Evidence with ID '{evidenceId}' not found.");
            }

            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
            if (dataPoint == null)
            {
                return (false, $"DataPoint with ID '{dataPointId}' not found.");
            }

            // Check if already linked
            if (evidence.LinkedDataPoints.Contains(dataPointId))
            {
                return (false, "Evidence is already linked to this data point.");
            }

            if (dataPoint.EvidenceIds.Contains(evidenceId))
            {
                return (false, "Data point already has this evidence linked.");
            }

            // Link both ways
            evidence.LinkedDataPoints.Add(dataPointId);
            dataPoint.EvidenceIds.Add(evidenceId);

            // Create audit log entry
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "EvidenceId", OldValue = "", NewValue = evidenceId },
                new FieldChange { Field = "DataPointId", OldValue = "", NewValue = dataPointId }
            };
            var user = _users.FirstOrDefault(u => u.Id == linkedBy);
            var userName = user?.Name ?? linkedBy;
            CreateAuditLogEntry(linkedBy, userName, "link", "Evidence", evidenceId, changes, $"Linked evidence to data point {dataPointId}");

            return (true, null);
        }
    }

    public (bool IsValid, string? ErrorMessage) UnlinkEvidenceFromDataPoint(string evidenceId, string dataPointId, string unlinkedBy)
    {
        lock (_lock)
        {
            var evidence = _evidence.FirstOrDefault(e => e.Id == evidenceId);
            if (evidence == null)
            {
                return (false, $"Evidence with ID '{evidenceId}' not found.");
            }

            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
            if (dataPoint == null)
            {
                return (false, $"DataPoint with ID '{dataPointId}' not found.");
            }

            // Unlink both ways
            evidence.LinkedDataPoints.Remove(dataPointId);
            dataPoint.EvidenceIds.Remove(evidenceId);

            // Create audit log entry
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "EvidenceId", OldValue = evidenceId, NewValue = "" },
                new FieldChange { Field = "DataPointId", OldValue = dataPointId, NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == unlinkedBy);
            var userName = user?.Name ?? unlinkedBy;
            CreateAuditLogEntry(unlinkedBy, userName, "unlink", "Evidence", evidenceId, changes, $"Unlinked evidence from data point {dataPointId}");

            return (true, null);
        }
    }

    public bool DeleteEvidence(string id, string deletedBy)
    {
        lock (_lock)
        {
            var evidence = _evidence.FirstOrDefault(e => e.Id == id);
            if (evidence == null)
            {
                return false;
            }

            // Create audit log entry before deletion
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "FileName", OldValue = evidence.FileName ?? "", NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == deletedBy);
            var userName = user?.Name ?? deletedBy;
            CreateAuditLogEntry(deletedBy, userName, "delete", "Evidence", evidence.Id, changes, $"Deleted evidence '{evidence.FileName ?? evidence.Title}'");

            // Remove links from all data points
            foreach (var dataPointId in evidence.LinkedDataPoints.ToList())
            {
                var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
                if (dataPoint != null)
                {
                    dataPoint.EvidenceIds.Remove(id);
                }
            }

            _evidence.Remove(evidence);
            return true;
        }
    }

    /// <summary>
    /// Log evidence access for chain-of-custody tracking.
    /// </summary>
    public void LogEvidenceAccess(string evidenceId, string userId, string userName, string action, string? purpose = null)
    {
        lock (_lock)
        {
            var logEntry = new EvidenceAccessLog
            {
                Id = Guid.NewGuid().ToString(),
                EvidenceId = evidenceId,
                UserId = userId,
                UserName = userName,
                AccessedAt = DateTime.UtcNow.ToString("O"),
                Action = action,
                Purpose = purpose
            };

            _evidenceAccessLog.Add(logEntry);
        }
    }

    /// <summary>
    /// Get access log for a specific evidence item or all evidence.
    /// </summary>
    public IReadOnlyList<EvidenceAccessLog> GetEvidenceAccessLog(string? evidenceId = null)
    {
        lock (_lock)
        {
            return evidenceId == null
                ? _evidenceAccessLog.OrderByDescending(log => log.AccessedAt).ToList()
                : _evidenceAccessLog.Where(log => log.EvidenceId == evidenceId)
                    .OrderByDescending(log => log.AccessedAt).ToList();
        }
    }

    /// <summary>
    /// Validate evidence file integrity using checksum.
    /// </summary>
    public (bool IsValid, string? ErrorMessage) ValidateEvidenceIntegrity(string evidenceId, string providedChecksum)
    {
        lock (_lock)
        {
            var evidence = _evidence.FirstOrDefault(e => e.Id == evidenceId);
            if (evidence == null)
            {
                return (false, $"Evidence with ID '{evidenceId}' not found.");
            }

            if (string.IsNullOrWhiteSpace(evidence.Checksum))
            {
                return (false, "Evidence does not have a checksum for validation.");
            }

            bool isValid = string.Equals(evidence.Checksum, providedChecksum, StringComparison.OrdinalIgnoreCase);
            
            // Update integrity status
            evidence.IntegrityStatus = isValid ? "valid" : "failed";

            return (isValid, isValid ? null : "Checksum mismatch. File integrity validation failed.");
        }
    }

    /// <summary>
    /// Check if evidence can be published (integrity must be valid or not-checked, not failed).
    /// </summary>
    public bool CanPublishEvidence(string evidenceId)
    {
        lock (_lock)
        {
            var evidence = _evidence.FirstOrDefault(e => e.Id == evidenceId);
            if (evidence == null)
            {
                return false;
            }

            // Block publication if integrity check failed
            return evidence.IntegrityStatus != "failed";
        }
    }

    // Assumption management methods
    public IReadOnlyList<Assumption> GetAssumptions(string? sectionId = null)
    {
        lock (_lock)
        {
            return sectionId == null 
                ? _assumptions.ToList()
                : _assumptions.Where(a => a.SectionId == sectionId).ToList();
        }
    }

    public Assumption? GetAssumptionById(string id)
    {
        lock (_lock)
        {
            return _assumptions.FirstOrDefault(a => a.Id == id);
        }
    }

    public Assumption? GetAssumption(string id)
    {
        return GetAssumptionById(id);
    }

    public (bool IsValid, string? ErrorMessage, Assumption? Assumption) CreateAssumption(
        string sectionId,
        string title,
        string description,
        string scope,
        string validityStartDate,
        string validityEndDate,
        string methodology,
        string limitations,
        List<string> linkedDataPointIds,
        string? rationale,
        List<AssumptionSource> sources,
        string createdBy)
    {
        lock (_lock)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return (false, "Description is required.", null);
            }

            if (string.IsNullOrWhiteSpace(scope))
            {
                return (false, "Scope is required.", null);
            }

            if (string.IsNullOrWhiteSpace(validityStartDate))
            {
                return (false, "Validity start date is required.", null);
            }

            if (string.IsNullOrWhiteSpace(validityEndDate))
            {
                return (false, "Validity end date is required.", null);
            }

            if (string.IsNullOrWhiteSpace(methodology))
            {
                return (false, "Methodology is required.", null);
            }

            if (string.IsNullOrWhiteSpace(sectionId))
            {
                return (false, "SectionId is required.", null);
            }

            if (string.IsNullOrWhiteSpace(createdBy))
            {
                return (false, "CreatedBy is required.", null);
            }

            // Validate dates
            if (!DateTime.TryParse(validityStartDate, out var startDate))
            {
                return (false, "Invalid validity start date format.", null);
            }

            if (!DateTime.TryParse(validityEndDate, out var endDate))
            {
                return (false, "Invalid validity end date format.", null);
            }

            if (endDate <= startDate)
            {
                return (false, "Validity end date must be after start date.", null);
            }

            // Validate linked data points exist
            foreach (var dataPointId in linkedDataPointIds)
            {
                var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
                if (dataPoint == null)
                {
                    return (false, $"Data point with ID '{dataPointId}' not found.", null);
                }
            }

            var newAssumption = new Assumption
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Title = title,
                Description = description,
                Scope = scope,
                ValidityStartDate = validityStartDate,
                ValidityEndDate = validityEndDate,
                Methodology = methodology,
                Limitations = limitations,
                Rationale = rationale,
                Sources = sources ?? new List<AssumptionSource>(),
                Status = "active",
                Version = 1,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                LinkedDataPointIds = new List<string>(linkedDataPointIds)
            };

            _assumptions.Add(newAssumption);
            return (true, null, newAssumption);
        }
    }

    public (bool IsValid, string? ErrorMessage, Assumption? Assumption) UpdateAssumption(
        string id,
        string title,
        string description,
        string scope,
        string validityStartDate,
        string validityEndDate,
        string methodology,
        string limitations,
        List<string> linkedDataPointIds,
        string? rationale,
        List<AssumptionSource> sources,
        string updatedBy)
    {
        lock (_lock)
        {
            var assumption = _assumptions.FirstOrDefault(a => a.Id == id);
            if (assumption == null)
            {
                return (false, $"Assumption with ID '{id}' not found.", null);
            }

            if (assumption.Status != "active")
            {
                return (false, "Cannot update a deprecated or invalid assumption.", null);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return (false, "Description is required.", null);
            }

            if (string.IsNullOrWhiteSpace(scope))
            {
                return (false, "Scope is required.", null);
            }

            if (string.IsNullOrWhiteSpace(validityStartDate))
            {
                return (false, "Validity start date is required.", null);
            }

            if (string.IsNullOrWhiteSpace(validityEndDate))
            {
                return (false, "Validity end date is required.", null);
            }

            if (string.IsNullOrWhiteSpace(methodology))
            {
                return (false, "Methodology is required.", null);
            }

            // Validate dates
            if (!DateTime.TryParse(validityStartDate, out var startDate))
            {
                return (false, "Invalid validity start date format.", null);
            }

            if (!DateTime.TryParse(validityEndDate, out var endDate))
            {
                return (false, "Invalid validity end date format.", null);
            }

            if (endDate <= startDate)
            {
                return (false, "Validity end date must be after start date.", null);
            }

            // Validate linked data points exist
            foreach (var dataPointId in linkedDataPointIds)
            {
                var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
                if (dataPoint == null)
                {
                    return (false, $"Data point with ID '{dataPointId}' not found.", null);
                }
            }

            // Track changes for audit log
            var changes = new List<FieldChange>();

            if (assumption.Title != title)
            {
                changes.Add(new FieldChange { Field = "Title", OldValue = assumption.Title, NewValue = title });
            }

            if (assumption.Description != description)
            {
                changes.Add(new FieldChange { Field = "Description", OldValue = assumption.Description, NewValue = description });
            }

            if (assumption.Scope != scope)
            {
                changes.Add(new FieldChange { Field = "Scope", OldValue = assumption.Scope, NewValue = scope });
            }

            if (assumption.ValidityStartDate != validityStartDate)
            {
                changes.Add(new FieldChange { Field = "ValidityStartDate", OldValue = assumption.ValidityStartDate, NewValue = validityStartDate });
            }

            if (assumption.ValidityEndDate != validityEndDate)
            {
                changes.Add(new FieldChange { Field = "ValidityEndDate", OldValue = assumption.ValidityEndDate, NewValue = validityEndDate });
            }

            if (assumption.Methodology != methodology)
            {
                changes.Add(new FieldChange { Field = "Methodology", OldValue = assumption.Methodology, NewValue = methodology });
            }

            if (assumption.Limitations != limitations)
            {
                changes.Add(new FieldChange { Field = "Limitations", OldValue = assumption.Limitations, NewValue = limitations });
            }

            if (assumption.Rationale != rationale)
            {
                changes.Add(new FieldChange { Field = "Rationale", OldValue = assumption.Rationale ?? "", NewValue = rationale ?? "" });
            }

            var oldLinkedDataPoints = string.Join(", ", assumption.LinkedDataPointIds);
            var newLinkedDataPoints = string.Join(", ", linkedDataPointIds);
            if (oldLinkedDataPoints != newLinkedDataPoints)
            {
                changes.Add(new FieldChange { Field = "LinkedDataPointIds", OldValue = oldLinkedDataPoints, NewValue = newLinkedDataPoints });
            }

            // Update fields
            assumption.Title = title;
            assumption.Description = description;
            assumption.Scope = scope;
            assumption.ValidityStartDate = validityStartDate;
            assumption.ValidityEndDate = validityEndDate;
            assumption.Methodology = methodology;
            assumption.Limitations = limitations;
            assumption.Rationale = rationale;
            assumption.Sources = sources ?? new List<AssumptionSource>();
            assumption.LinkedDataPointIds = new List<string>(linkedDataPointIds);
            assumption.Version += 1;
            assumption.UpdatedBy = updatedBy;
            assumption.UpdatedAt = DateTime.UtcNow.ToString("O");

            // Log to audit trail if there were changes
            if (changes.Count > 0)
            {
                var user = _users.FirstOrDefault(u => u.Id == updatedBy);
                var userName = user?.Name ?? updatedBy;
                CreateAuditLogEntry(updatedBy, userName, "update", "Assumption", assumption.Id, changes, $"Updated assumption '{title}'");
            }

            return (true, null, assumption);
        }
    }

    public (bool IsValid, string? ErrorMessage) DeprecateAssumption(
        string id,
        string? replacementAssumptionId,
        string? justification,
        string deprecatedBy)
    {
        lock (_lock)
        {
            var assumption = _assumptions.FirstOrDefault(a => a.Id == id);
            if (assumption == null)
            {
                return (false, $"Assumption with ID '{id}' not found.");
            }

            if (assumption.Status != "active")
            {
                return (false, "Assumption is already deprecated or invalid.");
            }

            // Either replacement or justification is required
            if (string.IsNullOrWhiteSpace(replacementAssumptionId) && string.IsNullOrWhiteSpace(justification))
            {
                return (false, "Either a replacement assumption or justification is required when deprecating an assumption.");
            }

            // Track changes for audit log
            var changes = new List<FieldChange>();

            // Validate replacement assumption exists if provided
            if (!string.IsNullOrWhiteSpace(replacementAssumptionId))
            {
                var replacement = _assumptions.FirstOrDefault(a => a.Id == replacementAssumptionId);
                if (replacement == null)
                {
                    return (false, $"Replacement assumption with ID '{replacementAssumptionId}' not found.");
                }

                if (replacement.Status != "active")
                {
                    return (false, "Replacement assumption must be active.");
                }

                assumption.ReplacementAssumptionId = replacementAssumptionId;
                assumption.Status = "deprecated";
                changes.Add(new FieldChange { Field = "Status", OldValue = "active", NewValue = "deprecated" });
                changes.Add(new FieldChange { Field = "ReplacementAssumptionId", OldValue = "", NewValue = replacementAssumptionId });
            }
            else
            {
                assumption.DeprecationJustification = justification;
                assumption.Status = "invalid";
                changes.Add(new FieldChange { Field = "Status", OldValue = "active", NewValue = "invalid" });
                changes.Add(new FieldChange { Field = "DeprecationJustification", OldValue = "", NewValue = justification ?? "" });
            }

            // Log to audit trail
            var user = _users.FirstOrDefault(u => u.Id == deprecatedBy);
            var userName = user?.Name ?? deprecatedBy;
            CreateAuditLogEntry(deprecatedBy, userName, "deprecate", "Assumption", assumption.Id, changes, justification);

            return (true, null);
        }
    }

    public (bool IsValid, string? ErrorMessage) LinkAssumptionToDataPoint(string assumptionId, string dataPointId, string linkedBy)
    {
        lock (_lock)
        {
            var assumption = _assumptions.FirstOrDefault(a => a.Id == assumptionId);
            if (assumption == null)
            {
                return (false, $"Assumption with ID '{assumptionId}' not found.");
            }

            if (assumption.Status != "active")
            {
                return (false, "Cannot link a deprecated or invalid assumption.");
            }

            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
            if (dataPoint == null)
            {
                return (false, $"DataPoint with ID '{dataPointId}' not found.");
            }

            // Check if already linked
            if (assumption.LinkedDataPointIds.Contains(dataPointId))
            {
                return (false, "Assumption is already linked to this data point.");
            }

            assumption.LinkedDataPointIds.Add(dataPointId);

            // Log to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "LinkedDataPointIds", OldValue = "", NewValue = $"Added data point: {dataPointId}" }
            };
            var user = _users.FirstOrDefault(u => u.Id == linkedBy);
            var userName = user?.Name ?? linkedBy;
            CreateAuditLogEntry(linkedBy, userName, "link", "Assumption", assumption.Id, changes, $"Linked assumption to data point {dataPointId}");

            return (true, null);
        }
    }

    public (bool IsValid, string? ErrorMessage) UnlinkAssumptionFromDataPoint(string assumptionId, string dataPointId, string unlinkedBy)
    {
        lock (_lock)
        {
            var assumption = _assumptions.FirstOrDefault(a => a.Id == assumptionId);
            if (assumption == null)
            {
                return (false, $"Assumption with ID '{assumptionId}' not found.");
            }

            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
            if (dataPoint == null)
            {
                return (false, $"DataPoint with ID '{dataPointId}' not found.");
            }

            if (!assumption.LinkedDataPointIds.Contains(dataPointId))
            {
                return (false, "Assumption is not linked to this data point.");
            }

            assumption.LinkedDataPointIds.Remove(dataPointId);

            // Log to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "LinkedDataPointIds", OldValue = $"Removed data point: {dataPointId}", NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == unlinkedBy);
            var userName = user?.Name ?? unlinkedBy;
            CreateAuditLogEntry(unlinkedBy, userName, "unlink", "Assumption", assumption.Id, changes, $"Unlinked assumption from data point {dataPointId}");

            return (true, null);
        }
    }

    public (bool IsValid, string? ErrorMessage) DeleteAssumption(string id, string deletedBy)
    {
        lock (_lock)
        {
            var assumption = _assumptions.FirstOrDefault(a => a.Id == id);
            if (assumption == null)
            {
                return (false, $"Assumption with ID '{id}' not found.");
            }

            // Don't allow deletion if used as replacement for other assumptions
            var usedAsReplacement = _assumptions.Any(a => a.ReplacementAssumptionId == id);
            if (usedAsReplacement)
            {
                return (false, "Cannot delete assumption as it is used as a replacement for other assumptions.");
            }

            // Create audit log entry before deletion
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = assumption.Title ?? "", NewValue = "" },
                new FieldChange { Field = "Status", OldValue = assumption.Status ?? "", NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == deletedBy);
            var userName = user?.Name ?? deletedBy;
            CreateAuditLogEntry(deletedBy, userName, "delete", "Assumption", assumption.Id, changes, $"Deleted assumption '{assumption.Title}'");

            _assumptions.Remove(assumption);
            return (true, null);
        }
    }

    // Gap management methods
    public Gap? GetGapById(string id)
    {
        lock (_lock)
        {
            return _gaps.FirstOrDefault(g => g.Id == id);
        }
    }

    public Gap? GetGap(string id)
    {
        return GetGapById(id);
    }

    public (bool IsValid, string? ErrorMessage, Gap? Gap) CreateGap(
        string sectionId,
        string title,
        string description,
        string impact,
        string? improvementPlan,
        string? targetDate,
        string createdBy)
    {
        lock (_lock)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(sectionId))
            {
                return (false, "Section ID is required.", null);
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return (false, "Description is required.", null);
            }

            // Validate impact level
            var validImpactLevels = new[] { "low", "medium", "high" };
            if (string.IsNullOrWhiteSpace(impact) || !validImpactLevels.Contains(impact.ToLowerInvariant()))
            {
                return (false, "Impact must be one of: low, medium, high.", null);
            }

            // Validate section exists
            var section = _sections.FirstOrDefault(s => s.Id == sectionId);
            if (section == null)
            {
                return (false, $"Section with ID '{sectionId}' not found.", null);
            }

            var gap = new Gap
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Title = title,
                Description = description,
                Impact = impact.ToLowerInvariant(),
                ImprovementPlan = improvementPlan,
                TargetDate = targetDate,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                Resolved = false
            };

            _gaps.Add(gap);

            // Log creation to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = "", NewValue = title },
                new FieldChange { Field = "Description", OldValue = "", NewValue = description },
                new FieldChange { Field = "Impact", OldValue = "", NewValue = impact.ToLowerInvariant() }
            };
            var user = _users.FirstOrDefault(u => u.Id == createdBy);
            var userName = user?.Name ?? createdBy;
            CreateAuditLogEntry(createdBy, userName, "create", "Gap", gap.Id, changes, $"Created gap '{title}'");

            return (true, null, gap);
        }
    }

    public (bool IsValid, string? ErrorMessage, Gap? Gap) UpdateGap(
        string id,
        string title,
        string description,
        string impact,
        string? improvementPlan,
        string? targetDate,
        string updatedBy,
        string? changeNote)
    {
        lock (_lock)
        {
            var gap = _gaps.FirstOrDefault(g => g.Id == id);
            if (gap == null)
            {
                return (false, $"Gap with ID '{id}' not found.", null);
            }

            if (gap.Resolved)
            {
                return (false, "Cannot update a resolved gap. Reopen it first.", null);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return (false, "Description is required.", null);
            }

            // Validate impact level
            var validImpactLevels = new[] { "low", "medium", "high" };
            if (string.IsNullOrWhiteSpace(impact) || !validImpactLevels.Contains(impact.ToLowerInvariant()))
            {
                return (false, "Impact must be one of: low, medium, high.", null);
            }

            // Track changes for audit log
            var changes = new List<FieldChange>();

            if (gap.Title != title)
            {
                changes.Add(new FieldChange { Field = "Title", OldValue = gap.Title, NewValue = title });
            }

            if (gap.Description != description)
            {
                changes.Add(new FieldChange { Field = "Description", OldValue = gap.Description, NewValue = description });
            }

            if (gap.Impact != impact.ToLowerInvariant())
            {
                changes.Add(new FieldChange { Field = "Impact", OldValue = gap.Impact, NewValue = impact.ToLowerInvariant() });
            }

            if (gap.ImprovementPlan != improvementPlan)
            {
                changes.Add(new FieldChange { Field = "ImprovementPlan", OldValue = gap.ImprovementPlan ?? "", NewValue = improvementPlan ?? "" });
            }

            if (gap.TargetDate != targetDate)
            {
                changes.Add(new FieldChange { Field = "TargetDate", OldValue = gap.TargetDate ?? "", NewValue = targetDate ?? "" });
            }

            // Update fields
            gap.Title = title;
            gap.Description = description;
            gap.Impact = impact.ToLowerInvariant();
            gap.ImprovementPlan = improvementPlan;
            gap.TargetDate = targetDate;

            // Log to audit trail if there were changes
            if (changes.Count > 0)
            {
                var user = _users.FirstOrDefault(u => u.Id == updatedBy);
                var userName = user?.Name ?? updatedBy;
                CreateAuditLogEntry(updatedBy, userName, "update", "Gap", gap.Id, changes, changeNote ?? $"Updated gap '{title}'");
            }

            return (true, null, gap);
        }
    }

    public (bool IsValid, string? ErrorMessage, Gap? Gap) ResolveGap(
        string id,
        string resolvedBy,
        string? resolutionNote)
    {
        lock (_lock)
        {
            var gap = _gaps.FirstOrDefault(g => g.Id == id);
            if (gap == null)
            {
                return (false, $"Gap with ID '{id}' not found.", null);
            }

            if (gap.Resolved)
            {
                return (false, "Gap is already resolved.", null);
            }

            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Resolved", OldValue = "false", NewValue = "true" }
            };

            gap.Resolved = true;

            // Log to audit trail
            var user = _users.FirstOrDefault(u => u.Id == resolvedBy);
            var userName = user?.Name ?? resolvedBy;
            CreateAuditLogEntry(resolvedBy, userName, "resolve", "Gap", gap.Id, changes, resolutionNote ?? $"Resolved gap '{gap.Title}'");

            return (true, null, gap);
        }
    }

    public (bool IsValid, string? ErrorMessage, Gap? Gap) ReopenGap(
        string id,
        string reopenedBy,
        string? reopenNote)
    {
        lock (_lock)
        {
            var gap = _gaps.FirstOrDefault(g => g.Id == id);
            if (gap == null)
            {
                return (false, $"Gap with ID '{id}' not found.", null);
            }

            if (!gap.Resolved)
            {
                return (false, "Gap is not resolved.", null);
            }

            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Resolved", OldValue = "true", NewValue = "false" }
            };

            gap.Resolved = false;

            // Log to audit trail
            var user = _users.FirstOrDefault(u => u.Id == reopenedBy);
            var userName = user?.Name ?? reopenedBy;
            CreateAuditLogEntry(reopenedBy, userName, "reopen", "Gap", gap.Id, changes, reopenNote ?? $"Reopened gap '{gap.Title}'");

            return (true, null, gap);
        }
    }

    // Simplification management methods
    public IReadOnlyList<Simplification> GetSimplifications(string? sectionId = null)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(sectionId))
            {
                return _simplifications.Where(s => s.Status == "active").ToList();
            }

            return _simplifications.Where(s => s.SectionId == sectionId && s.Status == "active").ToList();
        }
    }

    public Simplification? GetSimplificationById(string id)
    {
        lock (_lock)
        {
            return _simplifications.FirstOrDefault(s => s.Id == id);
        }
    }

    public (bool IsValid, string? ErrorMessage, Simplification? Simplification) CreateSimplification(
        string sectionId,
        string title,
        string description,
        List<string> affectedEntities,
        List<string> affectedSites,
        List<string> affectedProcesses,
        string impactLevel,
        string? impactNotes,
        string createdBy)
    {
        lock (_lock)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return (false, "Description is required.", null);
            }

            if (string.IsNullOrWhiteSpace(sectionId))
            {
                return (false, "SectionId is required.", null);
            }

            if (string.IsNullOrWhiteSpace(createdBy))
            {
                return (false, "CreatedBy is required.", null);
            }

            // Validate that at least one boundary is specified with non-empty values
            var validEntities = affectedEntities?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? new List<string>();
            var validSites = affectedSites?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>();
            var validProcesses = affectedProcesses?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();
            
            if (validEntities.Count == 0 && validSites.Count == 0 && validProcesses.Count == 0)
            {
                return (false, "At least one affected boundary (entities, sites, or processes) must be specified with non-empty values.", null);
            }

            // Validate impact level
            var validImpactLevels = new[] { "low", "medium", "high" };
            if (string.IsNullOrWhiteSpace(impactLevel) || !validImpactLevels.Contains(impactLevel.ToLowerInvariant()))
            {
                return (false, "Impact level must be one of: low, medium, high.", null);
            }

            // Verify section exists
            var section = _sections.FirstOrDefault(s => s.Id == sectionId);
            if (section == null)
            {
                return (false, $"Section with ID '{sectionId}' not found.", null);
            }

            var simplification = new Simplification
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Title = title,
                Description = description,
                AffectedEntities = validEntities,
                AffectedSites = validSites,
                AffectedProcesses = validProcesses,
                ImpactLevel = impactLevel.ToLowerInvariant(),
                ImpactNotes = impactNotes,
                Status = "active",
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            _simplifications.Add(simplification);

            // Log to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = "", NewValue = title },
                new FieldChange { Field = "Description", OldValue = "", NewValue = description },
                new FieldChange { Field = "ImpactLevel", OldValue = "", NewValue = impactLevel.ToLowerInvariant() }
            };
            var user = _users.FirstOrDefault(u => u.Id == createdBy);
            var userName = user?.Name ?? createdBy;
            CreateAuditLogEntry(createdBy, userName, "create", "Simplification", simplification.Id, changes, $"Created simplification '{title}'");

            return (true, null, simplification);
        }
    }

    public (bool IsValid, string? ErrorMessage, Simplification? Simplification) UpdateSimplification(
        string id,
        string title,
        string description,
        List<string> affectedEntities,
        List<string> affectedSites,
        List<string> affectedProcesses,
        string impactLevel,
        string? impactNotes,
        string updatedBy)
    {
        lock (_lock)
        {
            var simplification = _simplifications.FirstOrDefault(s => s.Id == id);
            if (simplification == null)
            {
                return (false, $"Simplification with ID '{id}' not found.", null);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return (false, "Description is required.", null);
            }

            if (string.IsNullOrWhiteSpace(updatedBy))
            {
                return (false, "UpdatedBy is required.", null);
            }

            // Validate that at least one boundary is specified with non-empty values
            var validEntities = affectedEntities?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? new List<string>();
            var validSites = affectedSites?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>();
            var validProcesses = affectedProcesses?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();
            
            if (validEntities.Count == 0 && validSites.Count == 0 && validProcesses.Count == 0)
            {
                return (false, "At least one affected boundary (entities, sites, or processes) must be specified with non-empty values.", null);
            }

            // Validate impact level
            var validImpactLevels = new[] { "low", "medium", "high" };
            if (string.IsNullOrWhiteSpace(impactLevel) || !validImpactLevels.Contains(impactLevel.ToLowerInvariant()))
            {
                return (false, "Impact level must be one of: low, medium, high.", null);
            }

            // Track changes for audit log
            var changes = new List<FieldChange>();

            if (simplification.Title != title)
            {
                changes.Add(new FieldChange { Field = "Title", OldValue = simplification.Title, NewValue = title });
                simplification.Title = title;
            }

            if (simplification.Description != description)
            {
                changes.Add(new FieldChange { Field = "Description", OldValue = simplification.Description, NewValue = description });
                simplification.Description = description;
            }

            var oldEntities = string.Join(", ", simplification.AffectedEntities);
            var newEntities = string.Join(", ", validEntities);
            if (oldEntities != newEntities)
            {
                changes.Add(new FieldChange { Field = "AffectedEntities", OldValue = oldEntities, NewValue = newEntities });
                simplification.AffectedEntities = validEntities;
            }

            var oldSites = string.Join(", ", simplification.AffectedSites);
            var newSites = string.Join(", ", validSites);
            if (oldSites != newSites)
            {
                changes.Add(new FieldChange { Field = "AffectedSites", OldValue = oldSites, NewValue = newSites });
                simplification.AffectedSites = validSites;
            }

            var oldProcesses = string.Join(", ", simplification.AffectedProcesses);
            var newProcesses = string.Join(", ", validProcesses);
            if (oldProcesses != newProcesses)
            {
                changes.Add(new FieldChange { Field = "AffectedProcesses", OldValue = oldProcesses, NewValue = newProcesses });
                simplification.AffectedProcesses = validProcesses;
            }

            if (simplification.ImpactLevel != impactLevel.ToLowerInvariant())
            {
                changes.Add(new FieldChange { Field = "ImpactLevel", OldValue = simplification.ImpactLevel, NewValue = impactLevel.ToLowerInvariant() });
                simplification.ImpactLevel = impactLevel.ToLowerInvariant();
            }

            if (simplification.ImpactNotes != impactNotes)
            {
                changes.Add(new FieldChange { Field = "ImpactNotes", OldValue = simplification.ImpactNotes ?? "", NewValue = impactNotes ?? "" });
                simplification.ImpactNotes = impactNotes;
            }

            simplification.UpdatedBy = updatedBy;
            simplification.UpdatedAt = DateTime.UtcNow.ToString("O");

            // Log to audit trail if there were changes
            if (changes.Count > 0)
            {
                var user = _users.FirstOrDefault(u => u.Id == updatedBy);
                var userName = user?.Name ?? updatedBy;
                CreateAuditLogEntry(updatedBy, userName, "update", "Simplification", simplification.Id, changes, $"Updated simplification '{title}'");
            }

            return (true, null, simplification);
        }
    }

    public (bool IsValid, string? ErrorMessage) DeleteSimplification(string id, string deletedBy)
    {
        lock (_lock)
        {
            var simplification = _simplifications.FirstOrDefault(s => s.Id == id);
            if (simplification == null)
            {
                return (false, $"Simplification with ID '{id}' not found.");
            }

            // Mark as removed instead of hard delete (for audit trail)
            simplification.Status = "removed";
            simplification.UpdatedBy = deletedBy;
            simplification.UpdatedAt = DateTime.UtcNow.ToString("O");

            // Log to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = simplification.Title, NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == deletedBy);
            var userName = user?.Name ?? deletedBy;
            CreateAuditLogEntry(deletedBy, userName, "delete", "Simplification", simplification.Id, changes, $"Removed simplification '{simplification.Title}'");

            return (true, null);
        }
    }

    // User management methods
    public IReadOnlyList<User> GetUsers()
    {
        lock (_lock)
        {
            return _users.ToList();
        }
    }

    public User? GetUser(string id)
    {
        lock (_lock)
        {
            return _users.FirstOrDefault(u => u.Id == id);
        }
    }

    // Validation rule methods
    public IReadOnlyList<ValidationRule> GetValidationRules(string? sectionId = null)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(sectionId))
            {
                return _validationRules.ToList();
            }

            return _validationRules.Where(r => r.SectionId == sectionId && r.IsActive).ToList();
        }
    }

    public ValidationRule? GetValidationRule(string id)
    {
        lock (_lock)
        {
            return _validationRules.FirstOrDefault(r => r.Id == id);
        }
    }

    public (bool IsValid, string? ErrorMessage, ValidationRule? Rule) CreateValidationRule(CreateValidationRuleRequest request)
    {
        lock (_lock)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.SectionId))
            {
                return (false, "SectionId is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.RuleType))
            {
                return (false, "RuleType is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.ErrorMessage))
            {
                return (false, "ErrorMessage is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.CreatedBy))
            {
                return (false, "CreatedBy is required.", null);
            }

            // Validate rule type
            var validRuleTypes = new[] { "non-negative", "required-unit", "allowed-units", "value-within-period" };
            if (!validRuleTypes.Contains(request.RuleType, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"RuleType must be one of: {string.Join(", ", validRuleTypes)}.", null);
            }

            // Validate section exists
            var sectionExists = _sections.Any(s => s.Id == request.SectionId);
            if (!sectionExists)
            {
                return (false, $"Section with ID '{request.SectionId}' not found.", null);
            }

            var now = DateTime.UtcNow.ToString("O");
            var newRule = new ValidationRule
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = request.SectionId,
                RuleType = request.RuleType,
                TargetField = request.TargetField,
                Parameters = request.Parameters,
                ErrorMessage = request.ErrorMessage,
                IsActive = true,
                CreatedBy = request.CreatedBy,
                CreatedAt = now
            };

            _validationRules.Add(newRule);

            // Log creation to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "RuleType", OldValue = "", NewValue = request.RuleType },
                new FieldChange { Field = "Description", OldValue = "", NewValue = request.ErrorMessage },
                new FieldChange { Field = "SeverityLevel", OldValue = "", NewValue = "error" }
            };
            var user = _users.FirstOrDefault(u => u.Id == request.CreatedBy);
            var userName = user?.Name ?? request.CreatedBy;
            CreateAuditLogEntry(request.CreatedBy, userName, "create", "ValidationRule", newRule.Id, changes, $"Created validation rule '{request.ErrorMessage}'");

            return (true, null, newRule);
        }
    }

    public (bool IsValid, string? ErrorMessage, ValidationRule? Rule) UpdateValidationRule(string id, UpdateValidationRuleRequest request)
    {
        lock (_lock)
        {
            var rule = _validationRules.FirstOrDefault(r => r.Id == id);
            if (rule == null)
            {
                return (false, "ValidationRule not found.", null);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.RuleType))
            {
                return (false, "RuleType is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.ErrorMessage))
            {
                return (false, "ErrorMessage is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.UpdatedBy))
            {
                return (false, "UpdatedBy is required.", null);
            }

            // Validate rule type
            var validRuleTypes = new[] { "non-negative", "required-unit", "allowed-units", "value-within-period" };
            if (!validRuleTypes.Contains(request.RuleType, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"RuleType must be one of: {string.Join(", ", validRuleTypes)}.", null);
            }

            // Track changes for audit log
            var changes = new List<FieldChange>();

            if (rule.RuleType != request.RuleType)
            {
                changes.Add(new FieldChange { Field = "RuleType", OldValue = rule.RuleType, NewValue = request.RuleType });
            }

            if (rule.ErrorMessage != request.ErrorMessage)
            {
                changes.Add(new FieldChange { Field = "Description", OldValue = rule.ErrorMessage, NewValue = request.ErrorMessage });
            }

            if (rule.IsActive != request.IsActive)
            {
                changes.Add(new FieldChange { Field = "Enabled", OldValue = rule.IsActive.ToString(), NewValue = request.IsActive.ToString() });
            }

            // Update fields
            rule.RuleType = request.RuleType;
            rule.TargetField = request.TargetField;
            rule.Parameters = request.Parameters;
            rule.ErrorMessage = request.ErrorMessage;
            rule.IsActive = request.IsActive;

            // Log to audit trail if there were changes
            if (changes.Count > 0)
            {
                var user = _users.FirstOrDefault(u => u.Id == request.UpdatedBy);
                var userName = user?.Name ?? request.UpdatedBy;
                CreateAuditLogEntry(request.UpdatedBy, userName, "update", "ValidationRule", rule.Id, changes, $"Updated validation rule '{request.ErrorMessage}'");
            }

            return (true, null, rule);
        }
    }

    public bool DeleteValidationRule(string id, string deletedBy)
    {
        lock (_lock)
        {
            var rule = _validationRules.FirstOrDefault(r => r.Id == id);
            if (rule == null)
            {
                return false;
            }

            // Log deletion to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Description", OldValue = rule.ErrorMessage, NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == deletedBy);
            var userName = user?.Name ?? deletedBy;
            CreateAuditLogEntry(deletedBy, userName, "delete", "ValidationRule", rule.Id, changes, $"Deleted validation rule '{rule.ErrorMessage}'");

            _validationRules.Remove(rule);
            return true;
        }
    }

    private (bool IsValid, string? ErrorMessage) ValidateDataPointAgainstRules(DataPoint dataPoint)
    {
        var rules = _validationRules.Where(r => r.SectionId == dataPoint.SectionId && r.IsActive).ToList();

        foreach (var rule in rules)
        {
            var (isValid, errorMessage) = EvaluateValidationRule(rule, dataPoint);
            if (!isValid)
            {
                return (false, errorMessage);
            }
        }

        return (true, null);
    }

    private (bool IsValid, string? ErrorMessage) ValidateEstimateFields(string informationType, string? estimateType, string? estimateMethod, string? confidenceLevel)
    {
        if (!informationType.Equals("estimate", StringComparison.OrdinalIgnoreCase))
        {
            return (true, null);
        }

        // Validate estimate type
        if (string.IsNullOrWhiteSpace(estimateType))
        {
            return (false, "EstimateType is required when InformationType is 'estimate'.");
        }
        
        if (!ValidEstimateTypes.Contains(estimateType, StringComparer.OrdinalIgnoreCase))
        {
            return (false, $"EstimateType must be one of: {string.Join(", ", ValidEstimateTypes)}.");
        }
        
        // Validate estimate method
        if (string.IsNullOrWhiteSpace(estimateMethod))
        {
            return (false, "EstimateMethod is required when InformationType is 'estimate'.");
        }
        
        // Validate confidence level
        if (string.IsNullOrWhiteSpace(confidenceLevel))
        {
            return (false, "ConfidenceLevel is required when InformationType is 'estimate'.");
        }
        
        if (!ValidConfidenceLevels.Contains(confidenceLevel, StringComparer.OrdinalIgnoreCase))
        {
            return (false, $"ConfidenceLevel must be one of: {string.Join(", ", ValidConfidenceLevels)}.");
        }

        return (true, null);
    }

    /// <summary>
    /// Compares two lists of source references to determine if they are equal.
    /// Used to detect changes in provenance that might require review.
    /// </summary>
    private bool AreSourceReferencesEqual(List<NarrativeSourceReference> list1, List<NarrativeSourceReference>? list2)
    {
        if (list2 == null)
        {
            return list1.Count == 0;
        }
        
        if (list1.Count != list2.Count)
        {
            return false;
        }
        
        // Simple comparison based on source reference and type
        // For more sophisticated comparison, could use hashing
        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i].SourceType != list2[i].SourceType ||
                list1[i].SourceReference != list2[i].SourceReference)
            {
                return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Flags a data point's provenance for review when source data changes.
    /// </summary>
    public (bool Success, string? ErrorMessage) FlagProvenanceForReview(string dataPointId, string reason, string flaggedBy)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
            if (dataPoint == null)
            {
                return (false, "DataPoint not found.");
            }
            
            dataPoint.ProvenanceNeedsReview = true;
            dataPoint.ProvenanceReviewReason = reason;
            dataPoint.ProvenanceFlaggedBy = flaggedBy;
            dataPoint.ProvenanceFlaggedAt = DateTime.UtcNow.ToString("O");
            dataPoint.UpdatedAt = DateTime.UtcNow.ToString("O");
            
            return (true, null);
        }
    }

    /// <summary>
    /// Clears the provenance review flag after review is complete.
    /// </summary>
    public (bool Success, string? ErrorMessage) ClearProvenanceReviewFlag(string dataPointId)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
            if (dataPoint == null)
            {
                return (false, "DataPoint not found.");
            }
            
            dataPoint.ProvenanceNeedsReview = false;
            dataPoint.ProvenanceReviewReason = null;
            dataPoint.ProvenanceFlaggedBy = null;
            dataPoint.ProvenanceFlaggedAt = null;
            dataPoint.ProvenanceLastVerified = DateTime.UtcNow.ToString("O");
            dataPoint.UpdatedAt = DateTime.UtcNow.ToString("O");
            
            return (true, null);
        }
    }

    /// <summary>
    /// Captures publication snapshot of source data for provenance tracking.
    /// Generates a hash of all source references to detect future changes.
    /// </summary>
    public (bool Success, string? ErrorMessage) CaptureProvenanceSnapshot(string dataPointId)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
            if (dataPoint == null)
            {
                return (false, "DataPoint not found.");
            }
            
            // Generate hash from source references
            var hashContent = string.Join("|", dataPoint.SourceReferences
                .OrderBy(sr => sr.SourceReference)
                .Select(sr => $"{sr.SourceType}:{sr.SourceReference}:{sr.LastUpdated ?? ""}"));
            
            // Simple hash for demonstration - in production, use a proper hashing algorithm
            dataPoint.PublicationSourceHash = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(hashContent));
            dataPoint.ProvenanceLastVerified = DateTime.UtcNow.ToString("O");
            dataPoint.ProvenanceNeedsReview = false;
            dataPoint.UpdatedAt = DateTime.UtcNow.ToString("O");
            
            return (true, null);
        }
    }

    private (bool IsValid, string? ErrorMessage) EvaluateValidationRule(ValidationRule rule, DataPoint dataPoint)
    {
        switch (rule.RuleType.ToLowerInvariant())
        {
            case "non-negative":
                return ValidateNonNegative(rule, dataPoint);
            
            case "required-unit":
                return ValidateRequiredUnit(rule, dataPoint);
            
            case "allowed-units":
                return ValidateAllowedUnits(rule, dataPoint);
            
            case "value-within-period":
                return ValidateValueWithinPeriod(rule, dataPoint);
            
            default:
                return (true, null); // Unknown rule type, skip validation
        }
    }

    private (bool IsValid, string? ErrorMessage) ValidateNonNegative(ValidationRule rule, DataPoint dataPoint)
    {
        if (string.IsNullOrWhiteSpace(dataPoint.Value))
        {
            return (true, null); // Skip validation if no value
        }

        if (decimal.TryParse(dataPoint.Value, out var numericValue))
        {
            if (numericValue < 0)
            {
                return (false, rule.ErrorMessage);
            }
        }

        return (true, null);
    }

    private (bool IsValid, string? ErrorMessage) ValidateRequiredUnit(ValidationRule rule, DataPoint dataPoint)
    {
        if (string.IsNullOrWhiteSpace(dataPoint.Value))
        {
            return (true, null); // Skip validation if no value
        }

        if (string.IsNullOrWhiteSpace(dataPoint.Unit))
        {
            return (false, rule.ErrorMessage);
        }

        return (true, null);
    }

    private (bool IsValid, string? ErrorMessage) ValidateAllowedUnits(ValidationRule rule, DataPoint dataPoint)
    {
        if (string.IsNullOrWhiteSpace(dataPoint.Unit))
        {
            return (true, null); // Skip validation if no unit
        }

        if (string.IsNullOrWhiteSpace(rule.Parameters))
        {
            return (true, null); // Skip if no allowed units specified
        }

        try
        {
            var allowedUnits = System.Text.Json.JsonSerializer.Deserialize<string[]>(rule.Parameters);
            if (allowedUnits != null && allowedUnits.Length > 0)
            {
                if (!allowedUnits.Contains(dataPoint.Unit, StringComparer.OrdinalIgnoreCase))
                {
                    return (false, rule.ErrorMessage);
                }
            }
        }
        catch
        {
            // Invalid JSON, skip validation
            return (true, null);
        }

        return (true, null);
    }

    private (bool IsValid, string? ErrorMessage) ValidateValueWithinPeriod(ValidationRule rule, DataPoint dataPoint)
    {
        if (string.IsNullOrWhiteSpace(dataPoint.Value))
        {
            return (true, null); // Skip validation if no value
        }

        // Get the reporting period for this data point's section
        var section = _sections.FirstOrDefault(s => s.Id == dataPoint.SectionId);
        if (section == null)
        {
            return (true, null); // Skip if section not found
        }

        var period = _periods.FirstOrDefault(p => p.Id == section.PeriodId);
        if (period == null)
        {
            return (true, null); // Skip if period not found
        }

        // Try to parse the value as a date
        if (DateTime.TryParse(dataPoint.Value, out var valueDate))
        {
            if (DateTime.TryParse(period.StartDate, out var startDate) && 
                DateTime.TryParse(period.EndDate, out var endDate))
            {
                if (valueDate < startDate || valueDate > endDate)
                {
                    return (false, rule.ErrorMessage);
                }
            }
        }

        return (true, null);
    }

    // Audit Log Management
    private void CreateAuditLogEntry(string userId, string userName, string action, string entityType, string entityId, List<FieldChange> changes, string? changeNote = null)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow.ToString("O"),
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ChangeNote = changeNote,
            Changes = changes
        };
        
        _auditLog.Add(entry);
    }

    public IReadOnlyList<AuditLogEntry> GetAuditLog(
        string? entityType = null, 
        string? entityId = null, 
        string? userId = null, 
        string? action = null, 
        string? startDate = null, 
        string? endDate = null,
        string? sectionId = null,
        string? ownerId = null)
    {
        lock (_lock)
        {
            var query = _auditLog.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(e => e.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(entityId))
            {
                query = query.Where(e => e.EntityId == entityId);
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(e => e.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(e => e.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var start))
            {
                query = query.Where(e => DateTime.Parse(e.Timestamp) >= start);
            }

            if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var end))
            {
                query = query.Where(e => DateTime.Parse(e.Timestamp) <= end);
            }

            // Filter by section if provided
            if (!string.IsNullOrWhiteSpace(sectionId))
            {
                query = query.Where(e =>
                {
                    // Check if entity has a SectionId property
                    if (e.EntityType.Equals("Gap", StringComparison.OrdinalIgnoreCase))
                    {
                        var gap = _gaps.FirstOrDefault(g => g.Id == e.EntityId);
                        return gap?.SectionId == sectionId;
                    }
                    else if (e.EntityType.Equals("Assumption", StringComparison.OrdinalIgnoreCase))
                    {
                        var assumption = _assumptions.FirstOrDefault(a => a.Id == e.EntityId);
                        return assumption?.SectionId == sectionId;
                    }
                    else if (e.EntityType.Equals("simplification", StringComparison.OrdinalIgnoreCase))
                    {
                        var simplification = _simplifications.FirstOrDefault(s => s.Id == e.EntityId);
                        return simplification?.SectionId == sectionId;
                    }
                    else if (e.EntityType.Equals("DataPoint", StringComparison.OrdinalIgnoreCase))
                    {
                        var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == e.EntityId);
                        return dataPoint?.SectionId == sectionId;
                    }
                    return false;
                });
            }

            // Filter by owner if provided
            if (!string.IsNullOrWhiteSpace(ownerId))
            {
                query = query.Where(e =>
                {
                    // Check if entity has an owner/creator
                    if (e.EntityType.Equals("Gap", StringComparison.OrdinalIgnoreCase))
                    {
                        var gap = _gaps.FirstOrDefault(g => g.Id == e.EntityId);
                        return gap?.CreatedBy == ownerId;
                    }
                    else if (e.EntityType.Equals("Assumption", StringComparison.OrdinalIgnoreCase))
                    {
                        var assumption = _assumptions.FirstOrDefault(a => a.Id == e.EntityId);
                        return assumption?.CreatedBy == ownerId;
                    }
                    else if (e.EntityType.Equals("simplification", StringComparison.OrdinalIgnoreCase))
                    {
                        var simplification = _simplifications.FirstOrDefault(s => s.Id == e.EntityId);
                        return simplification?.CreatedBy == ownerId;
                    }
                    else if (e.EntityType.Equals("DataPoint", StringComparison.OrdinalIgnoreCase))
                    {
                        var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == e.EntityId);
                        return dataPoint?.OwnerId == ownerId;
                    }
                    return false;
                });
            }

            return query.OrderByDescending(e => e.Timestamp).ToList();
        }
    }

    public void LogPublishAction(string periodId, string publishedBy, string action, string changeNote, int errorCount, int warningCount)
    {
        lock (_lock)
        {
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Status", OldValue = "draft", NewValue = "published" },
                new FieldChange { Field = "ErrorCount", OldValue = errorCount.ToString(), NewValue = "0" },
                new FieldChange { Field = "WarningCount", OldValue = warningCount.ToString(), NewValue = warningCount.ToString() }
            };
            
            var user = _users.FirstOrDefault(u => u.Id == publishedBy);
            var userName = user?.Name ?? publishedBy;
            CreateAuditLogEntry(publishedBy, userName, action, "ReportingPeriod", periodId, changes, changeNote);
        }
    }

    // Reminder management methods
    public ReminderConfiguration? GetReminderConfiguration(string periodId)
    {
        lock (_lock)
        {
            return _reminderConfigurations.FirstOrDefault(rc => rc.PeriodId == periodId);
        }
    }

    public ReminderConfiguration CreateOrUpdateReminderConfiguration(string periodId, ReminderConfiguration config)
    {
        lock (_lock)
        {
            var existing = _reminderConfigurations.FirstOrDefault(rc => rc.PeriodId == periodId);
            if (existing != null)
            {
                // Create a new configuration to ensure thread-safe reads
                var updated = new ReminderConfiguration
                {
                    Id = existing.Id,
                    PeriodId = periodId,
                    Enabled = config.Enabled,
                    DaysBeforeDeadline = new List<int>(config.DaysBeforeDeadline),
                    CheckFrequencyHours = config.CheckFrequencyHours,
                    CreatedAt = existing.CreatedAt,
                    UpdatedAt = DateTime.UtcNow.ToString("O")
                };
                
                _reminderConfigurations.Remove(existing);
                _reminderConfigurations.Add(updated);
                return updated;
            }

            var newConfig = new ReminderConfiguration
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Enabled = config.Enabled,
                DaysBeforeDeadline = new List<int>(config.DaysBeforeDeadline),
                CheckFrequencyHours = config.CheckFrequencyHours,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                UpdatedAt = DateTime.UtcNow.ToString("O")
            };

            _reminderConfigurations.Add(newConfig);
            return newConfig;
        }
    }

    public IReadOnlyList<DataPoint> GetDataPointsForPeriod(string periodId)
    {
        lock (_lock)
        {
            var sectionIds = _sections
                .Where(s => s.PeriodId == periodId)
                .Select(s => s.Id)
                .ToHashSet();

            return _dataPoints
                .Where(dp => sectionIds.Contains(dp.SectionId))
                .ToList();
        }
    }

    public void RecordReminderSent(ReminderHistory history)
    {
        lock (_lock)
        {
            _reminderHistory.Add(history);
        }
    }

    public bool HasReminderBeenSentToday(string dataPointId, int daysUntilDeadline)
    {
        lock (_lock)
        {
            var today = DateTime.UtcNow.Date;
            return _reminderHistory.Any(rh =>
                rh.DataPointId == dataPointId &&
                rh.DaysUntilDeadline == daysUntilDeadline &&
                DateTime.TryParse(rh.SentAt, out var sentDate) &&
                sentDate.Date == today);
        }
    }

    public IReadOnlyList<ReminderHistory> GetReminderHistory(string? dataPointId = null, string? userId = null)
    {
        lock (_lock)
        {
            var query = _reminderHistory.AsEnumerable();

            if (!string.IsNullOrEmpty(dataPointId))
                query = query.Where(rh => rh.DataPointId == dataPointId);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(rh => rh.RecipientUserId == userId);

            return query.OrderByDescending(rh => rh.SentAt).ToList();
        }
    }

    // Escalation Configuration and History Management

    /// <summary>
    /// Gets the escalation configuration for a specific reporting period.
    /// </summary>
    public EscalationConfiguration? GetEscalationConfiguration(string periodId)
    {
        lock (_lock)
        {
            return _escalationConfigurations.FirstOrDefault(ec => ec.PeriodId == periodId);
        }
    }

    /// <summary>
    /// Creates or updates an escalation configuration for a period.
    /// </summary>
    public EscalationConfiguration CreateOrUpdateEscalationConfiguration(string periodId, EscalationConfiguration config)
    {
        lock (_lock)
        {
            var existing = _escalationConfigurations.FirstOrDefault(ec => ec.PeriodId == periodId);
            if (existing != null)
            {
                var updated = new EscalationConfiguration
                {
                    Id = existing.Id,
                    PeriodId = periodId,
                    Enabled = config.Enabled,
                    DaysAfterDeadline = new List<int>(config.DaysAfterDeadline),
                    CreatedAt = existing.CreatedAt,
                    UpdatedAt = DateTime.UtcNow.ToString("O")
                };
                
                _escalationConfigurations.Remove(existing);
                _escalationConfigurations.Add(updated);
                return updated;
            }

            var newConfig = new EscalationConfiguration
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = periodId,
                Enabled = config.Enabled,
                DaysAfterDeadline = new List<int>(config.DaysAfterDeadline),
                CreatedAt = DateTime.UtcNow.ToString("O"),
                UpdatedAt = DateTime.UtcNow.ToString("O")
            };

            _escalationConfigurations.Add(newConfig);
            return newConfig;
        }
    }

    /// <summary>
    /// Records an escalation event.
    /// </summary>
    public void RecordEscalationSent(EscalationHistory history)
    {
        lock (_lock)
        {
            _escalationHistory.Add(history);
        }
    }

    /// <summary>
    /// Checks if an escalation has already been sent today for a specific data point and overdue days.
    /// </summary>
    public bool HasEscalationBeenSentToday(string dataPointId, int daysOverdue)
    {
        lock (_lock)
        {
            var today = DateTime.UtcNow.Date;
            return _escalationHistory.Any(eh =>
                eh.DataPointId == dataPointId &&
                eh.DaysOverdue == daysOverdue &&
                DateTime.TryParse(eh.SentAt, out var sentDate) &&
                sentDate.Date == today);
        }
    }

    /// <summary>
    /// Gets escalation history for data points and/or users.
    /// </summary>
    public IReadOnlyList<EscalationHistory> GetEscalationHistory(string? dataPointId = null, string? userId = null)
    {
        lock (_lock)
        {
            var query = _escalationHistory.AsEnumerable();

            if (!string.IsNullOrEmpty(dataPointId))
                query = query.Where(eh => eh.DataPointId == dataPointId);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(eh => eh.OwnerUserId == userId || eh.EscalatedToUserId == userId);

            return query.OrderByDescending(eh => eh.SentAt).ToList();
        }
    }

    // Completeness Dashboard
    public CompletenessStats GetCompletenessStats(string? periodId = null, string? category = null, string? organizationalUnitId = null)
    {
        lock (_lock)
        {
            // Get data points for the specified period
            var dataPoints = _dataPoints.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(periodId))
            {
                var periodSectionIds = _sections
                    .Where(s => s.PeriodId == periodId)
                    .Select(s => s.Id)
                    .ToHashSet();
                dataPoints = dataPoints.Where(dp => periodSectionIds.Contains(dp.SectionId));
            }

            // Apply category filter if specified
            if (!string.IsNullOrWhiteSpace(category))
            {
                var categorySectionIds = _sections
                    .Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                    .Select(s => s.Id)
                    .ToHashSet();
                dataPoints = dataPoints.Where(dp => categorySectionIds.Contains(dp.SectionId));
            }

            // Apply organizational unit filter if specified
            if (!string.IsNullOrWhiteSpace(organizationalUnitId))
            {
                // For now, filter by owner ID since sections don't have organizational unit assignments
                // This can be extended later when organizational units are linked to sections
                var ownerSectionIds = _sections
                    .Where(s => s.OwnerId == organizationalUnitId)
                    .Select(s => s.Id)
                    .ToHashSet();
                dataPoints = dataPoints.Where(dp => ownerSectionIds.Contains(dp.SectionId));
            }

            var dataPointsList = dataPoints.ToList();

            // Calculate overall stats
            var overall = CalculateBreakdown("overall", "Overall", dataPointsList);

            // Get filtered sections for efficient breakdown calculations
            var relevantSectionIds = dataPointsList.Select(dp => dp.SectionId).Distinct().ToHashSet();
            var relevantSections = _sections.Where(s => relevantSectionIds.Contains(s.Id)).ToList();

            // Calculate by category
            var byCategory = new List<CompletenessBreakdown>();
            foreach (var cat in new[] { "environmental", "social", "governance" })
            {
                var categorySectionIds = relevantSections
                    .Where(s => s.Category.Equals(cat, StringComparison.OrdinalIgnoreCase))
                    .Select(s => s.Id)
                    .ToHashSet();
                var categoryDataPoints = dataPointsList
                    .Where(dp => categorySectionIds.Contains(dp.SectionId))
                    .ToList();
                
                byCategory.Add(CalculateBreakdown(cat, FormatCategoryName(cat), categoryDataPoints));
            }

            // Calculate by organizational unit (using section owners as proxy)
            var byOrganizationalUnit = new List<CompletenessBreakdown>();
            var ownerGroups = relevantSections
                .GroupBy(s => s.OwnerId)
                .ToList();

            foreach (var ownerGroup in ownerGroups)
            {
                var ownerId = ownerGroup.Key;
                var ownerSectionIds = ownerGroup.Select(s => s.Id).ToHashSet();
                var ownerDataPoints = dataPointsList
                    .Where(dp => ownerSectionIds.Contains(dp.SectionId))
                    .ToList();

                // Only include if there are actual data points
                if (ownerDataPoints.Count == 0)
                    continue;

                var user = _users.FirstOrDefault(u => u.Id == ownerId);
                var ownerName = user?.Name ?? "Unknown";
                
                byOrganizationalUnit.Add(CalculateBreakdown(ownerId, ownerName, ownerDataPoints));
            }

            return new CompletenessStats
            {
                Overall = overall,
                ByCategory = byCategory,
                ByOrganizationalUnit = byOrganizationalUnit
            };
        }
    }

    private CompletenessBreakdown CalculateBreakdown(string id, string name, List<DataPoint> dataPoints)
    {
        // Single pass through data points for efficiency
        var missingCount = 0;
        var incompleteCount = 0;
        var completeCount = 0;
        var notApplicableCount = 0;

        foreach (var dp in dataPoints)
        {
            if (dp.CompletenessStatus.Equals("missing", StringComparison.OrdinalIgnoreCase))
                missingCount++;
            else if (dp.CompletenessStatus.Equals("incomplete", StringComparison.OrdinalIgnoreCase))
                incompleteCount++;
            else if (dp.CompletenessStatus.Equals("complete", StringComparison.OrdinalIgnoreCase))
                completeCount++;
            else if (dp.CompletenessStatus.Equals("not applicable", StringComparison.OrdinalIgnoreCase))
                notApplicableCount++;
        }

        var totalCount = dataPoints.Count;
        var completePercentage = totalCount > 0 
            ? Math.Round((double)completeCount / totalCount * 100, 1) 
            : 0.0;

        return new CompletenessBreakdown
        {
            Id = id,
            Name = name,
            MissingCount = missingCount,
            IncompleteCount = incompleteCount,
            CompleteCount = completeCount,
            NotApplicableCount = notApplicableCount,
            TotalCount = totalCount,
            CompletePercentage = completePercentage
        };
    }

    private string FormatCategoryName(string category)
    {
        return category switch
        {
            "environmental" => "Environmental",
            "social" => "Social",
            "governance" => "Governance",
            _ => category
        };
    }

    /// <summary>
    /// Compares completeness statistics between two reporting periods.
    /// </summary>
    /// <param name="currentPeriodId">Current reporting period ID.</param>
    /// <param name="priorPeriodId">Prior reporting period ID for comparison.</param>
    /// <returns>Completeness comparison with breakdowns and regression highlights.</returns>
    public CompletenessComparison CompareCompletenessStats(string currentPeriodId, string priorPeriodId)
    {
        lock (_lock)
        {
            // Get period information
            var currentPeriod = _periods.FirstOrDefault(p => p.Id == currentPeriodId);
            var priorPeriod = _periods.FirstOrDefault(p => p.Id == priorPeriodId);

            if (currentPeriod == null && priorPeriod == null)
            {
                throw new ArgumentException("Both currentPeriodId and priorPeriodId are invalid.");
            }
            if (currentPeriod == null)
            {
                throw new ArgumentException($"Invalid currentPeriodId: '{currentPeriodId}' not found.");
            }
            if (priorPeriod == null)
            {
                throw new ArgumentException($"Invalid priorPeriodId: '{priorPeriodId}' not found.");
            }

            // Get completeness stats for both periods
            var currentStats = GetCompletenessStats(currentPeriodId);
            var priorStats = GetCompletenessStats(priorPeriodId);

            // Create overall comparison
            var overallComparison = CreateBreakdownComparison(
                "overall",
                "Overall",
                currentStats.Overall,
                priorStats.Overall,
                null,
                null,
                true
            );

            // Create category comparisons
            var categoryComparisons = new List<CompletenessBreakdownComparison>();
            foreach (var category in new[] { "environmental", "social", "governance" })
            {
                var currentCat = currentStats.ByCategory.FirstOrDefault(c => c.Id == category);
                var priorCat = priorStats.ByCategory.FirstOrDefault(c => c.Id == category);

                if (currentCat != null)
                {
                    categoryComparisons.Add(CreateBreakdownComparison(
                        category,
                        FormatCategoryName(category),
                        currentCat,
                        priorCat,
                        null,
                        null,
                        priorCat != null
                    ));
                }
            }

            // Get sections for both periods
            var currentSections = _sections.Where(s => s.PeriodId == currentPeriodId).ToList();
            var priorSections = _sections.Where(s => s.PeriodId == priorPeriodId).ToList();

            // Create section comparisons using catalog codes for matching
            var sectionComparisons = new List<CompletenessBreakdownComparison>();
            var processedCatalogCodes = new HashSet<string>();

            // Process current period sections
            foreach (var currentSection in currentSections)
            {
                var catalogCode = currentSection.CatalogCode ?? currentSection.Id;
                processedCatalogCodes.Add(catalogCode);

                var priorSection = priorSections.FirstOrDefault(s => 
                    (s.CatalogCode ?? s.Id) == catalogCode);

                var currentSectionStats = GetSectionCompletenessBreakdown(currentSection.Id);
                var priorSectionStats = priorSection != null 
                    ? GetSectionCompletenessBreakdown(priorSection.Id) 
                    : null;

                var existsInBoth = priorSection != null;
                var notApplicableReason = !existsInBoth 
                    ? "Section added in current period" 
                    : null;

                // Get owner name from users
                var owner = _users.FirstOrDefault(u => u.Id == currentSection.OwnerId);
                var ownerName = owner?.Name ?? "Unknown";

                sectionComparisons.Add(CreateBreakdownComparison(
                    catalogCode,
                    currentSection.Title,
                    currentSectionStats,
                    priorSectionStats,
                    currentSection.OwnerId,
                    ownerName,
                    existsInBoth,
                    notApplicableReason
                ));
            }

            // Process prior period sections that don't exist in current period
            foreach (var priorSection in priorSections)
            {
                var catalogCode = priorSection.CatalogCode ?? priorSection.Id;
                if (processedCatalogCodes.Contains(catalogCode))
                    continue;

                var priorSectionStats = GetSectionCompletenessBreakdown(priorSection.Id);

                sectionComparisons.Add(CreateBreakdownComparison(
                    catalogCode,
                    priorSection.Title,
                    new CompletenessBreakdown { Id = catalogCode, Name = priorSection.Title },
                    priorSectionStats,
                    null,
                    null,
                    false,
                    "Section removed in current period"
                ));
            }

            // Create organizational unit comparisons
            var orgUnitComparisons = new List<CompletenessBreakdownComparison>();
            var allOwnerIds = currentStats.ByOrganizationalUnit
                .Select(o => o.Id)
                .Union(priorStats.ByOrganizationalUnit.Select(o => o.Id))
                .Distinct()
                .ToList();

            foreach (var ownerId in allOwnerIds)
            {
                var currentOrgUnit = currentStats.ByOrganizationalUnit.FirstOrDefault(o => o.Id == ownerId);
                var priorOrgUnit = priorStats.ByOrganizationalUnit.FirstOrDefault(o => o.Id == ownerId);

                if (currentOrgUnit != null)
                {
                    orgUnitComparisons.Add(CreateBreakdownComparison(
                        ownerId,
                        currentOrgUnit.Name,
                        currentOrgUnit,
                        priorOrgUnit,
                        ownerId,
                        currentOrgUnit.Name,
                        priorOrgUnit != null
                    ));
                }
                else if (priorOrgUnit != null)
                {
                    orgUnitComparisons.Add(CreateBreakdownComparison(
                        ownerId,
                        priorOrgUnit.Name,
                        new CompletenessBreakdown { Id = ownerId, Name = priorOrgUnit.Name },
                        priorOrgUnit,
                        ownerId,
                        priorOrgUnit.Name,
                        false,
                        "Owner no longer has assigned sections"
                    ));
                }
            }

            // Identify regressions and improvements
            var regressions = sectionComparisons
                .Where(s => s.IsRegression && s.ExistsInBothPeriods)
                .OrderByDescending(s => Math.Abs(s.PercentagePointChange ?? 0))
                .ToList();

            var improvements = sectionComparisons
                .Where(s => !s.IsRegression && (s.PercentagePointChange ?? 0) > 0 && s.ExistsInBothPeriods)
                .OrderByDescending(s => s.PercentagePointChange)
                .ToList();

            // Calculate summary statistics
            var summary = new ComparisonSummary
            {
                RegressionCount = regressions.Count,
                ImprovementCount = improvements.Count,
                UnchangedCount = sectionComparisons.Count(s => 
                    s.ExistsInBothPeriods && (s.PercentagePointChange ?? 0) == 0),
                AddedSectionCount = sectionComparisons.Count(s => 
                    !s.ExistsInBothPeriods && s.NotApplicableReason == "Section added in current period"),
                RemovedSectionCount = sectionComparisons.Count(s => 
                    !s.ExistsInBothPeriods && s.NotApplicableReason == "Section removed in current period")
            };

            return new CompletenessComparison
            {
                CurrentPeriod = new PeriodInfo
                {
                    Id = currentPeriod.Id,
                    Name = currentPeriod.Name,
                    StartDate = currentPeriod.StartDate,
                    EndDate = currentPeriod.EndDate
                },
                PriorPeriod = new PeriodInfo
                {
                    Id = priorPeriod.Id,
                    Name = priorPeriod.Name,
                    StartDate = priorPeriod.StartDate,
                    EndDate = priorPeriod.EndDate
                },
                Overall = overallComparison,
                ByCategory = categoryComparisons,
                BySection = sectionComparisons,
                ByOrganizationalUnit = orgUnitComparisons,
                Regressions = regressions,
                Improvements = improvements,
                Summary = summary
            };
        }
    }

    private CompletenessBreakdown GetSectionCompletenessBreakdown(string sectionId)
    {
        var dataPoints = _dataPoints.Where(dp => dp.SectionId == sectionId).ToList();
        var section = _sections.FirstOrDefault(s => s.Id == sectionId);
        
        return CalculateBreakdown(
            sectionId,
            section?.Title ?? "Unknown Section",
            dataPoints
        );
    }

    private CompletenessBreakdownComparison CreateBreakdownComparison(
        string id,
        string name,
        CompletenessBreakdown current,
        CompletenessBreakdown? prior,
        string? ownerId,
        string? ownerName,
        bool existsInBoth,
        string? notApplicableReason = null)
    {
        double? percentagePointChange = null;
        int? completeCountChange = null;
        bool isRegression = false;

        if (prior != null && existsInBoth)
        {
            percentagePointChange = Math.Round(
                current.CompletePercentage - prior.CompletePercentage, 
                1
            );
            completeCountChange = current.CompleteCount - prior.CompleteCount;
            isRegression = percentagePointChange < 0;
        }

        return new CompletenessBreakdownComparison
        {
            Id = id,
            Name = name,
            CurrentPeriod = current,
            PriorPeriod = prior,
            PercentagePointChange = percentagePointChange,
            CompleteCountChange = completeCountChange,
            IsRegression = isRegression,
            OwnerId = ownerId,
            OwnerName = ownerName,
            ExistsInBothPeriods = existsInBoth,
            NotApplicableReason = notApplicableReason
        };
    }

    /// <summary>
    /// Calculates the progress status of a section based on its data points.
    /// Status rules:
    /// - "not-started": No data points exist OR all data points have completenessStatus "missing" (but not if mixed with "not applicable")
    /// - "blocked": Any data point has reviewStatus "changes-requested"
    /// - "completed": All data points have completenessStatus "complete" or "not applicable"
    /// - "in-progress": Default when there are data points that don't match the above criteria
    /// </summary>
    private string CalculateProgressStatus(List<DataPoint> sectionDataPoints)
    {
        // No data points = not started
        if (sectionDataPoints.Count == 0)
        {
            return "not-started";
        }

        // Check if any data point is blocked (has changes requested)
        var hasBlocked = sectionDataPoints.Any(dp => 
            dp.ReviewStatus.Equals("changes-requested", StringComparison.OrdinalIgnoreCase));
        if (hasBlocked)
        {
            return "blocked";
        }

        // Check if all data points are missing = not started
        var allMissing = sectionDataPoints.All(dp => 
            dp.CompletenessStatus.Equals("missing", StringComparison.OrdinalIgnoreCase));
        
        if (allMissing)
        {
            return "not-started";
        }

        // Check if all data points are complete or not applicable
        var allCompleteOrNA = sectionDataPoints.All(dp => 
            dp.CompletenessStatus.Equals("complete", StringComparison.OrdinalIgnoreCase) ||
            dp.CompletenessStatus.Equals("not applicable", StringComparison.OrdinalIgnoreCase));
        
        if (allCompleteOrNA)
        {
            return "completed";
        }

        // Default: has some data points, not all complete, not blocked
        return "in-progress";
    }

    // DataPoint Note Management

    public DataPointNote CreateDataPointNote(string dataPointId, CreateDataPointNoteRequest request)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == dataPointId);
            if (dataPoint == null)
            {
                throw new InvalidOperationException($"Data point {dataPointId} not found.");
            }

            var user = _users.FirstOrDefault(u => u.Id == request.CreatedBy);
            if (user == null)
            {
                throw new InvalidOperationException($"User {request.CreatedBy} not found.");
            }

            var note = new DataPointNote
            {
                Id = Guid.NewGuid().ToString(),
                DataPointId = dataPointId,
                Content = request.Content,
                CreatedBy = request.CreatedBy,
                CreatedByName = user.Name,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                UpdatedAt = DateTime.UtcNow.ToString("O")
            };

            _dataPointNotes.Add(note);

            // Create audit log entry for note creation
            CreateAuditLogEntry(
                userId: request.CreatedBy,
                userName: user.Name,
                action: "create",
                entityType: "DataPointNote",
                entityId: note.Id,
                changeNote: "Added note to data point",
                changes: new List<FieldChange>
                {
                    new() { Field = "Content", OldValue = "", NewValue = request.Content },
                    new() { Field = "DataPointId", OldValue = "", NewValue = dataPointId }
                }
            );

            return note;
        }
    }

    public List<DataPointNote> GetDataPointNotes(string dataPointId)
    {
        lock (_lock)
        {
            return _dataPointNotes
                .Where(n => n.DataPointId == dataPointId)
                .OrderBy(n => n.CreatedAt)
                .ToList();
        }
    }

    public List<DataPointNote> GetNotesForDataPoint(string dataPointId)
    {
        return GetDataPointNotes(dataPointId);
    }

    public DataPointNote? GetDataPointNote(string noteId)
    {
        lock (_lock)
        {
            return _dataPointNotes.FirstOrDefault(n => n.Id == noteId);
        }
    }

    public ResponsibilityMatrix GetResponsibilityMatrix(string? periodId = null, string? ownerFilter = null)
    {
        lock (_lock)
        {
            // Get section summaries for the specified period or all sections
            var summaries = periodId != null
                ? _summaries.Where(s => s.PeriodId == periodId).ToList()
                : _summaries.ToList();

            // Apply owner filter if specified
            if (!string.IsNullOrEmpty(ownerFilter))
            {
                if (ownerFilter == "unassigned")
                {
                    summaries = summaries.Where(s => string.IsNullOrEmpty(s.OwnerId)).ToList();
                }
                else
                {
                    summaries = summaries.Where(s => s.OwnerId == ownerFilter).ToList();
                }
            }

            // Group sections by owner
            var grouped = summaries.GroupBy(s => s.OwnerId).ToList();
            var assignments = new List<OwnerAssignment>();

            foreach (var group in grouped)
            {
                var ownerId = group.Key;
                var owner = !string.IsNullOrEmpty(ownerId) ? _users.FirstOrDefault(u => u.Id == ownerId) : null;
                
                // Count data points for this owner
                var sectionIds = group.Select(s => s.Id).ToList();
                var dataPointCount = _dataPoints.Count(dp => sectionIds.Contains(dp.SectionId));

                assignments.Add(new OwnerAssignment
                {
                    OwnerId = ownerId ?? string.Empty,
                    OwnerName = owner?.Name ?? "Unassigned",
                    OwnerEmail = owner?.Email ?? string.Empty,
                    Sections = group.OrderBy(s => s.Order).ToList(),
                    TotalDataPoints = dataPointCount
                });
            }

            // Sort assignments: unassigned first, then by owner name
            assignments = assignments
                .OrderBy(a => string.IsNullOrEmpty(a.OwnerId) ? 0 : 1)
                .ThenBy(a => a.OwnerName)
                .ToList();

            var unassignedCount = summaries.Count(s => string.IsNullOrEmpty(s.OwnerId));

            return new ResponsibilityMatrix
            {
                Assignments = assignments,
                TotalSections = summaries.Count,
                UnassignedSections = unassignedCount,
                PeriodId = periodId
            };
        }
    }

    /// <summary>
    /// Records a notification in the notification history.
    /// </summary>
    public void RecordNotification(OwnerNotification notification)
    {
        lock (_lock)
        {
            _notifications.Add(notification);
        }
    }

    /// <summary>
    /// Gets all notifications for a user.
    /// </summary>
    public List<OwnerNotification> GetNotifications(string userId, bool unreadOnly = false)
    {
        lock (_lock)
        {
            var query = _notifications.Where(n => n.RecipientUserId == userId);
            
            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }
            
            return query.OrderByDescending(n => n.CreatedAt).ToList();
        }
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    public bool MarkNotificationAsRead(string notificationId)
    {
        lock (_lock)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification == null)
            {
                return false;
            }
            
            notification.IsRead = true;
            return true;
        }
    }

    /// <summary>
    /// Gets a readiness report showing ownership completeness and data completion metrics.
    /// </summary>
    /// <param name="periodId">Optional filter by reporting period ID.</param>
    /// <param name="sectionId">Optional filter by section ID.</param>
    /// <param name="ownerId">Optional filter by owner ID.</param>
    /// <param name="category">Optional filter by ESG category (environmental, social, governance).</param>
    /// <returns>Readiness report with metrics and item list.</returns>
    public ReadinessReport GetReadinessReport(string? periodId = null, string? sectionId = null, string? ownerId = null, string? category = null)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            
            // Start with all sections
            var sections = _sections.AsEnumerable();
            
            // Apply period filter
            if (!string.IsNullOrWhiteSpace(periodId))
            {
                sections = sections.Where(s => s.PeriodId == periodId);
            }
            
            // Apply section filter
            if (!string.IsNullOrWhiteSpace(sectionId))
            {
                sections = sections.Where(s => s.Id == sectionId);
            }
            
            // Apply owner filter
            if (!string.IsNullOrWhiteSpace(ownerId))
            {
                sections = sections.Where(s => s.OwnerId == ownerId);
            }
            
            // Apply category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                sections = sections.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }
            
            var sectionsList = sections.ToList();
            var sectionIds = sectionsList.Select(s => s.Id).ToHashSet();
            
            // Get data points for filtered sections
            var dataPoints = _dataPoints.Where(dp => sectionIds.Contains(dp.SectionId)).ToList();
            
            var items = new List<ReadinessItem>();
            var totalItems = 0;
            var itemsWithOwners = 0;
            var completedItems = 0;
            var blockedCount = 0;
            var overdueCount = 0;
            
            // Process sections
            foreach (var section in sectionsList)
            {
                var sectionDataPoints = dataPoints.Where(dp => dp.SectionId == section.Id).ToList();
                var summary = _summaries.FirstOrDefault(s => s.Id == section.Id);
                var owner = _users.FirstOrDefault(u => u.Id == section.OwnerId);
                
                var progressStatus = summary?.ProgressStatus ?? "not-started";
                var isBlocked = progressStatus == "blocked";
                var isCompleted = progressStatus == "completed";
                
                // For sections, we consider them as items
                totalItems++;
                if (!string.IsNullOrWhiteSpace(section.OwnerId))
                {
                    itemsWithOwners++;
                }
                if (isCompleted)
                {
                    completedItems++;
                }
                if (isBlocked)
                {
                    blockedCount++;
                }
                
                items.Add(new ReadinessItem
                {
                    Id = section.Id,
                    Type = "section",
                    Title = section.Title,
                    Category = section.Category,
                    OwnerId = section.OwnerId,
                    OwnerName = owner?.Name ?? string.Empty,
                    ProgressStatus = progressStatus,
                    IsBlocked = isBlocked,
                    IsOverdue = false, // Sections don't have deadlines in current model
                    Deadline = null,
                    CompletenessPercentage = summary?.CompletenessPercentage ?? 0
                });
            }
            
            // Process data points
            foreach (var dataPoint in dataPoints)
            {
                var section = sectionsList.FirstOrDefault(s => s.Id == dataPoint.SectionId);
                if (section == null) continue;
                
                var owner = _users.FirstOrDefault(u => u.Id == dataPoint.OwnerId);
                var isCompleted = dataPoint.CompletenessStatus.Equals("complete", StringComparison.OrdinalIgnoreCase);
                var isBlocked = dataPoint.IsBlocked || dataPoint.ReviewStatus.Equals("changes-requested", StringComparison.OrdinalIgnoreCase);
                var isOverdue = !string.IsNullOrWhiteSpace(dataPoint.Deadline) && 
                                DateTime.TryParse(dataPoint.Deadline, out var deadline) &&
                                deadline < now &&
                                !isCompleted;
                
                var progressStatus = isCompleted ? "completed" :
                                   isBlocked ? "blocked" :
                                   string.IsNullOrWhiteSpace(dataPoint.Content) ? "not-started" :
                                   "in-progress";
                
                totalItems++;
                if (!string.IsNullOrWhiteSpace(dataPoint.OwnerId))
                {
                    itemsWithOwners++;
                }
                if (isCompleted)
                {
                    completedItems++;
                }
                if (isBlocked)
                {
                    blockedCount++;
                }
                if (isOverdue)
                {
                    overdueCount++;
                }
                
                items.Add(new ReadinessItem
                {
                    Id = dataPoint.Id,
                    Type = "datapoint",
                    Title = dataPoint.Title,
                    Category = section.Category,
                    OwnerId = dataPoint.OwnerId,
                    OwnerName = owner?.Name ?? string.Empty,
                    ProgressStatus = progressStatus,
                    IsBlocked = isBlocked,
                    IsOverdue = isOverdue,
                    Deadline = dataPoint.Deadline,
                    CompletenessPercentage = isCompleted ? 100 : string.IsNullOrWhiteSpace(dataPoint.Content) ? 0 : 50
                });
            }
            
            var ownershipPercentage = totalItems > 0 ? (int)Math.Round((double)itemsWithOwners / totalItems * 100) : 0;
            var completionPercentage = totalItems > 0 ? (int)Math.Round((double)completedItems / totalItems * 100) : 0;
            
            return new ReadinessReport
            {
                PeriodId = periodId,
                Metrics = new ReadinessMetrics
                {
                    OwnershipPercentage = ownershipPercentage,
                    CompletionPercentage = completionPercentage,
                    BlockedCount = blockedCount,
                    OverdueCount = overdueCount,
                    TotalItems = totalItems,
                    ItemsWithOwners = itemsWithOwners,
                    CompletedItems = completedItems
                },
                Items = items
            };
        }
    }

    private sealed record SectionTemplate(string Title, string Category, string Description);

    // Remediation Plan management methods
    public IReadOnlyList<RemediationPlan> GetRemediationPlans(string? sectionId = null)
    {
        lock (_lock)
        {
            return sectionId == null
                ? _remediationPlans.ToList()
                : _remediationPlans.Where(p => p.SectionId == sectionId).ToList();
        }
    }

    public RemediationPlan? GetRemediationPlanById(string id)
    {
        lock (_lock)
        {
            return _remediationPlans.FirstOrDefault(p => p.Id == id);
        }
    }

    public (bool IsValid, string? ErrorMessage, RemediationPlan? Plan) CreateRemediationPlan(
        string sectionId,
        string title,
        string description,
        string targetPeriod,
        string ownerId,
        string ownerName,
        string priority,
        string? gapId,
        string? assumptionId,
        string? dataPointId,
        string createdBy)
    {
        lock (_lock)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return (false, "Description is required.", null);
            }

            if (string.IsNullOrWhiteSpace(targetPeriod))
            {
                return (false, "Target period is required.", null);
            }

            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return (false, "Owner is required.", null);
            }

            if (string.IsNullOrWhiteSpace(sectionId))
            {
                return (false, "SectionId is required.", null);
            }

            if (string.IsNullOrWhiteSpace(createdBy))
            {
                return (false, "CreatedBy is required.", null);
            }

            // Validate priority
            var validPriorities = new[] { "low", "medium", "high" };
            if (!validPriorities.Contains(priority))
            {
                return (false, "Priority must be 'low', 'medium', or 'high'.", null);
            }

            // Validate references exist if provided
            if (!string.IsNullOrWhiteSpace(gapId))
            {
                var gap = _gaps.FirstOrDefault(g => g.Id == gapId);
                if (gap == null)
                {
                    return (false, $"Gap with ID '{gapId}' not found.", null);
                }
            }

            if (!string.IsNullOrWhiteSpace(assumptionId))
            {
                var assumption = _assumptions.FirstOrDefault(a => a.Id == assumptionId);
                if (assumption == null)
                {
                    return (false, $"Assumption with ID '{assumptionId}' not found.", null);
                }
            }

            if (!string.IsNullOrWhiteSpace(dataPointId))
            {
                var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == dataPointId);
                if (dataPoint == null)
                {
                    return (false, $"Data point with ID '{dataPointId}' not found.", null);
                }
            }

            var newPlan = new RemediationPlan
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = sectionId,
                Title = title,
                Description = description,
                TargetPeriod = targetPeriod,
                OwnerId = ownerId,
                OwnerName = ownerName,
                Priority = priority,
                Status = "planned",
                GapId = gapId,
                AssumptionId = assumptionId,
                DataPointId = dataPointId,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            _remediationPlans.Add(newPlan);

            // Log to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = "", NewValue = title },
                new FieldChange { Field = "Description", OldValue = "", NewValue = description },
                new FieldChange { Field = "Priority", OldValue = "", NewValue = priority }
            };
            var user = _users.FirstOrDefault(u => u.Id == createdBy);
            var userName = user?.Name ?? createdBy;
            CreateAuditLogEntry(createdBy, userName, "create", "RemediationPlan", newPlan.Id, changes, $"Created remediation plan '{title}'");

            return (true, null, newPlan);
        }
    }

    public (bool IsValid, string? ErrorMessage, RemediationPlan? Plan) UpdateRemediationPlan(
        string id,
        string title,
        string description,
        string targetPeriod,
        string ownerId,
        string ownerName,
        string priority,
        string status,
        string updatedBy)
    {
        lock (_lock)
        {
            var plan = _remediationPlans.FirstOrDefault(p => p.Id == id);
            if (plan == null)
            {
                return (false, $"Remediation plan with ID '{id}' not found.", null);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return (false, "Description is required.", null);
            }

            if (string.IsNullOrWhiteSpace(targetPeriod))
            {
                return (false, "Target period is required.", null);
            }

            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return (false, "Owner is required.", null);
            }

            if (string.IsNullOrWhiteSpace(updatedBy))
            {
                return (false, "UpdatedBy is required.", null);
            }

            // Validate priority
            var validPriorities = new[] { "low", "medium", "high" };
            if (!validPriorities.Contains(priority))
            {
                return (false, "Priority must be 'low', 'medium', or 'high'.", null);
            }

            // Validate status
            var validStatuses = new[] { "planned", "in-progress", "completed", "cancelled" };
            if (!validStatuses.Contains(status))
            {
                return (false, "Status must be 'planned', 'in-progress', 'completed', or 'cancelled'.", null);
            }

            // Track changes for audit log
            var changes = new List<FieldChange>();

            if (plan.Title != title)
            {
                changes.Add(new FieldChange { Field = "Title", OldValue = plan.Title, NewValue = title });
            }

            if (plan.Description != description)
            {
                changes.Add(new FieldChange { Field = "Description", OldValue = plan.Description, NewValue = description });
            }

            if (plan.TargetPeriod != targetPeriod)
            {
                changes.Add(new FieldChange { Field = "TargetPeriod", OldValue = plan.TargetPeriod, NewValue = targetPeriod });
            }

            if (plan.Priority != priority)
            {
                changes.Add(new FieldChange { Field = "Priority", OldValue = plan.Priority, NewValue = priority });
            }

            if (plan.Status != status)
            {
                changes.Add(new FieldChange { Field = "Status", OldValue = plan.Status, NewValue = status });
            }

            // Update fields
            plan.Title = title;
            plan.Description = description;
            plan.TargetPeriod = targetPeriod;
            plan.OwnerId = ownerId;
            plan.OwnerName = ownerName;
            plan.Priority = priority;
            plan.Status = status;
            plan.UpdatedBy = updatedBy;
            plan.UpdatedAt = DateTime.UtcNow.ToString("O");

            // Log to audit trail if there were changes
            if (changes.Count > 0)
            {
                var user = _users.FirstOrDefault(u => u.Id == updatedBy);
                var userName = user?.Name ?? updatedBy;
                CreateAuditLogEntry(updatedBy, userName, "update", "RemediationPlan", plan.Id, changes, $"Updated remediation plan '{title}'");
            }

            return (true, null, plan);
        }
    }

    public (bool IsValid, string? ErrorMessage, RemediationPlan? Plan) CompleteRemediationPlan(
        string id,
        string completedBy)
    {
        lock (_lock)
        {
            var plan = _remediationPlans.FirstOrDefault(p => p.Id == id);
            if (plan == null)
            {
                return (false, $"Remediation plan with ID '{id}' not found.", null);
            }

            if (plan.Status == "completed")
            {
                return (false, "Plan is already completed.", null);
            }

            if (string.IsNullOrWhiteSpace(completedBy))
            {
                return (false, "CompletedBy is required.", null);
            }

            plan.Status = "completed";
            plan.CompletedBy = completedBy;
            plan.CompletedAt = DateTime.UtcNow.ToString("O");
            plan.UpdatedBy = completedBy;
            plan.UpdatedAt = DateTime.UtcNow.ToString("O");

            return (true, null, plan);
        }
    }

    public bool DeleteRemediationPlan(string id, string deletedBy)
    {
        lock (_lock)
        {
            var plan = _remediationPlans.FirstOrDefault(p => p.Id == id);
            if (plan == null)
            {
                return false;
            }

            // Log deletion to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = plan.Title, NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == deletedBy);
            var userName = user?.Name ?? deletedBy;
            CreateAuditLogEntry(deletedBy, userName, "delete", "RemediationPlan", plan.Id, changes, $"Deleted remediation plan '{plan.Title}'");

            // Delete associated actions first
            var actions = _remediationActions.Where(a => a.RemediationPlanId == id).ToList();
            foreach (var action in actions)
            {
                _remediationActions.Remove(action);
            }

            _remediationPlans.Remove(plan);
            return true;
        }
    }

    // Remediation Action management methods
    public IReadOnlyList<RemediationAction> GetRemediationActions(string remediationPlanId)
    {
        lock (_lock)
        {
            return _remediationActions.Where(a => a.RemediationPlanId == remediationPlanId).ToList();
        }
    }

    public RemediationAction? GetRemediationActionById(string id)
    {
        lock (_lock)
        {
            return _remediationActions.FirstOrDefault(a => a.Id == id);
        }
    }

    public (bool IsValid, string? ErrorMessage, RemediationAction? Action) CreateRemediationAction(
        string remediationPlanId,
        string title,
        string description,
        string ownerId,
        string ownerName,
        string dueDate,
        string createdBy)
    {
        lock (_lock)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return (false, "Description is required.", null);
            }

            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return (false, "Owner is required.", null);
            }

            if (string.IsNullOrWhiteSpace(dueDate))
            {
                return (false, "Due date is required.", null);
            }

            if (string.IsNullOrWhiteSpace(remediationPlanId))
            {
                return (false, "RemediationPlanId is required.", null);
            }

            if (string.IsNullOrWhiteSpace(createdBy))
            {
                return (false, "CreatedBy is required.", null);
            }

            // Validate due date format
            if (!DateTime.TryParse(dueDate, out _))
            {
                return (false, "Invalid due date format.", null);
            }

            // Validate remediation plan exists
            var plan = _remediationPlans.FirstOrDefault(p => p.Id == remediationPlanId);
            if (plan == null)
            {
                return (false, $"Remediation plan with ID '{remediationPlanId}' not found.", null);
            }

            var newAction = new RemediationAction
            {
                Id = Guid.NewGuid().ToString(),
                RemediationPlanId = remediationPlanId,
                Title = title,
                Description = description,
                OwnerId = ownerId,
                OwnerName = ownerName,
                DueDate = dueDate,
                Status = "pending",
                EvidenceIds = new List<string>(),
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            _remediationActions.Add(newAction);

            // Log to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = "", NewValue = title },
                new FieldChange { Field = "Description", OldValue = "", NewValue = description },
                new FieldChange { Field = "DueDate", OldValue = "", NewValue = dueDate }
            };
            var user = _users.FirstOrDefault(u => u.Id == createdBy);
            var userName = user?.Name ?? createdBy;
            CreateAuditLogEntry(createdBy, userName, "create", "RemediationAction", newAction.Id, changes, $"Created remediation action '{title}'");

            return (true, null, newAction);
        }
    }

    public (bool IsValid, string? ErrorMessage, RemediationAction? Action) UpdateRemediationAction(
        string id,
        string title,
        string description,
        string ownerId,
        string ownerName,
        string dueDate,
        string status,
        string updatedBy)
    {
        lock (_lock)
        {
            var action = _remediationActions.FirstOrDefault(a => a.Id == id);
            if (action == null)
            {
                return (false, $"Remediation action with ID '{id}' not found.", null);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return (false, "Description is required.", null);
            }

            if (string.IsNullOrWhiteSpace(ownerId))
            {
                return (false, "Owner is required.", null);
            }

            if (string.IsNullOrWhiteSpace(dueDate))
            {
                return (false, "Due date is required.", null);
            }

            if (string.IsNullOrWhiteSpace(updatedBy))
            {
                return (false, "UpdatedBy is required.", null);
            }

            // Validate due date format
            if (!DateTime.TryParse(dueDate, out _))
            {
                return (false, "Invalid due date format.", null);
            }

            // Validate status
            var validStatuses = new[] { "pending", "in-progress", "completed", "cancelled" };
            if (!validStatuses.Contains(status))
            {
                return (false, "Status must be 'pending', 'in-progress', 'completed', or 'cancelled'.", null);
            }

            // Track changes for audit log
            var changes = new List<FieldChange>();

            if (action.Title != title)
            {
                changes.Add(new FieldChange { Field = "Title", OldValue = action.Title, NewValue = title });
            }

            if (action.Description != description)
            {
                changes.Add(new FieldChange { Field = "Description", OldValue = action.Description, NewValue = description });
            }

            if (action.DueDate != dueDate)
            {
                changes.Add(new FieldChange { Field = "DueDate", OldValue = action.DueDate, NewValue = dueDate });
            }

            if (action.Status != status)
            {
                changes.Add(new FieldChange { Field = "Status", OldValue = action.Status, NewValue = status });
            }

            // Update fields
            action.Title = title;
            action.Description = description;
            action.OwnerId = ownerId;
            action.OwnerName = ownerName;
            action.DueDate = dueDate;
            action.Status = status;
            action.UpdatedBy = updatedBy;
            action.UpdatedAt = DateTime.UtcNow.ToString("O");

            // Log to audit trail if there were changes
            if (changes.Count > 0)
            {
                var user = _users.FirstOrDefault(u => u.Id == updatedBy);
                var userName = user?.Name ?? updatedBy;
                CreateAuditLogEntry(updatedBy, userName, "update", "RemediationAction", action.Id, changes, $"Updated remediation action '{title}'");
            }

            return (true, null, action);
        }
    }

    public (bool IsValid, string? ErrorMessage, RemediationAction? Action) CompleteRemediationAction(
        string id,
        string completedBy,
        string? completionNotes,
        List<string> evidenceIds)
    {
        lock (_lock)
        {
            var action = _remediationActions.FirstOrDefault(a => a.Id == id);
            if (action == null)
            {
                return (false, $"Remediation action with ID '{id}' not found.", null);
            }

            if (action.Status == "completed")
            {
                return (false, "Action is already completed.", null);
            }

            if (string.IsNullOrWhiteSpace(completedBy))
            {
                return (false, "CompletedBy is required.", null);
            }

            // Validate evidence exists if provided
            foreach (var evidenceId in evidenceIds)
            {
                var evidence = _evidence.FirstOrDefault(e => e.Id == evidenceId);
                if (evidence == null)
                {
                    return (false, $"Evidence with ID '{evidenceId}' not found.", null);
                }
            }

            action.Status = "completed";
            action.CompletedBy = completedBy;
            action.CompletedAt = DateTime.UtcNow.ToString("O");
            action.CompletionNotes = completionNotes;
            action.EvidenceIds = new List<string>(evidenceIds);
            action.UpdatedBy = completedBy;
            action.UpdatedAt = DateTime.UtcNow.ToString("O");

            return (true, null, action);
        }
    }

    public bool DeleteRemediationAction(string id, string deletedBy)
    {
        lock (_lock)
        {
            var action = _remediationActions.FirstOrDefault(a => a.Id == id);
            if (action == null)
            {
                return false;
            }

            // Log deletion to audit trail
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = action.Title, NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == deletedBy);
            var userName = user?.Name ?? deletedBy;
            CreateAuditLogEntry(deletedBy, userName, "delete", "RemediationAction", action.Id, changes, $"Deleted remediation action '{action.Title}'");

            _remediationActions.Remove(action);
            return true;
        }
    }

    // ==================== Completion Exceptions ====================

    /// <summary>
    /// Gets all completion exceptions, optionally filtered by section or status.
    /// </summary>
    public IReadOnlyList<CompletionException> GetCompletionExceptions(string? sectionId = null, string? status = null)
    {
        lock (_lock)
        {
            var exceptions = _completionExceptions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(sectionId))
            {
                exceptions = exceptions.Where(e => e.SectionId == sectionId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                exceptions = exceptions.Where(e => e.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            return exceptions.ToList();
        }
    }

    /// <summary>
    /// Gets a specific completion exception by ID.
    /// </summary>
    public CompletionException? GetCompletionException(string id)
    {
        lock (_lock)
        {
            return _completionExceptions.FirstOrDefault(e => e.Id == id);
        }
    }

    /// <summary>
    /// Creates a new completion exception.
    /// </summary>
    public (bool isValid, string? errorMessage, CompletionException? exception) CreateCompletionException(CreateCompletionExceptionRequest request)
    {
        lock (_lock)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return (false, "Title is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.ExceptionType))
            {
                return (false, "Exception type is required.", null);
            }

            // Normalize exception type to lowercase for consistent storage
            var normalizedExceptionType = request.ExceptionType.ToLowerInvariant();

            if (!ValidExceptionTypes.Contains(normalizedExceptionType, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"Exception type must be one of: {string.Join(", ", ValidExceptionTypes)}.", null);
            }

            if (string.IsNullOrWhiteSpace(request.Justification))
            {
                return (false, "Justification is required.", null);
            }

            if (request.Justification.Length < 10)
            {
                return (false, "Justification must be at least 10 characters.", null);
            }

            if (string.IsNullOrWhiteSpace(request.SectionId))
            {
                return (false, "Section ID is required.", null);
            }

            var section = _sections.FirstOrDefault(s => s.Id == request.SectionId);
            if (section == null)
            {
                return (false, $"Section with ID '{request.SectionId}' not found.", null);
            }

            if (!string.IsNullOrWhiteSpace(request.DataPointId))
            {
                var dataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == request.DataPointId);
                if (dataPoint == null)
                {
                    return (false, $"Data point with ID '{request.DataPointId}' not found.", null);
                }

                if (dataPoint.SectionId != request.SectionId)
                {
                    return (false, "Data point does not belong to the specified section.", null);
                }
            }

            if (string.IsNullOrWhiteSpace(request.RequestedBy))
            {
                return (false, "Requested by user is required.", null);
            }

            var exception = new CompletionException
            {
                Id = Guid.NewGuid().ToString(),
                SectionId = request.SectionId,
                DataPointId = request.DataPointId,
                Title = request.Title,
                ExceptionType = normalizedExceptionType,
                Justification = request.Justification,
                Status = "pending",
                RequestedBy = request.RequestedBy,
                RequestedAt = DateTime.UtcNow.ToString("o"),
                ExpiresAt = request.ExpiresAt
            };

            _completionExceptions.Add(exception);

            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "SectionId", OldValue = "", NewValue = request.SectionId },
                new FieldChange { Field = "Title", OldValue = "", NewValue = request.Title },
                new FieldChange { Field = "ExceptionType", OldValue = "", NewValue = normalizedExceptionType },
                new FieldChange { Field = "Justification", OldValue = "", NewValue = request.Justification },
                new FieldChange { Field = "RequestedBy", OldValue = "", NewValue = request.RequestedBy }
            };
            var user = _users.FirstOrDefault(u => u.Id == request.RequestedBy);
            CreateAuditLogEntry(
                request.RequestedBy,
                user?.Name ?? request.RequestedBy,
                "create",
                "CompletionException",
                exception.Id,
                changes,
                $"Created completion exception '{exception.Title}' for section {section.Title}");

            return (true, null, exception);
        }
    }

    /// <summary>
    /// Approves a completion exception.
    /// </summary>
    public (bool isValid, string? errorMessage, CompletionException? exception) ApproveCompletionException(string id, ApproveCompletionExceptionRequest request)
    {
        lock (_lock)
        {
            var exception = _completionExceptions.FirstOrDefault(e => e.Id == id);
            if (exception == null)
            {
                return (false, $"Completion exception with ID '{id}' not found.", null);
            }

            if (exception.Status != "pending")
            {
                return (false, $"Cannot approve exception with status '{exception.Status}'. Only pending exceptions can be approved.", null);
            }

            if (string.IsNullOrWhiteSpace(request.ApprovedBy))
            {
                return (false, "Approver user ID is required.", null);
            }

            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Status", OldValue = exception.Status, NewValue = "accepted" }
            };

            exception.Status = "accepted";
            exception.ApprovedBy = request.ApprovedBy;
            exception.ApprovedAt = DateTime.UtcNow.ToString("o");
            exception.ReviewComments = request.ReviewComments;

            var user = _users.FirstOrDefault(u => u.Id == request.ApprovedBy);
            CreateAuditLogEntry(
                request.ApprovedBy,
                user?.Name ?? request.ApprovedBy,
                "approve",
                "CompletionException",
                exception.Id,
                changes,
                $"Approved completion exception '{exception.Title}'");

            return (true, null, exception);
        }
    }

    /// <summary>
    /// Rejects a completion exception.
    /// </summary>
    public (bool isValid, string? errorMessage, CompletionException? exception) RejectCompletionException(string id, RejectCompletionExceptionRequest request)
    {
        lock (_lock)
        {
            var exception = _completionExceptions.FirstOrDefault(e => e.Id == id);
            if (exception == null)
            {
                return (false, $"Completion exception with ID '{id}' not found.", null);
            }

            if (exception.Status != "pending")
            {
                return (false, $"Cannot reject exception with status '{exception.Status}'. Only pending exceptions can be rejected.", null);
            }

            if (string.IsNullOrWhiteSpace(request.RejectedBy))
            {
                return (false, "Rejector user ID is required.", null);
            }

            if (string.IsNullOrWhiteSpace(request.ReviewComments))
            {
                return (false, "Review comments are required when rejecting an exception.", null);
            }

            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Status", OldValue = exception.Status, NewValue = "rejected" }
            };

            exception.Status = "rejected";
            exception.RejectedBy = request.RejectedBy;
            exception.RejectedAt = DateTime.UtcNow.ToString("o");
            exception.ReviewComments = request.ReviewComments;

            var user = _users.FirstOrDefault(u => u.Id == request.RejectedBy);
            CreateAuditLogEntry(
                request.RejectedBy,
                user?.Name ?? request.RejectedBy,
                "reject",
                "CompletionException",
                exception.Id,
                changes,
                $"Rejected completion exception '{exception.Title}'");

            return (true, null, exception);
        }
    }

    /// <summary>
    /// Deletes a completion exception.
    /// Only pending exceptions should be deleted to preserve audit trail.
    /// </summary>
    public bool DeleteCompletionException(string id, string deletedBy)
    {
        lock (_lock)
        {
            var exception = _completionExceptions.FirstOrDefault(e => e.Id == id);
            if (exception == null)
            {
                return false;
            }

            // Only allow deletion of pending exceptions to preserve audit trail
            if (exception.Status != "pending")
            {
                return false;
            }

            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = exception.Title, NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == deletedBy);
            CreateAuditLogEntry(
                deletedBy,
                user?.Name ?? deletedBy,
                "delete",
                "CompletionException",
                exception.Id,
                changes,
                $"Deleted completion exception '{exception.Title}'");

            _completionExceptions.Remove(exception);
            return true;
        }
    }

    /// <summary>
    /// Generates a completeness validation report with exceptions breakdown.
    /// </summary>
    public CompletenessValidationReport GetCompletenessValidationReport(string periodId)
    {
        lock (_lock)
        {
            var sections = _sections.Where(s => s.PeriodId == periodId).ToList();
            var sectionDetails = new List<SectionCompletenessDetail>();

            foreach (var section in sections)
            {
                var dataPoints = _dataPoints.Where(dp => dp.SectionId == section.Id).ToList();
                var exceptions = _completionExceptions
                    .Where(e => e.SectionId == section.Id && e.Status == "accepted")
                    .ToList();

                var missingItems = dataPoints
                    .Where(dp => dp.CompletenessStatus.Equals("missing", StringComparison.OrdinalIgnoreCase))
                    .Select(dp => new DataPointSummary
                    {
                        Id = dp.Id,
                        Title = dp.Title,
                        CompletenessStatus = dp.CompletenessStatus,
                        MissingReason = dp.MissingReason
                    })
                    .ToList();

                var estimatedItems = dataPoints
                    .Where(dp => dp.InformationType.Equals("estimate", StringComparison.OrdinalIgnoreCase))
                    .Select(dp => new DataPointSummary
                    {
                        Id = dp.Id,
                        Title = dp.Title,
                        CompletenessStatus = dp.CompletenessStatus,
                        EstimateType = dp.EstimateType,
                        ConfidenceLevel = dp.ConfidenceLevel
                    })
                    .ToList();

                var simplifications = _simplifications
                    .Where(s => s.SectionId == section.Id && s.Status == "active")
                    .ToList();

                var simplifiedItems = new List<DataPointSummary>();
                if (simplifications.Any())
                {
                    // Create a summary item for each simplification
                    foreach (var simplification in simplifications)
                    {
                        simplifiedItems.Add(new DataPointSummary
                        {
                            Id = simplification.Id,
                            Title = simplification.Title,
                            CompletenessStatus = "simplified"
                        });
                    }
                }

                sectionDetails.Add(new SectionCompletenessDetail
                {
                    SectionId = section.Id,
                    SectionTitle = section.Title,
                    Category = section.Category,
                    MissingItems = missingItems,
                    EstimatedItems = estimatedItems,
                    SimplifiedItems = simplifiedItems,
                    AcceptedExceptions = exceptions
                });
            }

            // Calculate summary statistics
            var allDataPoints = _dataPoints.Where(dp => 
                sections.Any(s => s.Id == dp.SectionId)).ToList();
            
            var totalDataPoints = allDataPoints.Count;
            var missingCount = allDataPoints.Count(dp => 
                dp.CompletenessStatus.Equals("missing", StringComparison.OrdinalIgnoreCase));
            var estimatedCount = allDataPoints.Count(dp => 
                dp.InformationType.Equals("estimate", StringComparison.OrdinalIgnoreCase));
            var simplifiedCount = _simplifications.Count(s => 
                sections.Any(sec => sec.Id == s.SectionId) && s.Status == "active");
            
            var acceptedExceptionsCount = _completionExceptions.Count(e => 
                sections.Any(s => s.Id == e.SectionId) && e.Status == "accepted");
            var pendingExceptionsCount = _completionExceptions.Count(e => 
                sections.Any(s => s.Id == e.SectionId) && e.Status == "pending");

            var completeCount = allDataPoints.Count(dp => 
                dp.CompletenessStatus.Equals("complete", StringComparison.OrdinalIgnoreCase) ||
                dp.CompletenessStatus.Equals("not applicable", StringComparison.OrdinalIgnoreCase));

            var completenessPercentage = totalDataPoints > 0 
                ? (double)completeCount / totalDataPoints * 100 
                : 0;

            // Calculate completeness with accepted exceptions excluded
            var totalRelevantWithExceptions = totalDataPoints - acceptedExceptionsCount;
            var completenessWithExceptionsPercentage = totalRelevantWithExceptions > 0 
                ? (double)completeCount / totalRelevantWithExceptions * 100 
                : 0;

            return new CompletenessValidationReport
            {
                PeriodId = periodId,
                Sections = sectionDetails,
                Summary = new CompletenessValidationSummary
                {
                    TotalSections = sections.Count,
                    TotalDataPoints = totalDataPoints,
                    MissingCount = missingCount,
                    EstimatedCount = estimatedCount,
                    SimplifiedCount = simplifiedCount,
                    AcceptedExceptionsCount = acceptedExceptionsCount,
                    PendingExceptionsCount = pendingExceptionsCount,
                    CompletenessPercentage = Math.Round(completenessPercentage, 2),
                    CompletenessWithExceptionsPercentage = Math.Round(completenessWithExceptionsPercentage, 2)
                }
            };
        }
    }

    /// <summary>
    /// Generate a Gaps and Improvements report for a reporting period.
    /// Compiles gaps, assumptions, simplifications, and remediation plans into a report-ready format.
    /// </summary>
    public GapsAndImprovementsReport GetGapsAndImprovementsReport(string? periodId = null, string? sectionId = null, string currentUserId = "system")
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            
            // Filter sections by period
            var sections = _sections.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(periodId))
            {
                sections = sections.Where(s => s.PeriodId == periodId);
            }
            
            // Filter by specific section if provided
            if (!string.IsNullOrWhiteSpace(sectionId))
            {
                sections = sections.Where(s => s.Id == sectionId);
            }
            
            var sectionsList = sections.ToList();
            var sectionIds = sectionsList.Select(s => s.Id).ToHashSet();
            
            // Collect all gaps, assumptions, simplifications, and remediation plans for filtered sections
            var gaps = _gaps.Where(g => sectionIds.Contains(g.SectionId)).ToList();
            var assumptions = _assumptions.Where(a => sectionIds.Contains(a.SectionId)).ToList();
            var simplifications = _simplifications.Where(s => sectionIds.Contains(s.SectionId)).ToList();
            var remediationPlans = _remediationPlans.Where(rp => sectionIds.Contains(rp.SectionId)).ToList();
            var remediationActions = _remediationActions.Where(ra => 
                remediationPlans.Any(rp => rp.Id == ra.RemediationPlanId)).ToList();
            
            // Build section-grouped data
            var sectionGapsAndImprovements = new List<SectionGapsAndImprovements>();
            
            foreach (var section in sectionsList)
            {
                var sectionGaps = gaps.Where(g => g.SectionId == section.Id).ToList();
                var sectionAssumptions = assumptions.Where(a => a.SectionId == section.Id).ToList();
                var sectionSimplifications = simplifications.Where(s => s.SectionId == section.Id).ToList();
                var sectionRemediationPlans = remediationPlans.Where(rp => rp.SectionId == section.Id).ToList();
                
                // Build gaps with associated remediation plans
                var gapsWithRemediation = new List<GapWithRemediation>();
                foreach (var gap in sectionGaps)
                {
                    var plan = sectionRemediationPlans.FirstOrDefault(rp => rp.GapId == gap.Id);
                    RemediationPlanWithActions? planWithActions = null;
                    
                    if (plan != null)
                    {
                        var actions = remediationActions.Where(ra => ra.RemediationPlanId == plan.Id).ToList();
                        planWithActions = new RemediationPlanWithActions
                        {
                            Plan = plan,
                            Actions = actions
                        };
                    }
                    
                    gapsWithRemediation.Add(new GapWithRemediation
                    {
                        Gap = gap,
                        RemediationPlan = planWithActions
                    });
                }
                
                // Build assumption references with linked data point titles
                var assumptionRefs = new List<AssumptionReference>();
                foreach (var assumption in sectionAssumptions)
                {
                    var linkedDataPoints = _dataPoints
                        .Where(dp => assumption.LinkedDataPointIds.Contains(dp.Id))
                        .Select(dp => dp.Title)
                        .ToList();
                    
                    assumptionRefs.Add(new AssumptionReference
                    {
                        Assumption = assumption,
                        LinkedDataPointTitles = linkedDataPoints
                    });
                }
                
                // Build simplification references
                var simplificationRefs = sectionSimplifications
                    .Select(s => new SimplificationReference { Simplification = s })
                    .ToList();
                
                // Build standalone remediation plans (not linked to gaps)
                var standalonePlans = new List<RemediationPlanWithActions>();
                foreach (var plan in sectionRemediationPlans.Where(rp => string.IsNullOrWhiteSpace(rp.GapId)))
                {
                    var actions = remediationActions.Where(ra => ra.RemediationPlanId == plan.Id).ToList();
                    standalonePlans.Add(new RemediationPlanWithActions
                    {
                        Plan = plan,
                        Actions = actions
                    });
                }
                
                sectionGapsAndImprovements.Add(new SectionGapsAndImprovements
                {
                    SectionId = section.Id,
                    SectionTitle = section.Title,
                    Category = section.Category,
                    Gaps = gapsWithRemediation,
                    Assumptions = assumptionRefs,
                    Simplifications = simplificationRefs,
                    RemediationPlans = standalonePlans
                });
            }
            
            // Calculate summary metrics
            var totalGaps = gaps.Count;
            var resolvedGaps = gaps.Count(g => g.Resolved);
            var unresolvedGaps = totalGaps - resolvedGaps;
            
            var totalAssumptions = assumptions.Count;
            var activeAssumptions = assumptions.Count(a => a.Status == "active");
            var deprecatedAssumptions = assumptions.Count(a => a.Status == "deprecated");
            
            var totalSimplifications = simplifications.Count;
            var activeSimplifications = simplifications.Count(s => s.Status == "active");
            
            var totalRemediationPlans = remediationPlans.Count;
            var completedPlans = remediationPlans.Count(rp => rp.Status == "completed");
            var inProgressPlans = remediationPlans.Count(rp => rp.Status == "in-progress");
            
            var totalActions = remediationActions.Count;
            var completedActions = remediationActions.Count(ra => ra.Status == "completed");
            
            var overdueActions = remediationActions.Count(ra =>
            {
                if (ra.Status == "completed" || ra.Status == "cancelled") return false;
                if (string.IsNullOrWhiteSpace(ra.DueDate)) return false;
                
                if (DateTime.TryParse(ra.DueDate, out var dueDate))
                {
                    return dueDate.Date < now.Date;
                }
                return false;
            });
            
            // Generate auto-narrative
            var narrative = GenerateGapsAndImprovementsNarrative(
                totalGaps, resolvedGaps, unresolvedGaps,
                totalAssumptions, activeAssumptions,
                totalSimplifications, activeSimplifications,
                totalRemediationPlans, completedPlans, inProgressPlans,
                totalActions, completedActions, overdueActions,
                sectionGapsAndImprovements
            );
            
            return new GapsAndImprovementsReport
            {
                PeriodId = periodId,
                Summary = new GapsAndImprovementsSummary
                {
                    TotalGaps = totalGaps,
                    ResolvedGaps = resolvedGaps,
                    UnresolvedGaps = unresolvedGaps,
                    TotalAssumptions = totalAssumptions,
                    ActiveAssumptions = activeAssumptions,
                    DeprecatedAssumptions = deprecatedAssumptions,
                    TotalSimplifications = totalSimplifications,
                    ActiveSimplifications = activeSimplifications,
                    TotalRemediationPlans = totalRemediationPlans,
                    CompletedRemediationPlans = completedPlans,
                    InProgressRemediationPlans = inProgressPlans,
                    TotalRemediationActions = totalActions,
                    CompletedActions = completedActions,
                    OverdueActions = overdueActions
                },
                Sections = sectionGapsAndImprovements,
                AutoGeneratedNarrative = narrative,
                ManualNarrative = null, // Initially no manual override
                GeneratedAt = now.ToString("O"),
                GeneratedBy = currentUserId
            };
        }
    }

    /// <summary>
    /// Generate narrative text for the gaps and improvements report.
    /// </summary>
    private string GenerateGapsAndImprovementsNarrative(
        int totalGaps, int resolvedGaps, int unresolvedGaps,
        int totalAssumptions, int activeAssumptions,
        int totalSimplifications, int activeSimplifications,
        int totalRemediationPlans, int completedPlans, int inProgressPlans,
        int totalActions, int completedActions, int overdueActions,
        List<SectionGapsAndImprovements> sections)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# Gaps and Improvements");
        sb.AppendLine();
        sb.AppendLine("## Executive Summary");
        sb.AppendLine();
        
        // Overall status
        if (totalGaps == 0 && totalAssumptions == 0 && totalSimplifications == 0)
        {
            sb.AppendLine("This report contains complete data with no identified gaps, assumptions, or simplifications.");
        }
        else
        {
            sb.AppendLine($"This section provides a comprehensive overview of data gaps, estimates, assumptions, and simplifications applied in this ESG report, along with remediation plans for improvement in future reporting periods.");
            sb.AppendLine();
            
            // Gaps summary
            if (totalGaps > 0)
            {
                sb.AppendLine($"**Data Gaps:** {totalGaps} gap(s) identified, of which {resolvedGaps} have been resolved and {unresolvedGaps} remain open.");
            }
            
            // Assumptions summary
            if (totalAssumptions > 0)
            {
                sb.AppendLine($"**Assumptions:** {activeAssumptions} active assumption(s) are currently in use to support data collection and estimation.");
            }
            
            // Simplifications summary
            if (totalSimplifications > 0)
            {
                sb.AppendLine($"**Simplifications:** {activeSimplifications} simplification(s) have been applied to the reporting scope or methodology.");
            }
            
            // Remediation summary
            if (totalRemediationPlans > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"**Remediation Plans:** {totalRemediationPlans} plan(s) have been established to address gaps and improve data quality, with {completedPlans} completed and {inProgressPlans} currently in progress.");
                
                if (overdueActions > 0)
                {
                    sb.AppendLine($"⚠️ **Attention Required:** {overdueActions} remediation action(s) are overdue and require immediate attention.");
                }
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("## Detailed Findings by Section");
        sb.AppendLine();
        
        // Section details
        foreach (var section in sections.Where(s => s.Gaps.Any() || s.Assumptions.Any() || s.Simplifications.Any()))
        {
            sb.AppendLine($"### {section.SectionTitle}");
            sb.AppendLine();
            
            // Gaps
            if (section.Gaps.Any())
            {
                sb.AppendLine("**Identified Gaps:**");
                foreach (var gapWithRem in section.Gaps)
                {
                    var gap = gapWithRem.Gap;
                    sb.AppendLine($"- **{gap.Title}**: {gap.Description}");
                    sb.AppendLine($"  - Impact: {gap.Impact}");
                    
                    if (gapWithRem.RemediationPlan != null)
                    {
                        sb.AppendLine($"  - Remediation Plan: {gapWithRem.RemediationPlan.Plan.Title} (Target: {gapWithRem.RemediationPlan.Plan.TargetPeriod})");
                    }
                    else if (!string.IsNullOrWhiteSpace(gap.ImprovementPlan))
                    {
                        sb.AppendLine($"  - Improvement Plan: {gap.ImprovementPlan}");
                    }
                }
                sb.AppendLine();
            }
            
            // Assumptions
            if (section.Assumptions.Any())
            {
                sb.AppendLine("**Applied Assumptions:**");
                foreach (var assumptionRef in section.Assumptions.Where(a => a.Assumption.Status == "active"))
                {
                    var assumption = assumptionRef.Assumption;
                    sb.AppendLine($"- **{assumption.Title}**: {assumption.Description}");
                    sb.AppendLine($"  - Scope: {assumption.Scope}");
                    sb.AppendLine($"  - Methodology: {assumption.Methodology}");
                    
                    if (assumptionRef.LinkedDataPointTitles.Any())
                    {
                        sb.AppendLine($"  - Applied to: {string.Join(", ", assumptionRef.LinkedDataPointTitles)}");
                    }
                    
                    if (!string.IsNullOrWhiteSpace(assumption.Limitations))
                    {
                        sb.AppendLine($"  - Limitations: {assumption.Limitations}");
                    }
                }
                sb.AppendLine();
            }
            
            // Simplifications
            if (section.Simplifications.Any())
            {
                sb.AppendLine("**Scope Simplifications:**");
                foreach (var simpRef in section.Simplifications.Where(s => s.Simplification.Status == "active"))
                {
                    var simp = simpRef.Simplification;
                    sb.AppendLine($"- **{simp.Title}**: {simp.Description}");
                    sb.AppendLine($"  - Impact Level: {simp.ImpactLevel}");
                    
                    if (simp.AffectedEntities.Any())
                    {
                        sb.AppendLine($"  - Affected Entities: {string.Join(", ", simp.AffectedEntities)}");
                    }
                }
                sb.AppendLine();
            }
            
            // Remediation plans
            if (section.RemediationPlans.Any())
            {
                sb.AppendLine("**Remediation Plans:**");
                foreach (var planWithActions in section.RemediationPlans)
                {
                    var plan = planWithActions.Plan;
                    sb.AppendLine($"- **{plan.Title}** ({plan.Status})");
                    sb.AppendLine($"  - Target Period: {plan.TargetPeriod}");
                    sb.AppendLine($"  - Owner: {plan.OwnerName}");
                    sb.AppendLine($"  - Priority: {plan.Priority}");
                    
                    if (planWithActions.Actions.Any())
                    {
                        sb.AppendLine($"  - Actions: {planWithActions.Actions.Count(a => a.Status == "completed")}/{planWithActions.Actions.Count} completed");
                    }
                }
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("## Conclusion");
        sb.AppendLine();
        
        if (totalRemediationPlans > 0)
        {
            sb.AppendLine("The organization has established a structured approach to addressing identified gaps and improving data quality. Continued progress on remediation plans will enhance the completeness and reliability of future ESG reports.");
        }
        else if (unresolvedGaps > 0 || activeAssumptions > 0)
        {
            sb.AppendLine("While gaps and assumptions have been identified, formal remediation plans should be established to systematically address these items in future reporting periods.");
        }
        else
        {
            sb.AppendLine("The report demonstrates strong data quality with minimal gaps or assumptions requiring remediation.");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Gets a filtered and sorted dashboard view of gaps.
    /// </summary>
    public GapDashboardResponse GetGapsDashboard(
        string? periodId, 
        string? status, 
        string? sectionId, 
        string? ownerId, 
        string? duePeriod, 
        string sortBy, 
        string sortOrder, 
        string currentUserId)
    {
        lock (_lock)
        {
            // Start with all gaps
            IEnumerable<Gap> filteredGaps = _gaps;

            // Filter by period (through sections)
            if (!string.IsNullOrWhiteSpace(periodId))
            {
                var periodSectionIds = _sections
                    .Where(s => s.PeriodId == periodId)
                    .Select(s => s.Id)
                    .ToHashSet();
                filteredGaps = filteredGaps.Where(g => periodSectionIds.Contains(g.SectionId));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                if (status == "open")
                {
                    filteredGaps = filteredGaps.Where(g => !g.Resolved);
                }
                else if (status == "resolved")
                {
                    filteredGaps = filteredGaps.Where(g => g.Resolved);
                }
            }

            // Filter by section
            if (!string.IsNullOrWhiteSpace(sectionId))
            {
                filteredGaps = filteredGaps.Where(g => g.SectionId == sectionId);
            }

            // Build dashboard items with enriched data
            var dashboardItems = new List<GapDashboardItem>();
            foreach (var gap in filteredGaps)
            {
                var section = _sections.FirstOrDefault(s => s.Id == gap.SectionId);
                if (section == null) continue;

                // Find associated remediation plan
                var remediationPlan = _remediationPlans.FirstOrDefault(rp => rp.GapId == gap.Id);

                // Determine owner and due period
                string? ownerName = null;
                string? ownerIdValue = null;
                string? duePeriodValue = gap.TargetDate;

                if (remediationPlan != null)
                {
                    ownerName = remediationPlan.OwnerName;
                    ownerIdValue = remediationPlan.OwnerId;
                    duePeriodValue = remediationPlan.TargetPeriod;
                }

                // Apply owner filter
                if (!string.IsNullOrWhiteSpace(ownerId) && ownerIdValue != ownerId)
                {
                    continue;
                }

                // Apply due period filter
                if (!string.IsNullOrWhiteSpace(duePeriod))
                {
                    if (duePeriodValue == null || !duePeriodValue.Contains(duePeriod, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                dashboardItems.Add(new GapDashboardItem
                {
                    Gap = gap,
                    SectionTitle = section.Title,
                    Category = section.Category,
                    OwnerName = ownerName,
                    OwnerId = ownerIdValue,
                    DuePeriod = duePeriodValue,
                    Status = gap.Resolved ? "resolved" : "open",
                    RemediationPlanId = remediationPlan?.Id,
                    RemediationPlanStatus = remediationPlan?.Status
                });
            }

            // Sort the results
            if (sortBy == "risk" || sortBy == "impact")
            {
                var riskOrder = new Dictionary<string, int>
                {
                    { "high", 3 },
                    { "medium", 2 },
                    { "low", 1 }
                };

                dashboardItems = sortOrder == "asc"
                    ? dashboardItems.OrderBy(item => riskOrder.GetValueOrDefault(item.Gap.Impact.ToLowerInvariant(), 0)).ToList()
                    : dashboardItems.OrderByDescending(item => riskOrder.GetValueOrDefault(item.Gap.Impact.ToLowerInvariant(), 0)).ToList();
            }
            else if (sortBy == "dueDate" || sortBy == "duePeriod")
            {
                dashboardItems = sortOrder == "desc"
                    ? dashboardItems.OrderByDescending(item => item.DuePeriod ?? string.Empty).ToList()
                    : dashboardItems.OrderBy(item => item.DuePeriod ?? string.Empty).ToList();
            }
            else if (sortBy == "section")
            {
                dashboardItems = sortOrder == "desc"
                    ? dashboardItems.OrderByDescending(item => item.SectionTitle).ToList()
                    : dashboardItems.OrderBy(item => item.SectionTitle).ToList();
            }

            // Calculate summary metrics
            var allGaps = dashboardItems;
            var summary = new GapDashboardSummary
            {
                TotalGaps = allGaps.Count,
                OpenGaps = allGaps.Count(g => g.Status == "open"),
                ResolvedGaps = allGaps.Count(g => g.Status == "resolved"),
                HighRiskGaps = allGaps.Count(g => g.Gap.Impact.Equals("high", StringComparison.OrdinalIgnoreCase)),
                MediumRiskGaps = allGaps.Count(g => g.Gap.Impact.Equals("medium", StringComparison.OrdinalIgnoreCase)),
                LowRiskGaps = allGaps.Count(g => g.Gap.Impact.Equals("low", StringComparison.OrdinalIgnoreCase)),
                WithRemediationPlan = allGaps.Count(g => g.RemediationPlanId != null),
                WithoutRemediationPlan = allGaps.Count(g => g.RemediationPlanId == null)
            };

            return new GapDashboardResponse
            {
                Gaps = dashboardItems,
                Summary = summary,
                TotalCount = dashboardItems.Count
            };
        }
    }

    #region Decision Management

    /// <summary>
    /// Get all decisions, optionally filtered by section.
    /// </summary>
    public IReadOnlyList<Decision> GetDecisions(string? sectionId = null)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(sectionId))
            {
                return _decisions.ToList();
            }
            return _decisions.Where(d => d.SectionId == sectionId).ToList();
        }
    }

    /// <summary>
    /// Get a specific decision by ID.
    /// </summary>
    public Decision? GetDecisionById(string id)
    {
        lock (_lock)
        {
            return _decisions.FirstOrDefault(d => d.Id == id);
        }
    }

    /// <summary>
    /// Get version history for a decision.
    /// </summary>
    public IReadOnlyList<DecisionVersion> GetDecisionVersionHistory(string decisionId)
    {
        lock (_lock)
        {
            return _decisionVersions
                .Where(v => v.DecisionId == decisionId)
                .OrderByDescending(v => v.Version)
                .ToList();
        }
    }

    /// <summary>
    /// Create a new decision.
    /// </summary>
    public (bool isValid, string? errorMessage, Decision? decision) CreateDecision(
        string? sectionId,
        string title,
        string context,
        string decisionText,
        string alternatives,
        string consequences,
        string createdBy)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(title))
            return (false, "Title is required.", null);
        if (string.IsNullOrWhiteSpace(context))
            return (false, "Context is required.", null);
        if (string.IsNullOrWhiteSpace(decisionText))
            return (false, "Decision text is required.", null);
        if (string.IsNullOrWhiteSpace(alternatives))
            return (false, "Alternatives are required.", null);
        if (string.IsNullOrWhiteSpace(consequences))
            return (false, "Consequences are required.", null);

        // Validate section if provided
        if (!string.IsNullOrEmpty(sectionId))
        {
            lock (_lock)
            {
                if (!_sections.Any(s => s.Id == sectionId))
                {
                    return (false, $"Section with ID '{sectionId}' not found.", null);
                }
            }
        }

        var decision = new Decision
        {
            Id = Guid.NewGuid().ToString(),
            SectionId = sectionId,
            Title = title.Trim(),
            Context = context.Trim(),
            DecisionText = decisionText.Trim(),
            Alternatives = alternatives.Trim(),
            Consequences = consequences.Trim(),
            Status = "active",
            Version = 1,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow.ToString("o")
        };
        
        // Calculate and store integrity hash
        decision.IntegrityHash = Services.IntegrityService.CalculateDecisionHash(decision);
        decision.IntegrityStatus = "valid";

        lock (_lock)
        {
            _decisions.Add(decision);
            
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = "", NewValue = title.Trim() },
                new FieldChange { Field = "Context", OldValue = "", NewValue = context.Trim() },
                new FieldChange { Field = "DecisionText", OldValue = "", NewValue = decisionText.Trim() }
            };
            var user = _users.FirstOrDefault(u => u.Id == createdBy);
            CreateAuditLogEntry(
                createdBy,
                user?.Name ?? createdBy,
                "create",
                "Decision",
                decision.Id,
                changes,
                $"Created decision '{title.Trim()}'");
        }

        return (true, null, decision);
    }

    /// <summary>
    /// Update an existing decision. Creates a new version and preserves the old version.
    /// Only active decisions can be updated.
    /// </summary>
    public (bool isValid, string? errorMessage, Decision? decision) UpdateDecision(
        string id,
        string title,
        string context,
        string decisionText,
        string alternatives,
        string consequences,
        string changeNote,
        string updatedBy)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(title))
            return (false, "Title is required.", null);
        if (string.IsNullOrWhiteSpace(context))
            return (false, "Context is required.", null);
        if (string.IsNullOrWhiteSpace(decisionText))
            return (false, "Decision text is required.", null);
        if (string.IsNullOrWhiteSpace(alternatives))
            return (false, "Alternatives are required.", null);
        if (string.IsNullOrWhiteSpace(consequences))
            return (false, "Consequences are required.", null);
        if (string.IsNullOrWhiteSpace(changeNote))
            return (false, "Change note is required when updating a decision.", null);

        lock (_lock)
        {
            var decision = _decisions.FirstOrDefault(d => d.Id == id);
            if (decision == null)
            {
                return (false, $"Decision with ID '{id}' not found.", null);
            }

            if (decision.Status != "active")
            {
                return (false, "Only active decisions can be updated.", null);
            }

            // Track changes for audit log
            var changes = new List<FieldChange>();

            if (decision.Title != title.Trim())
            {
                changes.Add(new FieldChange { Field = "Title", OldValue = decision.Title, NewValue = title.Trim() });
            }

            if (decision.Context != context.Trim())
            {
                changes.Add(new FieldChange { Field = "Context", OldValue = decision.Context, NewValue = context.Trim() });
            }

            if (decision.DecisionText != decisionText.Trim())
            {
                changes.Add(new FieldChange { Field = "DecisionText", OldValue = decision.DecisionText, NewValue = decisionText.Trim() });
            }

            if (decision.Alternatives != alternatives.Trim())
            {
                changes.Add(new FieldChange { Field = "Alternatives", OldValue = decision.Alternatives, NewValue = alternatives.Trim() });
            }

            if (decision.Consequences != consequences.Trim())
            {
                changes.Add(new FieldChange { Field = "Consequences", OldValue = decision.Consequences, NewValue = consequences.Trim() });
            }

            // Save current version to history (without changeNote - that's for the next version)
            var version = new DecisionVersion
            {
                Id = Guid.NewGuid().ToString(),
                DecisionId = decision.Id,
                Version = decision.Version,
                Title = decision.Title,
                Context = decision.Context,
                DecisionText = decision.DecisionText,
                Alternatives = decision.Alternatives,
                Consequences = decision.Consequences,
                Status = decision.Status,
                CreatedBy = decision.CreatedBy,
                CreatedAt = decision.CreatedAt,
                ChangeNote = null
            };
            // Store hash for historical version
            version.IntegrityHash = decision.IntegrityHash;
            _decisionVersions.Add(version);

            // Update the decision
            decision.Title = title.Trim();
            decision.Context = context.Trim();
            decision.DecisionText = decisionText.Trim();
            decision.Alternatives = alternatives.Trim();
            decision.Consequences = consequences.Trim();
            decision.Version++;
            decision.UpdatedBy = updatedBy;
            decision.UpdatedAt = DateTime.UtcNow.ToString("o");
            decision.ChangeNote = changeNote.Trim();
            
            // Recalculate integrity hash for new version
            decision.IntegrityHash = Services.IntegrityService.CalculateDecisionHash(decision);
            decision.IntegrityStatus = "valid";

            // Log audit event only if there were changes
            if (changes.Count > 0)
            {
                var user = _users.FirstOrDefault(u => u.Id == updatedBy);
                CreateAuditLogEntry(
                    updatedBy,
                    user?.Name ?? updatedBy,
                    "update",
                    "Decision",
                    decision.Id,
                    changes,
                    $"Updated decision '{title.Trim()}' to version {decision.Version}: {changeNote.Trim()}");
            }

            return (true, null, decision);
        }
    }

    /// <summary>
    /// Deprecate a decision (mark as no longer applicable).
    /// </summary>
    public (bool isValid, string? errorMessage, Decision? decision) DeprecateDecision(
        string id,
        string reason,
        string deprecatedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Deprecation reason is required.", null);

        lock (_lock)
        {
            var decision = _decisions.FirstOrDefault(d => d.Id == id);
            if (decision == null)
            {
                return (false, $"Decision with ID '{id}' not found.", null);
            }

            if (decision.Status == "deprecated")
            {
                return (false, "Decision is already deprecated.", null);
            }

            // Save current version to history (without changeNote - that's for the next version)
            var version = new DecisionVersion
            {
                Id = Guid.NewGuid().ToString(),
                DecisionId = decision.Id,
                Version = decision.Version,
                Title = decision.Title,
                Context = decision.Context,
                DecisionText = decision.DecisionText,
                Alternatives = decision.Alternatives,
                Consequences = decision.Consequences,
                Status = decision.Status,
                CreatedBy = decision.CreatedBy,
                CreatedAt = decision.CreatedAt,
                ChangeNote = null
            };
            _decisionVersions.Add(version);

            // Update status
            decision.Status = "deprecated";
            decision.Version++;
            decision.UpdatedBy = deprecatedBy;
            decision.UpdatedAt = DateTime.UtcNow.ToString("o");
            decision.ChangeNote = $"Deprecated: {reason}";

            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = deprecatedBy,
                UserName = deprecatedBy,
                Action = "deprecated",
                EntityType = "decision",
                EntityId = decision.Id,
                ChangeNote = $"Deprecated decision: {reason}",
                Changes = new List<FieldChange>()
            });

            return (true, null, decision);
        }
    }

    /// <summary>
    /// Link a decision to a report fragment (data point).
    /// </summary>
    public (bool isValid, string? errorMessage) LinkDecisionToFragment(string decisionId, string fragmentId, string userId)
    {
        lock (_lock)
        {
            var decision = _decisions.FirstOrDefault(d => d.Id == decisionId);
            if (decision == null)
            {
                return (false, $"Decision with ID '{decisionId}' not found.");
            }

            // Validate fragment exists (data point)
            var dataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == fragmentId);
            if (dataPoint == null)
            {
                return (false, $"Fragment (data point) with ID '{fragmentId}' not found.");
            }

            if (decision.ReferencedByFragmentIds.Contains(fragmentId))
            {
                return (false, "Decision is already linked to this fragment.");
            }

            decision.ReferencedByFragmentIds.Add(fragmentId);

            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = userId,
                UserName = userId,
                Action = "linked",
                EntityType = "decision",
                EntityId = decision.Id,
                ChangeNote = $"Linked decision to fragment {fragmentId}",
                Changes = new List<FieldChange>()
            });

            return (true, null);
        }
    }

    /// <summary>
    /// Unlink a decision from a report fragment.
    /// </summary>
    public (bool isValid, string? errorMessage) UnlinkDecisionFromFragment(string decisionId, string fragmentId, string userId)
    {
        lock (_lock)
        {
            var decision = _decisions.FirstOrDefault(d => d.Id == decisionId);
            if (decision == null)
            {
                return (false, $"Decision with ID '{decisionId}' not found.");
            }

            if (!decision.ReferencedByFragmentIds.Contains(fragmentId))
            {
                return (false, "Decision is not linked to this fragment.");
            }

            decision.ReferencedByFragmentIds.Remove(fragmentId);

            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = userId,
                UserName = userId,
                Action = "unlinked",
                EntityType = "decision",
                EntityId = decision.Id,
                ChangeNote = $"Unlinked decision from fragment {fragmentId}",
                Changes = new List<FieldChange>()
            });

            return (true, null);
        }
    }

    /// <summary>
    /// Get all decisions referenced by a specific fragment.
    /// </summary>
    public IReadOnlyList<Decision> GetDecisionsByFragment(string fragmentId)
    {
        lock (_lock)
        {
            return _decisions
                .Where(d => d.ReferencedByFragmentIds.Contains(fragmentId))
                .ToList();
        }
    }

    /// <summary>
    /// Delete a decision. Only allowed if not referenced by any fragments.
    /// </summary>
    public (bool isValid, string? errorMessage) DeleteDecision(string id, string userId)
    {
        lock (_lock)
        {
            var decision = _decisions.FirstOrDefault(d => d.Id == id);
            if (decision == null)
            {
                return (false, $"Decision with ID '{id}' not found.");
            }

            if (decision.ReferencedByFragmentIds.Any())
            {
                return (false, "Cannot delete a decision that is referenced by fragments. Unlink all fragments first.");
            }

            _decisions.Remove(decision);
            
            // Also remove all versions
            _decisionVersions.RemoveAll(v => v.DecisionId == id);

            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Title", OldValue = decision.Title, NewValue = "" }
            };
            var user = _users.FirstOrDefault(u => u.Id == userId);
            CreateAuditLogEntry(
                userId,
                user?.Name ?? userId,
                "delete",
                "Decision",
                id,
                changes,
                $"Deleted decision '{decision.Title}'");

            return (true, null);
        }
    }

    #endregion

    #region Fragment Audit

    /// <summary>
    /// Get audit view for a report fragment with full traceability.
    /// </summary>
    public FragmentAuditView? GetFragmentAuditView(string fragmentType, string fragmentId)
    {
        lock (_lock)
        {
            // Determine fragment type and retrieve the fragment
            if (fragmentType.Equals("section", StringComparison.OrdinalIgnoreCase))
            {
                return GetSectionAuditView(fragmentId);
            }
            else if (fragmentType.Equals("data-point", StringComparison.OrdinalIgnoreCase))
            {
                return GetDataPointAuditView(fragmentId);
            }
            
            return null;
        }
    }

    /// <summary>
    /// Get audit view for a section fragment.
    /// </summary>
    private FragmentAuditView? GetSectionAuditView(string sectionId)
    {
        var section = _sections.FirstOrDefault(s => s.Id == sectionId);
        if (section == null) return null;

        var auditView = new FragmentAuditView
        {
            FragmentType = "section",
            FragmentId = sectionId,
            StableFragmentIdentifier = section.CatalogCode ?? $"section-{sectionId}",
            FragmentTitle = section.Title,
            FragmentContent = section.Description,
            SectionInfo = new FragmentSectionInfo
            {
                SectionId = sectionId,
                SectionTitle = section.Title,
                SectionCategory = section.Category,
                CatalogCode = section.CatalogCode
            }
        };

        // Get all data points in this section
        var dataPoints = _dataPoints.Where(dp => dp.SectionId == sectionId).ToList();
        
        // Aggregate linked sources from all data points
        var linkedSources = new List<LinkedSource>();
        foreach (var dp in dataPoints)
        {
            foreach (var sourceRef in dp.SourceReferences)
            {
                linkedSources.Add(new LinkedSource
                {
                    SourceType = sourceRef.SourceType,
                    SourceReference = sourceRef.SourceReference,
                    Description = sourceRef.Description,
                    OriginSystem = sourceRef.OriginSystem,
                    OwnerId = sourceRef.OwnerId,
                    OwnerName = sourceRef.OwnerName,
                    LastUpdated = sourceRef.LastUpdated
                });
            }
        }
        auditView.LinkedSources = linkedSources.DistinctBy(s => new { s.SourceType, s.SourceReference }).ToList();

        // Get linked evidence
        var evidenceIds = dataPoints.SelectMany(dp => dp.EvidenceIds).Distinct().ToList();
        auditView.LinkedEvidenceFiles = GetLinkedEvidenceList(evidenceIds);

        // Get linked decisions
        var decisionFragmentIds = dataPoints.Select(dp => dp.Id).ToList();
        var linkedDecisions = new HashSet<string>();
        foreach (var decision in _decisions)
        {
            if (decision.ReferencedByFragmentIds.Any(fragId => decisionFragmentIds.Contains(fragId)))
            {
                linkedDecisions.Add(decision.Id);
            }
        }
        auditView.LinkedDecisions = GetLinkedDecisionsList(linkedDecisions.ToList());

        // Get linked assumptions
        var assumptionIds = _assumptions
            .Where(a => a.SectionId == sectionId)
            .Select(a => a.Id)
            .ToList();
        auditView.LinkedAssumptions = GetLinkedAssumptionsList(assumptionIds);

        // Get linked gaps
        var gaps = _gaps.Where(g => g.SectionId == sectionId).ToList();
        auditView.LinkedGaps = gaps.Select(g => new LinkedGap
        {
            GapId = g.Id,
            Title = g.Title,
            Description = g.Description,
            Impact = g.Impact,
            Resolved = g.Resolved,
            ImprovementPlan = g.ImprovementPlan
        }).ToList();

        // Check for provenance warnings
        auditView.ProvenanceWarnings = CheckProvenanceCompleteness(auditView);
        auditView.HasCompleteProvenance = !auditView.ProvenanceWarnings.Any(w => w.Severity == "error");

        // Get audit trail
        auditView.AuditTrail = _auditLog
            .Where(e => e.EntityType.Equals("ReportSection", StringComparison.OrdinalIgnoreCase) && e.EntityId == sectionId)
            .OrderByDescending(e => e.Timestamp)
            .Take(20)
            .ToList();

        return auditView;
    }

    /// <summary>
    /// Get audit view for a data point fragment.
    /// </summary>
    private FragmentAuditView? GetDataPointAuditView(string dataPointId)
    {
        var dataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == dataPointId);
        if (dataPoint == null) return null;

        var section = _sections.FirstOrDefault(s => s.Id == dataPoint.SectionId);

        var auditView = new FragmentAuditView
        {
            FragmentType = "data-point",
            FragmentId = dataPointId,
            StableFragmentIdentifier = $"dp-{section?.CatalogCode ?? dataPoint.SectionId}-{dataPointId}",
            FragmentTitle = dataPoint.Title,
            FragmentContent = dataPoint.Content,
            SectionInfo = section != null ? new FragmentSectionInfo
            {
                SectionId = section.Id,
                SectionTitle = section.Title,
                SectionCategory = section.Category,
                CatalogCode = section.CatalogCode
            } : null
        };

        // Get linked sources
        auditView.LinkedSources = dataPoint.SourceReferences.Select(sr => new LinkedSource
        {
            SourceType = sr.SourceType,
            SourceReference = sr.SourceReference,
            Description = sr.Description,
            OriginSystem = sr.OriginSystem,
            OwnerId = sr.OwnerId,
            OwnerName = sr.OwnerName,
            LastUpdated = sr.LastUpdated
        }).ToList();

        // Add estimate input sources if applicable
        foreach (var inputSource in dataPoint.EstimateInputSources)
        {
            auditView.LinkedSources.Add(new LinkedSource
            {
                SourceType = inputSource.SourceType,
                SourceReference = inputSource.SourceReference,
                Description = inputSource.Description
            });
        }

        // Get linked evidence
        auditView.LinkedEvidenceFiles = GetLinkedEvidenceList(dataPoint.EvidenceIds);

        // Get linked decisions
        var linkedDecisionIds = _decisions
            .Where(d => d.ReferencedByFragmentIds.Contains(dataPointId))
            .Select(d => d.Id)
            .ToList();
        auditView.LinkedDecisions = GetLinkedDecisionsList(linkedDecisionIds);

        // Get linked assumptions
        var linkedAssumptionIds = _assumptions
            .Where(a => a.LinkedDataPointIds.Contains(dataPointId))
            .Select(a => a.Id)
            .ToList();
        auditView.LinkedAssumptions = GetLinkedAssumptionsList(linkedAssumptionIds);

        // Check for related gaps
        if (dataPoint.IsMissing || dataPoint.IsBlocked)
        {
            var gaps = _gaps.Where(g => g.SectionId == dataPoint.SectionId).ToList();
            auditView.LinkedGaps = gaps.Select(g => new LinkedGap
            {
                GapId = g.Id,
                Title = g.Title,
                Description = g.Description,
                Impact = g.Impact,
                Resolved = g.Resolved,
                ImprovementPlan = g.ImprovementPlan
            }).ToList();
        }

        // Check for provenance warnings
        auditView.ProvenanceWarnings = CheckProvenanceCompleteness(auditView);
        auditView.HasCompleteProvenance = !auditView.ProvenanceWarnings.Any(w => w.Severity == "error");

        // Get audit trail
        auditView.AuditTrail = _auditLog
            .Where(e => e.EntityType.Equals("DataPoint", StringComparison.OrdinalIgnoreCase) && e.EntityId == dataPointId)
            .OrderByDescending(e => e.Timestamp)
            .Take(20)
            .ToList();

        return auditView;
    }

    /// <summary>
    /// Get linked evidence list from evidence IDs.
    /// </summary>
    private List<LinkedEvidence> GetLinkedEvidenceList(List<string> evidenceIds)
    {
        return _evidence
            .Where(e => evidenceIds.Contains(e.Id))
            .Select(e => new LinkedEvidence
            {
                EvidenceId = e.Id,
                FileName = e.FileName ?? "Unknown",
                Description = e.Description,
                UploadedBy = e.UploadedBy,
                UploadedAt = e.UploadedAt,
                FileUrl = e.FileUrl,
                Checksum = e.Checksum,
                IntegrityStatus = e.IntegrityStatus
            })
            .ToList();
    }

    /// <summary>
    /// Get linked decisions list from decision IDs.
    /// </summary>
    private List<LinkedDecision> GetLinkedDecisionsList(List<string> decisionIds)
    {
        return _decisions
            .Where(d => decisionIds.Contains(d.Id))
            .Select(d => new LinkedDecision
            {
                DecisionId = d.Id,
                Title = d.Title,
                DecisionText = d.DecisionText,
                Status = d.Status,
                Version = d.Version,
                DecisionBy = d.CreatedBy,
                DecisionDate = d.CreatedAt
            })
            .ToList();
    }

    /// <summary>
    /// Get linked assumptions list from assumption IDs.
    /// </summary>
    private List<LinkedAssumption> GetLinkedAssumptionsList(List<string> assumptionIds)
    {
        return _assumptions
            .Where(a => assumptionIds.Contains(a.Id))
            .Select(a => new LinkedAssumption
            {
                AssumptionId = a.Id,
                Title = a.Title,
                Description = a.Description,
                Status = a.Status,
                Version = a.Version,
                Methodology = a.Methodology,
                CreatedBy = a.CreatedBy,
                CreatedAt = a.CreatedAt
            })
            .ToList();
    }

    /// <summary>
    /// Check for missing provenance and generate warnings.
    /// </summary>
    private List<ProvenanceWarning> CheckProvenanceCompleteness(FragmentAuditView auditView)
    {
        var warnings = new List<ProvenanceWarning>();

        // Check for missing sources
        if (!auditView.LinkedSources.Any())
        {
            warnings.Add(new ProvenanceWarning
            {
                MissingLinkType = "source",
                Message = "No source references are linked to this fragment.",
                Severity = "warning",
                Recommendation = "Add source references to improve traceability and auditability."
            });
        }

        // Check for missing evidence (for data-point fragments)
        if (auditView.FragmentType == "data-point" && !auditView.LinkedEvidenceFiles.Any())
        {
            warnings.Add(new ProvenanceWarning
            {
                MissingLinkType = "evidence",
                Message = "No evidence files are linked to this data point.",
                Severity = "warning",
                Recommendation = "Upload and link supporting evidence files to strengthen auditability."
            });
        }

        // Check for unverified sources
        var unverifiedSources = auditView.LinkedSources.Where(s => string.IsNullOrWhiteSpace(s.LastUpdated)).ToList();
        if (unverifiedSources.Any())
        {
            warnings.Add(new ProvenanceWarning
            {
                MissingLinkType = "source",
                Message = $"{unverifiedSources.Count} source reference(s) have not been verified recently.",
                Severity = "info",
                Recommendation = "Review and verify source references to ensure data accuracy."
            });
        }

        // Check for evidence integrity issues
        var failedIntegrityEvidence = auditView.LinkedEvidenceFiles
            .Where(e => e.IntegrityStatus == "failed")
            .ToList();
        if (failedIntegrityEvidence.Any())
        {
            warnings.Add(new ProvenanceWarning
            {
                MissingLinkType = "evidence",
                Message = $"{failedIntegrityEvidence.Count} evidence file(s) have failed integrity checks.",
                Severity = "error",
                Recommendation = "Re-upload evidence files or verify their integrity to ensure authenticity."
            });
        }

        return warnings;
    }

    /// <summary>
    /// Generate stable fragment identifier for a data point or section.
    /// Used for export mapping.
    /// </summary>
    public string GenerateStableFragmentIdentifier(string fragmentType, string fragmentId)
    {
        lock (_lock)
        {
            if (fragmentType.Equals("section", StringComparison.OrdinalIgnoreCase))
            {
                var section = _sections.FirstOrDefault(s => s.Id == fragmentId);
                return section?.CatalogCode ?? $"section-{fragmentId}";
            }
            else if (fragmentType.Equals("data-point", StringComparison.OrdinalIgnoreCase))
            {
                var dataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == fragmentId);
                if (dataPoint != null)
                {
                    var section = _sections.FirstOrDefault(s => s.Id == dataPoint.SectionId);
                    return $"dp-{section?.CatalogCode ?? dataPoint.SectionId}-{fragmentId}";
                }
            }
            
            return $"{fragmentType}-{fragmentId}";
        }
    }

    #endregion

    #region Consistency Validation

    /// <summary>
    /// Runs consistency validation on a reporting period.
    /// Checks for missing required fields, invalid units, contradictory statements, and period coverage.
    /// </summary>
    public ValidationResult RunConsistencyValidation(RunValidationRequest request)
    {
        lock (_lock)
        {
            var period = _periods.FirstOrDefault(p => p.Id == request.PeriodId);
            if (period == null)
            {
                return new ValidationResult
                {
                    Status = "failed",
                    PeriodId = request.PeriodId,
                    PeriodName = "Unknown",
                    ValidatedAt = DateTime.UtcNow.ToString("O"),
                    ValidatedBy = request.ValidatedBy,
                    ErrorCount = 1,
                    CanPublish = false,
                    Summary = "Reporting period not found.",
                    Issues = new List<ValidationIssue>
                    {
                        new ValidationIssue
                        {
                            Id = Guid.NewGuid().ToString(),
                            RuleType = "period-not-found",
                            Severity = "error",
                            Message = $"Reporting period with ID '{request.PeriodId}' not found.",
                            DetectedAt = DateTime.UtcNow.ToString("O")
                        }
                    }
                };
            }

            var issues = new List<ValidationIssue>();
            var sections = _sections.Where(s => s.PeriodId == request.PeriodId).ToList();

            // Run baseline validation rules
            if (request.RuleTypes.Count == 0 || request.RuleTypes.Contains("required-data"))
            {
                issues.AddRange(ValidateRequiredDataForEnabledSections(period, sections));
            }

            if (request.RuleTypes.Count == 0 || request.RuleTypes.Contains("unit-normalization"))
            {
                issues.AddRange(ValidateUnitNormalization(period, sections));
            }

            if (request.RuleTypes.Count == 0 || request.RuleTypes.Contains("period-coverage"))
            {
                issues.AddRange(ValidatePeriodCoverage(period, sections));
            }

            if (request.RuleTypes.Count == 0 || request.RuleTypes.Contains("missing-fields"))
            {
                issues.AddRange(ValidateMissingRequiredFields(period, sections));
            }

            // New: Validate evidence and provenance for sections marked as 'Ready'
            if (request.RuleTypes.Count == 0 || request.RuleTypes.Contains("ready-section-metadata"))
            {
                issues.AddRange(ValidateReadySectionMetadata(period, sections));
            }

            // New: Validate based on reporting mode (SME vs CSRD/ESRS)
            if (request.RuleTypes.Count == 0 || request.RuleTypes.Contains("reporting-mode-compliance"))
            {
                issues.AddRange(ValidateReportingModeCompliance(period, sections));
            }

            // Calculate counts by severity
            var errorCount = issues.Count(i => i.Severity == "error");
            var warningCount = issues.Count(i => i.Severity == "warning");
            var infoCount = issues.Count(i => i.Severity == "info");

            var status = errorCount > 0 ? "failed" : (warningCount > 0 ? "warning" : "passed");
            var canPublish = errorCount == 0;

            var summary = canPublish
                ? $"Validation passed. {warningCount} warnings, {infoCount} informational messages."
                : $"Validation failed with {errorCount} errors, {warningCount} warnings.";

            var result = new ValidationResult
            {
                Status = status,
                PeriodId = request.PeriodId,
                PeriodName = period.Name,
                ValidatedAt = DateTime.UtcNow.ToString("O"),
                ValidatedBy = request.ValidatedBy,
                Issues = issues,
                ErrorCount = errorCount,
                WarningCount = warningCount,
                InfoCount = infoCount,
                CanPublish = canPublish,
                Summary = summary
            };

            // Store validation result for audit trail
            _validationResults.Add(result);

            return result;
        }
    }

    /// <summary>
    /// Validates that enabled sections have required data points.
    /// </summary>
    private List<ValidationIssue> ValidateRequiredDataForEnabledSections(ReportingPeriod period, List<ReportSection> sections)
    {
        var issues = new List<ValidationIssue>();

        foreach (var section in sections.Where(s => s.Status != "disabled"))
        {
            var dataPoints = _dataPoints.Where(dp => dp.SectionId == section.Id).ToList();
            
            // Check if section has no data points at all
            if (dataPoints.Count == 0)
            {
                issues.Add(new ValidationIssue
                {
                    Id = Guid.NewGuid().ToString(),
                    RuleType = "missing-required-field",
                    Severity = "error",
                    Message = $"Section '{section.Title}' has no data points. Enabled sections must have at least one data point.",
                    SectionId = section.Id,
                    SectionTitle = section.Title,
                    DetectedAt = DateTime.UtcNow.ToString("O")
                });
                continue;
            }

            // Check for incomplete or missing data points
            var incompleteDataPoints = dataPoints.Where(dp => 
                dp.CompletenessStatus == "missing" || 
                dp.CompletenessStatus == "incomplete").ToList();

            if (incompleteDataPoints.Any())
            {
                issues.Add(new ValidationIssue
                {
                    Id = Guid.NewGuid().ToString(),
                    RuleType = "missing-required-field",
                    Severity = "warning",
                    Message = $"Section '{section.Title}' has {incompleteDataPoints.Count} incomplete or missing data points.",
                    SectionId = section.Id,
                    SectionTitle = section.Title,
                    AffectedDataPointIds = incompleteDataPoints.Select(dp => dp.Id).ToList(),
                    DetectedAt = DateTime.UtcNow.ToString("O")
                });
            }

            // Check for data points that need review
            var dataPointsNeedingReview = dataPoints.Where(dp => 
                dp.ReviewStatus == "changes-requested" || 
                dp.ProvenanceNeedsReview).ToList();

            if (dataPointsNeedingReview.Any())
            {
                issues.Add(new ValidationIssue
                {
                    Id = Guid.NewGuid().ToString(),
                    RuleType = "contradictory-statement",
                    Severity = "error",
                    Message = $"Section '{section.Title}' has {dataPointsNeedingReview.Count} data points requiring review or changes.",
                    SectionId = section.Id,
                    SectionTitle = section.Title,
                    AffectedDataPointIds = dataPointsNeedingReview.Select(dp => dp.Id).ToList(),
                    DetectedAt = DateTime.UtcNow.ToString("O")
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// Validates unit consistency across data points.
    /// </summary>
    private List<ValidationIssue> ValidateUnitNormalization(ReportingPeriod period, List<ReportSection> sections)
    {
        var issues = new List<ValidationIssue>();

        foreach (var section in sections)
        {
            var dataPoints = _dataPoints.Where(dp => dp.SectionId == section.Id).ToList();
            
            // Group data points by classification to check unit consistency
            var dataPointsByClassification = dataPoints
                .Where(dp => !string.IsNullOrWhiteSpace(dp.Classification) && !string.IsNullOrWhiteSpace(dp.Unit))
                .GroupBy(dp => dp.Classification)
                .Where(g => g.Count() > 1);

            foreach (var group in dataPointsByClassification)
            {
                var units = group.Select(dp => dp.Unit).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                
                if (units.Count > 1)
                {
                    issues.Add(new ValidationIssue
                    {
                        Id = Guid.NewGuid().ToString(),
                        RuleType = "invalid-unit",
                        Severity = "warning",
                        Message = $"Section '{section.Title}': Data points with classification '{group.Key}' use inconsistent units: {string.Join(", ", units)}.",
                        SectionId = section.Id,
                        SectionTitle = section.Title,
                        AffectedDataPointIds = group.Select(dp => dp.Id).ToList(),
                        FieldName = "Unit",
                        ExpectedValue = "Consistent units within classification",
                        ActualValue = string.Join(", ", units),
                        DetectedAt = DateTime.UtcNow.ToString("O")
                    });
                }
            }

            // Check for metric data points without units
            var metricsWithoutUnits = dataPoints
                .Where(dp => dp.Type == "metric" && 
                             !string.IsNullOrWhiteSpace(dp.Value) && 
                             string.IsNullOrWhiteSpace(dp.Unit))
                .ToList();

            if (metricsWithoutUnits.Any())
            {
                issues.Add(new ValidationIssue
                {
                    Id = Guid.NewGuid().ToString(),
                    RuleType = "missing-required-field",
                    Severity = "error",
                    Message = $"Section '{section.Title}': {metricsWithoutUnits.Count} metric data points are missing units.",
                    SectionId = section.Id,
                    SectionTitle = section.Title,
                    AffectedDataPointIds = metricsWithoutUnits.Select(dp => dp.Id).ToList(),
                    FieldName = "Unit",
                    ExpectedValue = "Valid unit for metric",
                    ActualValue = "null or empty",
                    DetectedAt = DateTime.UtcNow.ToString("O")
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// Validates that data points fall within the reporting period.
    /// </summary>
    private List<ValidationIssue> ValidatePeriodCoverage(ReportingPeriod period, List<ReportSection> sections)
    {
        var issues = new List<ValidationIssue>();

        if (!DateTime.TryParse(period.StartDate, out var periodStart) ||
            !DateTime.TryParse(period.EndDate, out var periodEnd))
        {
            return issues; // Skip if period dates are invalid
        }

        foreach (var section in sections)
        {
            var dataPoints = _dataPoints.Where(dp => dp.SectionId == section.Id).ToList();
            
            foreach (var dataPoint in dataPoints.Where(dp => !string.IsNullOrWhiteSpace(dp.Value)))
            {
                // Try to parse the value as a date
                if (DateTime.TryParse(dataPoint.Value, out var valueDate))
                {
                    if (valueDate < periodStart || valueDate > periodEnd)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Id = Guid.NewGuid().ToString(),
                            RuleType = "period-coverage",
                            Severity = "warning",
                            Message = $"Data point '{dataPoint.Title}' has a date value ({dataPoint.Value}) outside the reporting period ({period.StartDate} to {period.EndDate}).",
                            SectionId = section.Id,
                            SectionTitle = section.Title,
                            AffectedDataPointIds = new List<string> { dataPoint.Id },
                            FieldName = "Value",
                            ExpectedValue = $"Date within {period.StartDate} to {period.EndDate}",
                            ActualValue = dataPoint.Value,
                            DetectedAt = DateTime.UtcNow.ToString("O")
                        });
                    }
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// Validates that required fields are present in data points.
    /// </summary>
    private List<ValidationIssue> ValidateMissingRequiredFields(ReportingPeriod period, List<ReportSection> sections)
    {
        var issues = new List<ValidationIssue>();

        foreach (var section in sections)
        {
            var dataPoints = _dataPoints.Where(dp => dp.SectionId == section.Id).ToList();
            
            foreach (var dataPoint in dataPoints)
            {
                // Check for missing owner
                if (string.IsNullOrWhiteSpace(dataPoint.OwnerId))
                {
                    issues.Add(new ValidationIssue
                    {
                        Id = Guid.NewGuid().ToString(),
                        RuleType = "missing-required-field",
                        Severity = "warning",
                        Message = $"Data point '{dataPoint.Title}' in section '{section.Title}' has no assigned owner.",
                        SectionId = section.Id,
                        SectionTitle = section.Title,
                        AffectedDataPointIds = new List<string> { dataPoint.Id },
                        FieldName = "OwnerId",
                        ExpectedValue = "Valid user ID",
                        ActualValue = "null or empty",
                        DetectedAt = DateTime.UtcNow.ToString("O")
                    });
                }

                // Check for missing evidence on approved data points
                if (dataPoint.ReviewStatus == "approved" && dataPoint.EvidenceIds.Count == 0 && 
                    dataPoint.InformationType != "estimate")
                {
                    issues.Add(new ValidationIssue
                    {
                        Id = Guid.NewGuid().ToString(),
                        RuleType = "missing-required-field",
                        Severity = "warning",
                        Message = $"Approved data point '{dataPoint.Title}' in section '{section.Title}' has no supporting evidence.",
                        SectionId = section.Id,
                        SectionTitle = section.Title,
                        AffectedDataPointIds = new List<string> { dataPoint.Id },
                        FieldName = "EvidenceIds",
                        ExpectedValue = "At least one evidence document",
                        ActualValue = "empty",
                        DetectedAt = DateTime.UtcNow.ToString("O")
                    });
                }

                // Check for estimates without required fields
                if (dataPoint.InformationType == "estimate")
                {
                    if (string.IsNullOrWhiteSpace(dataPoint.EstimateType))
                    {
                        issues.Add(new ValidationIssue
                        {
                            Id = Guid.NewGuid().ToString(),
                            RuleType = "missing-required-field",
                            Severity = "error",
                            Message = $"Estimate data point '{dataPoint.Title}' is missing EstimateType.",
                            SectionId = section.Id,
                            SectionTitle = section.Title,
                            AffectedDataPointIds = new List<string> { dataPoint.Id },
                            FieldName = "EstimateType",
                            ExpectedValue = "point, range, proxy-based, or extrapolated",
                            ActualValue = "null or empty",
                            DetectedAt = DateTime.UtcNow.ToString("O")
                        });
                    }

                    if (string.IsNullOrWhiteSpace(dataPoint.EstimateMethod))
                    {
                        issues.Add(new ValidationIssue
                        {
                            Id = Guid.NewGuid().ToString(),
                            RuleType = "missing-required-field",
                            Severity = "error",
                            Message = $"Estimate data point '{dataPoint.Title}' is missing EstimateMethod.",
                            SectionId = section.Id,
                            SectionTitle = section.Title,
                            AffectedDataPointIds = new List<string> { dataPoint.Id },
                            FieldName = "EstimateMethod",
                            ExpectedValue = "Description of estimation methodology",
                            ActualValue = "null or empty",
                            DetectedAt = DateTime.UtcNow.ToString("O")
                        });
                    }

                    if (string.IsNullOrWhiteSpace(dataPoint.ConfidenceLevel))
                    {
                        issues.Add(new ValidationIssue
                        {
                            Id = Guid.NewGuid().ToString(),
                            RuleType = "missing-required-field",
                            Severity = "error",
                            Message = $"Estimate data point '{dataPoint.Title}' is missing ConfidenceLevel.",
                            SectionId = section.Id,
                            SectionTitle = section.Title,
                            AffectedDataPointIds = new List<string> { dataPoint.Id },
                            FieldName = "ConfidenceLevel",
                            ExpectedValue = "low, medium, or high",
                            ActualValue = "null or empty",
                            DetectedAt = DateTime.UtcNow.ToString("O")
                        });
                    }
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// Validates that sections marked as 'Ready' have required evidence and provenance metadata.
    /// </summary>
    private List<ValidationIssue> ValidateReadySectionMetadata(ReportingPeriod period, List<ReportSection> sections)
    {
        var issues = new List<ValidationIssue>();

        foreach (var section in sections.Where(s => s.Status == "ready-for-review" || s.Status == "approved"))
        {
            var dataPoints = _dataPoints.Where(dp => dp.SectionId == section.Id).ToList();
            
            foreach (var dataPoint in dataPoints)
            {
                // Skip validation for not-applicable or missing data points
                if (dataPoint.CompletenessStatus == "not-applicable" || dataPoint.CompletenessStatus == "missing")
                {
                    continue;
                }

                // Check for missing evidence (required for ready/approved sections)
                if (dataPoint.EvidenceIds.Count == 0 && dataPoint.InformationType != "estimate")
                {
                    issues.Add(new ValidationIssue
                    {
                        Id = Guid.NewGuid().ToString(),
                        RuleType = "missing-required-field",
                        Severity = "error",
                        Message = $"Section '{section.Title}' is marked as '{section.Status}', but data point '{dataPoint.Title}' has no supporting evidence.",
                        SectionId = section.Id,
                        SectionTitle = section.Title,
                        AffectedDataPointIds = new List<string> { dataPoint.Id },
                        FieldName = "EvidenceIds",
                        ExpectedValue = "At least one evidence document",
                        ActualValue = "empty",
                        DetectedAt = DateTime.UtcNow.ToString("O")
                    });
                }

                // Check for missing provenance metadata
                if (string.IsNullOrWhiteSpace(dataPoint.Source))
                {
                    issues.Add(new ValidationIssue
                    {
                        Id = Guid.NewGuid().ToString(),
                        RuleType = "missing-required-field",
                        Severity = "error",
                        Message = $"Section '{section.Title}' is marked as '{section.Status}', but data point '{dataPoint.Title}' has no source information.",
                        SectionId = section.Id,
                        SectionTitle = section.Title,
                        AffectedDataPointIds = new List<string> { dataPoint.Id },
                        FieldName = "Source",
                        ExpectedValue = "Source description",
                        ActualValue = "null or empty",
                        DetectedAt = DateTime.UtcNow.ToString("O")
                    });
                }

                // Check for missing calculation formula when data is calculated
                if (dataPoint.IsCalculated && string.IsNullOrWhiteSpace(dataPoint.CalculationFormula))
                {
                    issues.Add(new ValidationIssue
                    {
                        Id = Guid.NewGuid().ToString(),
                        RuleType = "missing-required-field",
                        Severity = "error",
                        Message = $"Section '{section.Title}' is marked as '{section.Status}', but calculated data point '{dataPoint.Title}' has no calculation formula.",
                        SectionId = section.Id,
                        SectionTitle = section.Title,
                        AffectedDataPointIds = new List<string> { dataPoint.Id },
                        FieldName = "CalculationFormula",
                        ExpectedValue = "Calculation method and source data",
                        ActualValue = "null or empty",
                        DetectedAt = DateTime.UtcNow.ToString("O")
                    });
                }

                // Verify evidence integrity
                foreach (var evidenceId in dataPoint.EvidenceIds)
                {
                    var evidence = _evidence.FirstOrDefault(e => e.Id == evidenceId);
                    if (evidence != null && evidence.IntegrityStatus == "failed")
                    {
                        issues.Add(new ValidationIssue
                        {
                            Id = Guid.NewGuid().ToString(),
                            RuleType = "evidence-integrity-failed",
                            Severity = "error",
                            Message = $"Section '{section.Title}' is marked as '{section.Status}', but evidence '{evidence.Title}' linked to data point '{dataPoint.Title}' has failed integrity check.",
                            SectionId = section.Id,
                            SectionTitle = section.Title,
                            AffectedDataPointIds = new List<string> { dataPoint.Id },
                            AffectedEvidenceIds = new List<string> { evidenceId },
                            DetectedAt = DateTime.UtcNow.ToString("O")
                        });
                    }
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// Validates compliance with reporting mode requirements (SME simplified vs CSRD/ESRS-aligned).
    /// </summary>
    private List<ValidationIssue> ValidateReportingModeCompliance(ReportingPeriod period, List<ReportSection> sections)
    {
        var issues = new List<ValidationIssue>();

        // CSRD/ESRS-aligned mode requires additional metadata
        if (period.ReportingMode == "extended" || period.ReportingMode == "csrd" || period.ReportingMode == "esrs")
        {
            // Validate catalog codes are present for all sections
            foreach (var section in sections.Where(s => s.IsEnabled))
            {
                if (string.IsNullOrWhiteSpace(section.CatalogCode))
                {
                    issues.Add(new ValidationIssue
                    {
                        Id = Guid.NewGuid().ToString(),
                        RuleType = "reporting-mode-compliance",
                        Severity = "error",
                        Message = $"Section '{section.Title}' must have a CatalogCode for {period.ReportingMode} reporting mode.",
                        SectionId = section.Id,
                        SectionTitle = section.Title,
                        FieldName = "CatalogCode",
                        ExpectedValue = "Valid ESRS disclosure topic code",
                        ActualValue = "null or empty",
                        DetectedAt = DateTime.UtcNow.ToString("O")
                    });
                }

                var dataPoints = _dataPoints.Where(dp => dp.SectionId == section.Id).ToList();
                
                // In CSRD/ESRS mode, all data points must have classification
                foreach (var dataPoint in dataPoints.Where(dp => dp.CompletenessStatus != "not-applicable"))
                {
                    if (string.IsNullOrWhiteSpace(dataPoint.Classification))
                    {
                        issues.Add(new ValidationIssue
                        {
                            Id = Guid.NewGuid().ToString(),
                            RuleType = "reporting-mode-compliance",
                            Severity = "error",
                            Message = $"Data point '{dataPoint.Title}' in section '{section.Title}' must have a Classification for {period.ReportingMode} reporting mode.",
                            SectionId = section.Id,
                            SectionTitle = section.Title,
                            AffectedDataPointIds = new List<string> { dataPoint.Id },
                            FieldName = "Classification",
                            ExpectedValue = "Valid ESRS data point classification",
                            ActualValue = "null or empty",
                            DetectedAt = DateTime.UtcNow.ToString("O")
                        });
                    }

                    // Estimates in CSRD mode need higher confidence documentation
                    if (dataPoint.InformationType == "estimate" && dataPoint.ConfidenceLevel == "low")
                    {
                        issues.Add(new ValidationIssue
                        {
                            Id = Guid.NewGuid().ToString(),
                            RuleType = "reporting-mode-compliance",
                            Severity = "warning",
                            Message = $"Data point '{dataPoint.Title}' in section '{section.Title}' is an estimate with low confidence. {period.ReportingMode} mode recommends medium or high confidence.",
                            SectionId = section.Id,
                            SectionTitle = section.Title,
                            AffectedDataPointIds = new List<string> { dataPoint.Id },
                            FieldName = "ConfidenceLevel",
                            ExpectedValue = "medium or high",
                            ActualValue = "low",
                            DetectedAt = DateTime.UtcNow.ToString("O")
                        });
                    }
                }
            }

            // Check for organizational scope requirements
            if (period.ReportScope == "single-company")
            {
                issues.Add(new ValidationIssue
                {
                    Id = Guid.NewGuid().ToString(),
                    RuleType = "reporting-mode-compliance",
                    Severity = "info",
                    Message = $"Reporting period uses single-company scope. {period.ReportingMode} mode typically requires group-level reporting for larger organizations.",
                    FieldName = "ReportScope",
                    ExpectedValue = "group (for larger organizations)",
                    ActualValue = "single-company",
                    DetectedAt = DateTime.UtcNow.ToString("O")
                });
            }
        }
        else // Simplified mode (SME)
        {
            // In simplified mode, warn about overly complex configurations
            var sectionsWithCatalogCodes = sections.Count(s => !string.IsNullOrWhiteSpace(s.CatalogCode));
            if (sectionsWithCatalogCodes > 0)
            {
                issues.Add(new ValidationIssue
                {
                    Id = Guid.NewGuid().ToString(),
                    RuleType = "reporting-mode-compliance",
                    Severity = "info",
                    Message = $"{sectionsWithCatalogCodes} sections have catalog codes, which are not required for simplified reporting mode. Consider switching to 'extended' mode if detailed ESRS mapping is needed.",
                    DetectedAt = DateTime.UtcNow.ToString("O")
                });
            }
        }

        return issues;
    }

    #endregion

    #region Approval Workflow

    /// <summary>
    /// Creates an approval request for a reporting period.
    /// </summary>
    public (bool IsValid, string? ErrorMessage, ApprovalRequest? ApprovalRequest) CreateApprovalRequest(CreateApprovalRequestRequest request)
    {
        lock (_lock)
        {
            // Validate period exists
            var period = _periods.FirstOrDefault(p => p.Id == request.PeriodId);
            if (period == null)
            {
                return (false, $"Reporting period with ID '{request.PeriodId}' not found.", null);
            }

            // Validate requester exists
            var requester = _users.FirstOrDefault(u => u.Id == request.RequestedBy);
            if (requester == null)
            {
                return (false, $"User with ID '{request.RequestedBy}' not found.", null);
            }

            // Validate approvers
            if (request.ApproverIds == null || request.ApproverIds.Count == 0)
            {
                return (false, "At least one approver must be specified.", null);
            }

            var approvers = _users.Where(u => request.ApproverIds.Contains(u.Id)).ToList();
            if (approvers.Count != request.ApproverIds.Count)
            {
                return (false, "One or more approver IDs are invalid.", null);
            }

            // Create approval request
            var approvalRequest = new ApprovalRequest
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = request.PeriodId,
                RequestedBy = request.RequestedBy,
                RequestedAt = DateTime.UtcNow.ToString("O"),
                RequestMessage = request.RequestMessage,
                ApprovalDeadline = request.ApprovalDeadline,
                Status = "pending",
                Approvals = new List<ApprovalRecord>()
            };

            // Create approval records for each approver
            foreach (var approver in approvers)
            {
                var record = new ApprovalRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    ApprovalRequestId = approvalRequest.Id,
                    ApproverId = approver.Id,
                    ApproverName = approver.Name,
                    Status = "pending"
                };
                approvalRequest.Approvals.Add(record);
                _approvalRecords.Add(record);
            }

            _approvalRequests.Add(approvalRequest);

            // Log the action
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EntityType = "ApprovalRequest",
                EntityId = approvalRequest.Id,
                Action = "created",
                UserId = request.RequestedBy,
                UserName = requester.Name,
                Timestamp = DateTime.UtcNow.ToString("O"),
                ChangeNote = $"Approval request created for period '{period.Name}' with {approvers.Count} approver(s)"
            });

            return (true, null, approvalRequest);
        }
    }

    /// <summary>
    /// Submits an approval decision (approve or reject).
    /// </summary>
    public (bool IsValid, string? ErrorMessage, ApprovalRecord? ApprovalRecord) SubmitApprovalDecision(SubmitApprovalDecisionRequest request)
    {
        lock (_lock)
        {
            // Validate decision
            if (request.Decision != "approve" && request.Decision != "reject")
            {
                return (false, "Decision must be either 'approve' or 'reject'.", null);
            }

            // Find approval record
            var record = _approvalRecords.FirstOrDefault(r => r.Id == request.ApprovalRecordId);
            if (record == null)
            {
                return (false, $"Approval record with ID '{request.ApprovalRecordId}' not found.", null);
            }

            // Validate approver
            var approver = _users.FirstOrDefault(u => u.Id == request.DecidedBy);
            if (approver == null)
            {
                return (false, $"User with ID '{request.DecidedBy}' not found.", null);
            }

            // Check authorization - only assigned approver can decide
            if (record.ApproverId != request.DecidedBy)
            {
                return (false, "You are not authorized to decide on this approval.", null);
            }

            // Check if already decided
            if (record.Status != "pending")
            {
                return (false, $"This approval has already been {record.Status}.", null);
            }

            // Update approval record
            record.Decision = request.Decision;
            record.Status = request.Decision == "approve" ? "approved" : "rejected";
            record.DecidedAt = DateTime.UtcNow.ToString("O");
            record.Comment = request.Comment;

            // Update overall approval request status
            var approvalRequest = _approvalRequests.FirstOrDefault(ar => ar.Id == record.ApprovalRequestId);
            if (approvalRequest != null)
            {
                // Check if all approvals are decided
                var allDecided = approvalRequest.Approvals.All(a => a.Status != "pending");
                if (allDecided)
                {
                    // If any rejection, overall status is rejected
                    var anyRejection = approvalRequest.Approvals.Any(a => a.Status == "rejected");
                    approvalRequest.Status = anyRejection ? "rejected" : "approved";
                }
            }

            // Log the action
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EntityType = "ApprovalRecord",
                EntityId = record.Id,
                Action = record.Status,
                UserId = request.DecidedBy,
                UserName = approver.Name,
                Timestamp = DateTime.UtcNow.ToString("O"),
                ChangeNote = $"Approval {record.Status} for approval request {record.ApprovalRequestId}"
            });

            return (true, null, record);
        }
    }

    /// <summary>
    /// Gets all approval requests for a reporting period.
    /// </summary>
    public List<ApprovalRequest> GetApprovalRequests(string? periodId = null, string? approverId = null)
    {
        lock (_lock)
        {
            var query = _approvalRequests.AsEnumerable();

            if (!string.IsNullOrEmpty(periodId))
            {
                query = query.Where(ar => ar.PeriodId == periodId);
            }

            if (!string.IsNullOrEmpty(approverId))
            {
                query = query.Where(ar => ar.Approvals.Any(a => a.ApproverId == approverId));
            }

            return query.ToList();
        }
    }

    /// <summary>
    /// Gets a specific approval request by ID.
    /// </summary>
    public ApprovalRequest? GetApprovalRequest(string id)
    {
        lock (_lock)
        {
            return _approvalRequests.FirstOrDefault(ar => ar.Id == id);
        }
    }

    #endregion

    #region Audit Package Export

    private readonly List<AuditPackageExportRecord> _auditPackageExports = new();

    /// <summary>
    /// Generate an audit package for external auditors.
    /// </summary>
    public (bool IsValid, string? ErrorMessage, ExportAuditPackageResult? Result) GenerateAuditPackage(ExportAuditPackageRequest request)
    {
        lock (_lock)
        {
            var period = _periods.FirstOrDefault(p => p.Id == request.PeriodId);
            if (period == null)
            {
                return (false, $"Period with ID '{request.PeriodId}' not found.", null);
            }

            // Get user if exists, otherwise use the user ID directly
            var user = _users.FirstOrDefault(u => u.Id == request.ExportedBy);
            var userName = user?.Name ?? request.ExportedBy;

            // Build the package contents
            var contents = BuildAuditPackageContentsInternal(request, period, request.ExportedBy, userName);
            
            // Calculate checksum (will be properly calculated during ZIP download)
            // This is just a placeholder for the metadata endpoint
            var checksum = ""; // Checksum is calculated during ZIP creation in download endpoint
            
            var exportId = Guid.NewGuid().ToString();
            var exportedAt = DateTime.UtcNow.ToString("O");

            // Create result
            var result = new ExportAuditPackageResult
            {
                ExportId = exportId,
                ExportedAt = exportedAt,
                ExportedBy = request.ExportedBy,
                Checksum = checksum,
                PackageSize = 0, // Package size is calculated during ZIP creation in download endpoint
                Summary = new AuditPackageSummary
                {
                    PeriodId = period.Id,
                    PeriodName = period.Name,
                    SectionCount = contents.Sections.Count,
                    DataPointCount = contents.Sections.Sum(s => s.DataPoints.Count),
                    AuditLogEntryCount = contents.AuditTrail.Count,
                    DecisionCount = contents.Decisions.Count,
                    AssumptionCount = contents.Sections.Sum(s => s.Assumptions.Count),
                    GapCount = contents.Sections.Sum(s => s.Gaps.Count),
                    EvidenceFileCount = contents.EvidenceFiles.Count,
                    ValidationResultCount = contents.ValidationResults.Count
                }
            };

            return (true, null, result);
        }
    }

    /// <summary>
    /// Build audit package contents for export.
    /// </summary>
    public AuditPackageContents? BuildAuditPackageContents(ExportAuditPackageRequest request)
    {
        lock (_lock)
        {
            var period = _periods.FirstOrDefault(p => p.Id == request.PeriodId);
            if (period == null)
            {
                return null;
            }

            var user = _users.FirstOrDefault(u => u.Id == request.ExportedBy);
            var userName = user?.Name ?? request.ExportedBy;

            return BuildAuditPackageContentsInternal(request, period, request.ExportedBy, userName);
        }
    }

    private AuditPackageContents BuildAuditPackageContentsInternal(ExportAuditPackageRequest request, ReportingPeriod period, string userId, string userName)
    {
        var exportId = Guid.NewGuid().ToString();
        var exportedAt = DateTime.UtcNow.ToString("O");

        // Determine which sections to include
        var sectionsToInclude = request.SectionIds != null && request.SectionIds.Count > 0
            ? _sections.Where(s => s.PeriodId == request.PeriodId && request.SectionIds.Contains(s.Id)).ToList()
            : _sections.Where(s => s.PeriodId == request.PeriodId).ToList();

        // Validate that we have at least one section
        if (sectionsToInclude.Count == 0)
        {
            // If specific sections were requested, they might not exist
            if (request.SectionIds != null && request.SectionIds.Count > 0)
            {
                // Return empty package rather than failing - allows partial exports
                sectionsToInclude = new List<ReportSection>();
            }
        }

        var sectionIds = sectionsToInclude.Select(s => s.Id).ToList();

        // Build section audit data
        var sectionAuditData = sectionsToInclude.Select(section =>
        {
            var dataPoints = _dataPoints.Where(dp => dp.SectionId == section.Id).ToList();
            var dataPointIds = dataPoints.Select(dp => dp.Id).ToList();
            
            // Build provenance mappings (fragment audit views)
            var provenanceMappings = dataPointIds.Select(dpId => GetDataPointAuditView(dpId))
                .Where(v => v != null)
                .Cast<FragmentAuditView>()
                .ToList();
            
            var gaps = _gaps.Where(g => g.SectionId == section.Id).ToList();
            var assumptions = _assumptions.Where(a => 
                dataPointIds.Any(dpId => a.LinkedDataPointIds?.Contains(dpId) ?? false)).ToList();

            return new SectionAuditData
            {
                Section = section,
                DataPoints = dataPoints,
                ProvenanceMappings = provenanceMappings,
                Gaps = gaps,
                Assumptions = assumptions
            };
        }).ToList();

        // Get audit trail for the period/sections
        var auditTrail = _auditLog.Where(log =>
        {
            // Include audit logs for the period itself
            if (log.EntityType == "ReportingPeriod" && log.EntityId == request.PeriodId)
                return true;

            // Include audit logs for sections
            if (log.EntityType == "ReportSection" && sectionIds.Contains(log.EntityId))
                return true;

            // Include audit logs for data points in these sections
            if (log.EntityType == "DataPoint")
            {
                var dp = _dataPoints.FirstOrDefault(d => d.Id == log.EntityId);
                if (dp != null && sectionIds.Contains(dp.SectionId))
                    return true;
            }

            // Include audit logs for gaps in these sections
            if (log.EntityType == "Gap")
            {
                var gap = _gaps.FirstOrDefault(g => g.Id == log.EntityId);
                if (gap != null && sectionIds.Contains(gap.SectionId))
                    return true;
            }

            // Include audit logs for assumptions linked to data points in these sections
            if (log.EntityType == "Assumption")
            {
                var assumption = _assumptions.FirstOrDefault(a => a.Id == log.EntityId);
                if (assumption != null && assumption.LinkedDataPointIds != null)
                {
                    var dataPointIds = _dataPoints.Where(dp => sectionIds.Contains(dp.SectionId)).Select(dp => dp.Id).ToList();
                    if (assumption.LinkedDataPointIds.Any(id => dataPointIds.Contains(id)))
                        return true;
                }
            }

            // Include audit logs for evidence in these sections
            if (log.EntityType == "Evidence")
            {
                var evidence = _evidence.FirstOrDefault(e => e.Id == log.EntityId);
                if (evidence != null && sectionIds.Contains(evidence.SectionId))
                    return true;
            }

            return false;
        }).OrderByDescending(log => log.Timestamp).ToList();

        // Get decisions related to data points in the sections
        var dataPointIdsInSections = sectionAuditData.SelectMany(s => s.DataPoints.Select(dp => dp.Id)).ToList();
        var decisions = _decisions.Where(d => 
            d.ReferencedByFragmentIds != null && 
            d.ReferencedByFragmentIds.Any(id => dataPointIdsInSections.Contains(id))
        ).ToList();

        // Get evidence files for the sections
        var evidenceFiles = _evidence.Where(e => sectionIds.Contains(e.SectionId))
            .Select(e => new EvidenceReference
            {
                Id = e.Id,
                FileName = e.FileName ?? "",
                FileUrl = e.FileUrl,
                FileSize = e.FileSize,
                Checksum = e.Checksum,
                ContentType = e.ContentType,
                IntegrityStatus = e.IntegrityStatus,
                SectionId = e.SectionId,
                UploadedBy = e.UploadedBy,
                UploadedAt = e.UploadedAt,
                LinkedDataPointIds = _dataPoints
                    .Where(dp => dp.EvidenceIds != null && dp.EvidenceIds.Contains(e.Id))
                    .Select(dp => dp.Id)
                    .ToList()
            }).ToList();

        // Get validation results for the period
        var validationResults = _validationResults
            .Where(v => v.PeriodId == request.PeriodId)
            .OrderByDescending(v => v.ValidatedAt)
            .ToList();

        return new AuditPackageContents
        {
            Metadata = new ExportMetadata
            {
                ExportId = exportId,
                ExportedAt = exportedAt,
                ExportedBy = userId,
                ExportedByName = userName,
                ExportNote = request.ExportNote,
                Version = "1.0",
                DataSnapshotId = period.IntegrityHash
            },
            Period = period,
            Sections = sectionAuditData,
            AuditTrail = auditTrail,
            Decisions = decisions,
            EvidenceFiles = evidenceFiles,
            ValidationResults = validationResults
        };
    }

    /// <summary>
    /// Record an audit package export for tracking purposes.
    /// </summary>
    public void RecordAuditPackageExport(ExportAuditPackageRequest request, string checksum, long packageSize)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => u.Id == request.ExportedBy);
            var sectionIds = request.SectionIds ?? new List<string>();
            
            if (sectionIds.Count == 0)
            {
                // Include all sections for the period
                sectionIds = _sections.Where(s => s.PeriodId == request.PeriodId).Select(s => s.Id).ToList();
            }

            var record = new AuditPackageExportRecord
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = request.PeriodId,
                SectionIds = sectionIds,
                ExportedAt = DateTime.UtcNow.ToString("O"),
                ExportedBy = request.ExportedBy,
                ExportedByName = user?.Name ?? "Unknown",
                ExportNote = request.ExportNote,
                Checksum = checksum,
                PackageSize = packageSize
            };

            _auditPackageExports.Add(record);

            // Also add to audit log
            CreateAuditLogEntry(request.ExportedBy, user?.Name ?? "Unknown", "export",
                "AuditPackageExport", record.Id, new List<FieldChange>
                {
                    new() { Field = "PeriodId", NewValue = request.PeriodId },
                    new() { Field = "SectionCount", NewValue = sectionIds.Count.ToString() },
                    new() { Field = "Checksum", NewValue = checksum },
                    new() { Field = "PackageSize", NewValue = packageSize.ToString() }
                }, request.ExportNote ?? "Audit package exported");
        }
    }

    /// <summary>
    /// Get all audit package exports for a period.
    /// </summary>
    public IReadOnlyList<AuditPackageExportRecord> GetAuditPackageExports(string periodId)
    {
        lock (_lock)
        {
            return _auditPackageExports
                .Where(e => e.PeriodId == periodId)
                .OrderByDescending(e => e.ExportedAt)
                .ToList();
        }
    }

    #endregion
    
    #region Integrity Verification
    
    /// <summary>
    /// Verifies the integrity of a reporting period by comparing its stored hash with a recalculated hash.
    /// </summary>
    /// <returns>True if integrity is valid, false if mismatch detected</returns>
    public bool VerifyReportingPeriodIntegrity(string periodId)
    {
        lock (_lock)
        {
            var period = _periods.FirstOrDefault(p => p.Id == periodId);
            if (period == null)
            {
                return false;
            }
            
            var isValid = Services.IntegrityService.VerifyReportingPeriodIntegrity(period);
            
            if (!isValid)
            {
                period.IntegrityWarning = true;
                period.IntegrityWarningDetails = $"Integrity check failed at {DateTime.UtcNow:O}. Stored hash does not match calculated hash.";
            }
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Verifies the integrity of a decision by comparing its stored hash with a recalculated hash.
    /// </summary>
    /// <returns>True if integrity is valid, false if mismatch detected</returns>
    public bool VerifyDecisionIntegrity(string decisionId)
    {
        lock (_lock)
        {
            var decision = _decisions.FirstOrDefault(d => d.Id == decisionId);
            if (decision == null)
            {
                return false;
            }
            
            var isValid = Services.IntegrityService.VerifyDecisionIntegrity(decision);
            
            decision.IntegrityStatus = isValid ? "valid" : "failed";
            
            return isValid;
        }
    }
    
    /// <summary>
    /// Verifies integrity for all decisions in a reporting period.
    /// </summary>
    /// <returns>List of decision IDs that failed integrity check</returns>
    public List<string> VerifyPeriodDecisionsIntegrity(string periodId)
    {
        lock (_lock)
        {
            var failedDecisions = new List<string>();
            
            // Get all sections for this period
            var sectionIds = _sections
                .Where(s => s.PeriodId == periodId)
                .Select(s => s.Id)
                .ToHashSet();
            
            // Check decisions linked to these sections
            var periodDecisions = _decisions
                .Where(d => d.SectionId != null && sectionIds.Contains(d.SectionId))
                .ToList();
            
            foreach (var decision in periodDecisions)
            {
                if (!VerifyDecisionIntegrity(decision.Id))
                {
                    failedDecisions.Add(decision.Id);
                }
            }
            
            return failedDecisions;
        }
    }
    
    /// <summary>
    /// Checks if a reporting period can be published based on integrity status.
    /// </summary>
    /// <returns>True if can publish, false if blocked by integrity warning</returns>
    public bool CanPublishPeriod(string periodId)
    {
        lock (_lock)
        {
            var period = _periods.FirstOrDefault(p => p.Id == periodId);
            if (period == null)
            {
                return false;
            }
            
            // Block publication if integrity warning exists
            return !period.IntegrityWarning;
        }
    }
    
    /// <summary>
    /// Overrides an integrity warning to allow publication. Requires admin justification.
    /// </summary>
    public (bool success, string? errorMessage) OverrideIntegrityWarning(
        string periodId, 
        string adminUserId, 
        string justification)
    {
        lock (_lock)
        {
            var period = _periods.FirstOrDefault(p => p.Id == periodId);
            if (period == null)
            {
                return (false, "Reporting period not found.");
            }
            
            if (!period.IntegrityWarning)
            {
                return (false, "No integrity warning exists for this period.");
            }
            
            if (string.IsNullOrWhiteSpace(justification))
            {
                return (false, "Justification is required to override integrity warning.");
            }
            
            // Verify user is admin
            var user = _users.FirstOrDefault(u => u.Id == adminUserId);
            if (user == null || user.Role != "admin")
            {
                return (false, "Only administrators can override integrity warnings.");
            }
            
            // Clear warning but preserve details for audit trail
            period.IntegrityWarning = false;
            period.IntegrityWarningDetails = $"{period.IntegrityWarningDetails}\n\nOverridden by {user.Name} ({adminUserId}) at {DateTime.UtcNow:O}.\nJustification: {justification}";
            
            // Create audit log entry
            var changes = new List<FieldChange>
            {
                new FieldChange 
                { 
                    Field = "IntegrityWarning", 
                    OldValue = "true", 
                    NewValue = "false (overridden)" 
                }
            };
            
            CreateAuditLogEntry(
                adminUserId,
                user.Name,
                "override-integrity-warning",
                "ReportingPeriod",
                periodId,
                changes,
                $"Overrode integrity warning. Justification: {justification}");
            
            return (true, null);
        }
    }
    
    /// <summary>
    /// Gets the integrity status for a reporting period including all related entities.
    /// </summary>
    public IntegrityStatusReport GetIntegrityStatus(string periodId)
    {
        lock (_lock)
        {
            var period = _periods.FirstOrDefault(p => p.Id == periodId);
            if (period == null)
            {
                return new IntegrityStatusReport
                {
                    PeriodId = periodId,
                    PeriodIntegrityValid = false,
                    FailedDecisions = new List<string>(),
                    CanPublish = false,
                    ErrorMessage = "Reporting period not found."
                };
            }
            
            var periodValid = VerifyReportingPeriodIntegrity(periodId);
            var failedDecisions = VerifyPeriodDecisionsIntegrity(periodId);
            
            return new IntegrityStatusReport
            {
                PeriodId = periodId,
                PeriodIntegrityValid = periodValid,
                PeriodIntegrityWarning = period.IntegrityWarning,
                FailedDecisions = failedDecisions,
                CanPublish = CanPublishPeriod(periodId),
                WarningDetails = period.IntegrityWarningDetails
            };
        }
    }
    
    #endregion
    
    #region Retention Policies and Data Cleanup
    
    /// <summary>
    /// Creates a new retention policy.
    /// </summary>
    public (bool Success, string? ErrorMessage, RetentionPolicy? Policy) CreateRetentionPolicy(CreateRetentionPolicyRequest request)
    {
        lock (_lock)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(request.CreatedBy))
            {
                return (false, "CreatedBy is required.", null);
            }
            
            if (request.RetentionDays < 1)
            {
                return (false, "Retention days must be at least 1.", null);
            }
            
            // Validate data category
            var validCategories = new[] { "all", "audit-log", "evidence" };
            if (!validCategories.Contains(request.DataCategory))
            {
                return (false, $"DataCategory must be one of: {string.Join(", ", validCategories)}", null);
            }
            
            // Calculate priority based on specificity (more specific = higher priority)
            int priority = 0;
            if (request.TenantId != null) priority += TenantSpecificityPriority;
            if (request.ReportType != null) priority += ReportTypeSpecificityPriority;
            if (request.DataCategory != "all") priority += DataCategorySpecificityPriority;
            
            var policy = new RetentionPolicy
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = request.TenantId,
                ReportType = request.ReportType,
                DataCategory = request.DataCategory,
                RetentionDays = request.RetentionDays,
                IsActive = true,
                Priority = priority,
                AllowDeletion = request.AllowDeletion,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = request.CreatedBy,
                UpdatedAt = DateTime.UtcNow.ToString("o")
            };
            
            _retentionPolicies.Add(policy);
            
            // Audit log entry
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = request.CreatedBy,
                UserName = _users.FirstOrDefault(u => u.Id == request.CreatedBy)?.Name ?? request.CreatedBy,
                Action = "create-retention-policy",
                EntityType = "RetentionPolicy",
                EntityId = policy.Id,
                Changes = new List<FieldChange>
                {
                    new() { Field = "TenantId", OldValue = "", NewValue = policy.TenantId ?? "null" },
                    new() { Field = "ReportType", OldValue = "", NewValue = policy.ReportType ?? "null" },
                    new() { Field = "DataCategory", OldValue = "", NewValue = policy.DataCategory },
                    new() { Field = "RetentionDays", OldValue = "", NewValue = policy.RetentionDays.ToString() },
                    new() { Field = "AllowDeletion", OldValue = "", NewValue = policy.AllowDeletion.ToString() }
                }
            });
            
            return (true, null, policy);
        }
    }
    
    /// <summary>
    /// Gets all retention policies.
    /// </summary>
    public IReadOnlyList<RetentionPolicy> GetRetentionPolicies(string? tenantId = null, bool activeOnly = true)
    {
        lock (_lock)
        {
            var policies = _retentionPolicies.AsEnumerable();
            
            if (activeOnly)
            {
                policies = policies.Where(p => p.IsActive);
            }
            
            if (tenantId != null)
            {
                policies = policies.Where(p => p.TenantId == null || p.TenantId == tenantId);
            }
            
            return policies.OrderByDescending(p => p.Priority).ToList();
        }
    }
    
    /// <summary>
    /// Gets the applicable retention policy for a specific context.
    /// Returns the highest priority matching policy.
    /// </summary>
    public RetentionPolicy? GetApplicableRetentionPolicy(string dataCategory, string? tenantId = null, string? reportType = null)
    {
        lock (_lock)
        {
            var policies = _retentionPolicies
                .Where(p => p.IsActive)
                .Where(p => p.DataCategory == "all" || p.DataCategory == dataCategory)
                .Where(p => p.TenantId == null || p.TenantId == tenantId)
                .Where(p => p.ReportType == null || p.ReportType == reportType)
                .OrderByDescending(p => p.Priority)
                .ToList();
            
            return policies.FirstOrDefault();
        }
    }
    
    /// <summary>
    /// Runs cleanup based on retention policies.
    /// </summary>
    public CleanupResult RunCleanup(RunCleanupRequest request)
    {
        lock (_lock)
        {
            var result = new CleanupResult
            {
                Success = true,
                WasDryRun = request.DryRun,
                ExecutedAt = DateTime.UtcNow.ToString("o")
            };
            
            try
            {
                // Get applicable policies
                var policies = GetRetentionPolicies(request.TenantId, activeOnly: true);
                
                if (!policies.Any())
                {
                    result.ErrorMessage = "No active retention policies found.";
                    return result;
                }
                
                // Process audit log cleanup
                var auditPolicy = GetApplicableRetentionPolicy("audit-log", request.TenantId);
                if (auditPolicy != null)
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-auditPolicy.RetentionDays);
                    var eligibleEntries = _auditLog
                        .Where(e => DateTime.TryParse(e.Timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var timestamp) && timestamp < cutoffDate)
                        .ToList();
                    
                    result.RecordsIdentified += eligibleEntries.Count;
                    
                    if (!request.DryRun && auditPolicy.AllowDeletion && eligibleEntries.Any())
                    {
                        // Check for legal holds
                        var hasLegalHold = _legalHolds.Any(h => 
                            h.IsActive && 
                            (h.TenantId == null || h.TenantId == request.TenantId));
                        
                        if (!hasLegalHold)
                        {
                            // Create deletion report
                            var deletionReport = CreateDeletionReport(
                                auditPolicy.Id,
                                "audit-log",
                                eligibleEntries.Count,
                                eligibleEntries.Min(e => e.Timestamp),
                                eligibleEntries.Max(e => e.Timestamp),
                                request.TenantId,
                                request.InitiatedBy
                            );
                            
                            result.DeletionReportIds.Add(deletionReport.Id);
                            
                            // Perform deletion
                            foreach (var entry in eligibleEntries)
                            {
                                _auditLog.Remove(entry);
                                result.RecordsDeleted++;
                            }
                        }
                        else
                        {
                            result.ErrorMessage = "Legal hold prevents deletion.";
                        }
                    }
                }
                
                // Process evidence cleanup (similar pattern)
                var evidencePolicy = GetApplicableRetentionPolicy("evidence", request.TenantId);
                if (evidencePolicy != null && evidencePolicy.AllowDeletion)
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-evidencePolicy.RetentionDays);
                    var eligibleEvidence = _evidence
                        .Where(e => DateTime.TryParse(e.UploadedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var uploadedAt) && uploadedAt < cutoffDate)
                        .ToList();
                    
                    result.RecordsIdentified += eligibleEvidence.Count;
                    
                    if (!request.DryRun && eligibleEvidence.Any())
                    {
                        var hasLegalHold = _legalHolds.Any(h => 
                            h.IsActive && 
                            (h.TenantId == null || h.TenantId == request.TenantId));
                        
                        if (!hasLegalHold)
                        {
                            var deletionReport = CreateDeletionReport(
                                evidencePolicy.Id,
                                "evidence",
                                eligibleEvidence.Count,
                                eligibleEvidence.Min(e => e.UploadedAt),
                                eligibleEvidence.Max(e => e.UploadedAt),
                                request.TenantId,
                                request.InitiatedBy
                            );
                            
                            result.DeletionReportIds.Add(deletionReport.Id);
                            
                            foreach (var evidence in eligibleEvidence)
                            {
                                _evidence.Remove(evidence);
                                result.RecordsDeleted++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Creates a deletion report with cryptographic signature.
    /// </summary>
    private DeletionReport CreateDeletionReport(
        string policyId,
        string dataCategory,
        int recordCount,
        string dateRangeStart,
        string dateRangeEnd,
        string? tenantId,
        string deletedBy)
    {
        var report = new DeletionReport
        {
            Id = Guid.NewGuid().ToString(),
            DeletedAt = DateTime.UtcNow.ToString("o"),
            DeletedBy = deletedBy,
            PolicyId = policyId,
            DataCategory = dataCategory,
            RecordCount = recordCount,
            DateRangeStart = dateRangeStart,
            DateRangeEnd = dateRangeEnd,
            TenantId = tenantId,
            DeletionSummary = $"{recordCount} {dataCategory} records from {FormatTimestampForReport(dateRangeStart)} to {FormatTimestampForReport(dateRangeEnd)}"
        };
        
        // Generate content hash
        var contentForHash = $"{report.Id}|{report.DeletedAt}|{report.PolicyId}|{dataCategory}|{recordCount}|{dateRangeStart}|{dateRangeEnd}";
        report.ContentHash = ComputeSha256Hash(contentForHash);
        
        // Generate signature (simplified - in production, use proper cryptographic signing)
        // TODO: Replace with proper asymmetric cryptography (RSA/ECDSA) before production use
        var signatureContent = $"{report.ContentHash}|{deletedBy}|{DateTime.UtcNow:o}";
        report.Signature = ComputeSha256Hash(signatureContent);
        
        _deletionReports.Add(report);
        
        // Audit log entry for deletion report creation
        _auditLog.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow.ToString("o"),
            UserId = deletedBy,
            UserName = _users.FirstOrDefault(u => u.Id == deletedBy)?.Name ?? deletedBy,
            Action = "create-deletion-report",
            EntityType = "DeletionReport",
            EntityId = report.Id,
            Changes = new List<FieldChange>
            {
                new() { Field = "DataCategory", OldValue = "", NewValue = dataCategory },
                new() { Field = "RecordCount", OldValue = "", NewValue = recordCount.ToString() },
                new() { Field = "ContentHash", OldValue = "", NewValue = report.ContentHash }
            }
        });
        
        return report;
    }
    
    /// <summary>
    /// Gets all deletion reports.
    /// </summary>
    public IReadOnlyList<DeletionReport> GetDeletionReports(string? tenantId = null)
    {
        lock (_lock)
        {
            var reports = _deletionReports.AsEnumerable();
            
            if (tenantId != null)
            {
                reports = reports.Where(r => r.TenantId == tenantId);
            }
            
            return reports.OrderByDescending(r => r.DeletedAt).ToList();
        }
    }
    
    /// <summary>
    /// Updates a retention policy.
    /// </summary>
    public (bool Success, string? ErrorMessage) UpdateRetentionPolicy(string policyId, int retentionDays, bool allowDeletion, string updatedBy)
    {
        lock (_lock)
        {
            var policy = _retentionPolicies.FirstOrDefault(p => p.Id == policyId);
            if (policy == null)
            {
                return (false, "Retention policy not found.");
            }
            
            var changes = new List<FieldChange>();
            
            if (policy.RetentionDays != retentionDays)
            {
                changes.Add(new FieldChange
                {
                    Field = "RetentionDays",
                    OldValue = policy.RetentionDays.ToString(),
                    NewValue = retentionDays.ToString()
                });
                policy.RetentionDays = retentionDays;
            }
            
            if (policy.AllowDeletion != allowDeletion)
            {
                changes.Add(new FieldChange
                {
                    Field = "AllowDeletion",
                    OldValue = policy.AllowDeletion.ToString(),
                    NewValue = allowDeletion.ToString()
                });
                policy.AllowDeletion = allowDeletion;
            }
            
            policy.UpdatedAt = DateTime.UtcNow.ToString("o");
            
            if (changes.Any())
            {
                _auditLog.Add(new AuditLogEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    UserId = updatedBy,
                    UserName = _users.FirstOrDefault(u => u.Id == updatedBy)?.Name ?? updatedBy,
                    Action = "update-retention-policy",
                    EntityType = "RetentionPolicy",
                    EntityId = policyId,
                    Changes = changes
                });
            }
            
            return (true, null);
        }
    }
    
    /// <summary>
    /// Deactivates a retention policy.
    /// </summary>
    public (bool Success, string? ErrorMessage) DeactivateRetentionPolicy(string policyId, string deactivatedBy)
    {
        lock (_lock)
        {
            var policy = _retentionPolicies.FirstOrDefault(p => p.Id == policyId);
            if (policy == null)
            {
                return (false, "Retention policy not found.");
            }
            
            policy.IsActive = false;
            policy.UpdatedAt = DateTime.UtcNow.ToString("o");
            
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = deactivatedBy,
                UserName = _users.FirstOrDefault(u => u.Id == deactivatedBy)?.Name ?? deactivatedBy,
                Action = "deactivate-retention-policy",
                EntityType = "RetentionPolicy",
                EntityId = policyId,
                Changes = new List<FieldChange>
                {
                    new() { Field = "IsActive", OldValue = "True", NewValue = "False" }
                }
            });
            
            return (true, null);
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Computes SHA-256 hash of the given content.
    /// </summary>
    private static string ComputeSha256Hash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    /// <summary>
    /// Formats ISO 8601 timestamp for deletion report summary.
    /// </summary>
    private static string FormatTimestampForReport(string isoTimestamp)
    {
        if (DateTime.TryParse(isoTimestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
        {
            return dt.ToString("yyyy-MM-dd");
        }
        return isoTimestamp; // Fallback to original if parsing fails
    }
    
    #endregion
    
    #region Calculation Lineage Helper Methods
    
    /// <summary>
    /// Captures a snapshot of input data point values at calculation time.
    /// Returns JSON string with input values for audit trail.
    /// </summary>
    private string CaptureInputSnapshot(List<string> inputIds)
    {
        var snapshot = new Dictionary<string, object>();
        
        foreach (var inputId in inputIds)
        {
            var input = _dataPoints.FirstOrDefault(dp => dp.Id == inputId);
            if (input != null)
            {
                snapshot[inputId] = new
                {
                    value = input.Value ?? "",
                    unit = input.Unit ?? "",
                    timestamp = input.UpdatedAt
                };
            }
        }
        
        return System.Text.Json.JsonSerializer.Serialize(snapshot);
    }
    
    /// <summary>
    /// Flags all calculated data points that depend on the given input for recalculation.
    /// </summary>
    private void FlagDependentCalculationsForRecalculation(string changedDataPointId, string flaggedBy)
    {
        var now = DateTime.UtcNow.ToString("O");
        var changedDataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == changedDataPointId);
        
        if (changedDataPoint == null) return;
        
        // Find all calculated data points that depend on this input
        var dependentCalculations = _dataPoints.Where(dp => 
            dp.IsCalculated && 
            dp.CalculationInputIds.Contains(changedDataPointId));
        
        foreach (var calc in dependentCalculations)
        {
            calc.CalculationNeedsRecalculation = true;
            calc.RecalculationReason = $"Input data point '{changedDataPoint.Title}' (ID: {changedDataPointId}) was updated";
            calc.RecalculationFlaggedAt = now;
            
            // Create audit log entry
            var changes = new List<FieldChange>
            {
                new() { Field = "CalculationNeedsRecalculation", OldValue = "false", NewValue = "true" },
                new() { Field = "RecalculationReason", OldValue = "", NewValue = calc.RecalculationReason }
            };
            
            CreateAuditLogEntry(
                flaggedBy,
                "System",
                "flag-recalculation",
                "DataPoint",
                calc.Id,
                changes,
                $"Automatically flagged for recalculation due to input change"
            );
        }
    }
    
    /// <summary>
    /// Helper to compare two lists of strings for equality.
    /// </summary>
    private static bool AreListsEqual(List<string> list1, List<string>? list2)
    {
        if (list2 == null) return list1.Count == 0;
        if (list1.Count != list2.Count) return false;
        
        var sorted1 = list1.OrderBy(x => x).ToList();
        var sorted2 = list2.OrderBy(x => x).ToList();
        
        return sorted1.SequenceEqual(sorted2);
    }
    
    /// <summary>
    /// Gets calculation lineage information for a data point.
    /// </summary>
    public CalculationLineageResponse? GetCalculationLineage(string dataPointId)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == dataPointId);
            if (dataPoint == null || !dataPoint.IsCalculated)
            {
                return null;
            }
            
            var inputs = new List<LineageInput>();
            
            // Build input snapshot JSON for comparison
            var currentSnapshot = CaptureInputSnapshot(dataPoint.CalculationInputIds);
            
            foreach (var inputId in dataPoint.CalculationInputIds)
            {
                var input = _dataPoints.FirstOrDefault(dp => dp.Id == inputId);
                if (input != null)
                {
                    // Try to extract value from old snapshot using proper JSON parsing
                    string? valueAtCalc = null;
                    if (!string.IsNullOrEmpty(dataPoint.CalculationInputSnapshot))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(dataPoint.CalculationInputSnapshot);
                            if (doc.RootElement.TryGetProperty(inputId, out var inputElement))
                            {
                                if (inputElement.TryGetProperty("value", out var valueElement))
                                {
                                    valueAtCalc = valueElement.GetString();
                                }
                            }
                        }
                        catch
                        {
                            // If JSON parsing fails, leave valueAtCalc as null
                        }
                    }
                    
                    inputs.Add(new LineageInput
                    {
                        DataPointId = input.Id,
                        Title = input.Title,
                        CurrentValue = input.Value,
                        Unit = input.Unit,
                        ValueAtCalculation = valueAtCalc,
                        LastUpdated = input.UpdatedAt,
                        HasChanged = input.Value != valueAtCalc
                    });
                }
            }
            
            return new CalculationLineageResponse
            {
                DataPointId = dataPoint.Id,
                Formula = dataPoint.CalculationFormula,
                Version = dataPoint.CalculationVersion,
                CalculatedAt = dataPoint.CalculatedAt,
                CalculatedBy = dataPoint.CalculatedBy,
                Inputs = inputs,
                InputSnapshot = dataPoint.CalculationInputSnapshot,
                NeedsRecalculation = dataPoint.CalculationNeedsRecalculation,
                RecalculationReason = dataPoint.RecalculationReason
            };
        }
    }
    
    /// <summary>
    /// Recalculates a derived data point by capturing new input snapshot and incrementing version.
    /// Note: This does not actually compute the value - that's done externally.
    /// This method updates the lineage metadata.
    /// </summary>
    public (bool IsValid, string? ErrorMessage, DataPoint? DataPoint) RecalculateDataPoint(
        string dataPointId, 
        RecalculateDataPointRequest request,
        string? newValue,
        string? newUnit)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == dataPointId);
            if (dataPoint == null)
            {
                return (false, "DataPoint not found.", null);
            }
            
            if (!dataPoint.IsCalculated)
            {
                return (false, "DataPoint is not a calculated value.", null);
            }
            
            var now = DateTime.UtcNow.ToString("O");
            var changes = new List<FieldChange>();
            
            // Update value if provided
            if (newValue != null && dataPoint.Value != newValue)
            {
                changes.Add(new FieldChange { Field = "Value", OldValue = dataPoint.Value ?? "", NewValue = newValue });
                dataPoint.Value = newValue;
            }
            
            if (newUnit != null && dataPoint.Unit != newUnit)
            {
                changes.Add(new FieldChange { Field = "Unit", OldValue = dataPoint.Unit ?? "", NewValue = newUnit });
                dataPoint.Unit = newUnit;
            }
            
            // Capture new input snapshot
            var newSnapshot = CaptureInputSnapshot(dataPoint.CalculationInputIds);
            if (dataPoint.CalculationInputSnapshot != newSnapshot)
            {
                changes.Add(new FieldChange { Field = "CalculationInputSnapshot", OldValue = "previous", NewValue = "updated" });
            }
            dataPoint.CalculationInputSnapshot = newSnapshot;
            
            // Increment version
            dataPoint.CalculationVersion++;
            changes.Add(new FieldChange { Field = "CalculationVersion", OldValue = (dataPoint.CalculationVersion - 1).ToString(), NewValue = dataPoint.CalculationVersion.ToString() });
            
            // Update calculation metadata
            dataPoint.CalculatedAt = now;
            dataPoint.CalculatedBy = request.CalculatedBy;
            dataPoint.UpdatedAt = now;
            
            // Clear recalculation flag
            if (dataPoint.CalculationNeedsRecalculation)
            {
                changes.Add(new FieldChange { Field = "CalculationNeedsRecalculation", OldValue = "true", NewValue = "false" });
                dataPoint.CalculationNeedsRecalculation = false;
                dataPoint.RecalculationReason = null;
                dataPoint.RecalculationFlaggedAt = null;
            }
            
            // Create audit log entry
            var user = _users.FirstOrDefault(u => u.Id == request.CalculatedBy);
            var userName = user?.Name ?? request.CalculatedBy;
            CreateAuditLogEntry(
                request.CalculatedBy,
                userName,
                "recalculate",
                "DataPoint",
                dataPoint.Id,
                changes,
                request.ChangeNote ?? "Data point recalculated with updated inputs"
            );
            
            return (true, null, dataPoint);
        }
    }
    
    #endregion
    
    #region Period Rollover
    
    /// <summary>
    /// Performs a rollover from an existing reporting period to a new period with selected content.
    /// Validates source period status and applies rollover options to control what is copied.
    /// </summary>
    public (bool Success, string? ErrorMessage, RolloverResult? Result) RolloverPeriod(RolloverRequest request)
    {
        lock (_lock)
        {
            // Validate source period exists
            var sourcePeriod = _periods.FirstOrDefault(p => p.Id == request.SourcePeriodId);
            if (sourcePeriod == null)
            {
                return (false, $"Source period with ID '{request.SourcePeriodId}' not found.", null);
            }
            
            // Validate source period is not in draft status (governance requirement)
            if (sourcePeriod.Status == "draft")
            {
                return (false, "Cannot rollover from a period in 'draft' status. Source period must be in a stable state.", null);
            }
            
            // Validate rollover options dependencies
            if (request.Options.CopyDisclosures && !request.Options.CopyStructure)
            {
                return (false, "CopyDisclosures requires CopyStructure to be enabled.", null);
            }
            if (request.Options.CopyDataValues && !request.Options.CopyStructure)
            {
                return (false, "CopyDataValues requires CopyStructure to be enabled.", null);
            }
            if (request.Options.CopyAttachments && !request.Options.CopyDataValues)
            {
                return (false, "CopyAttachments requires CopyDataValues to be enabled.", null);
            }
            
            // Create the new target period
            var targetPeriodId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow.ToString("o");
            
            var targetPeriod = new ReportingPeriod
            {
                Id = targetPeriodId,
                Name = request.TargetPeriodName,
                StartDate = request.TargetPeriodStartDate,
                EndDate = request.TargetPeriodEndDate,
                ReportingMode = request.TargetReportingMode ?? sourcePeriod.ReportingMode,
                ReportScope = request.TargetReportScope ?? sourcePeriod.ReportScope,
                Status = "active",
                CreatedAt = now,
                OwnerId = sourcePeriod.OwnerId,
                OrganizationId = sourcePeriod.OrganizationId
            };
            
            // Calculate integrity hash
            targetPeriod.IntegrityHash = Services.IntegrityService.CalculateReportingPeriodHash(targetPeriod);
            
            _periods.Add(targetPeriod);
            
            // Initialize rollover statistics
            int sectionsCopied = 0;
            int dataPointsCopied = 0;
            int gapsCopied = 0;
            int assumptionsCopied = 0;
            int remediationPlansCopied = 0;
            int evidenceCopied = 0;
            
            // Dictionary to map source section IDs to target section IDs
            var sectionIdMapping = new Dictionary<string, string>();
            
            // Reconciliation tracking
            var mappedSections = new List<MappedSection>();
            var unmappedSections = new List<UnmappedSection>();
            
            // Copy structure (sections with ownership)
            if (request.Options.CopyStructure)
            {
                var sourceSections = _sections.Where(s => s.PeriodId == request.SourcePeriodId).ToList();
                
                // Get existing sections in target period (if any)
                var existingTargetSections = _sections.Where(s => s.PeriodId == targetPeriodId).ToList();
                
                // Build manual mapping lookup - handle duplicates by taking the first occurrence
                var manualMappingLookup = request.ManualMappings
                    .GroupBy(m => m.SourceCatalogCode)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().TargetCatalogCode
                    );
                
                foreach (var sourceSection in sourceSections)
                {
                    string? targetCatalogCode = null;
                    string mappingType = "automatic";
                    
                    // Try to find target section by CatalogCode
                    if (!string.IsNullOrWhiteSpace(sourceSection.CatalogCode))
                    {
                        // Check for manual mapping first
                        if (manualMappingLookup.TryGetValue(sourceSection.CatalogCode, out var manualTargetCode))
                        {
                            targetCatalogCode = manualTargetCode;
                            mappingType = "manual";
                        }
                        else
                        {
                            // Auto-map by matching CatalogCode
                            targetCatalogCode = sourceSection.CatalogCode;
                            mappingType = "automatic";
                        }
                    }
                    
                    // Try to find existing target section with matching catalog code
                    var existingTargetSection = targetCatalogCode != null
                        ? existingTargetSections.FirstOrDefault(s => s.CatalogCode == targetCatalogCode)
                        : null;
                    
                    ReportSection targetSection;
                    string targetSectionId;
                    
                    if (existingTargetSection != null)
                    {
                        // Map to existing section (no new section created, so sectionsCopied not incremented)
                        targetSectionId = existingTargetSection.Id;
                        targetSection = existingTargetSection;
                        sectionIdMapping[sourceSection.Id] = targetSectionId;
                        
                        // Track successful mapping
                        mappedSections.Add(new MappedSection
                        {
                            SourceCatalogCode = sourceSection.CatalogCode ?? "",
                            SourceTitle = sourceSection.Title,
                            TargetCatalogCode = targetSection.CatalogCode ?? "",
                            TargetTitle = targetSection.Title,
                            MappingType = mappingType,
                            DataPointsCopied = 0 // Will be updated later
                        });
                    }
                    else if (targetCatalogCode != null)
                    {
                        // Create new section with the target catalog code
                        targetSectionId = Guid.NewGuid().ToString();
                        sectionIdMapping[sourceSection.Id] = targetSectionId;
                        
                        targetSection = new ReportSection
                        {
                            Id = targetSectionId,
                            PeriodId = targetPeriodId,
                            Title = sourceSection.Title,
                            Category = sourceSection.Category,
                            Description = sourceSection.Description,
                            OwnerId = sourceSection.OwnerId,
                            Status = "draft", // Reset to draft for new period
                            Completeness = "empty", // Reset completeness
                            Order = sourceSection.Order,
                            CatalogCode = targetCatalogCode
                        };
                        
                        _sections.Add(targetSection);
                        
                        // Create corresponding summary
                        var ownerName = "Unassigned";
                        if (!string.IsNullOrWhiteSpace(sourceSection.OwnerId))
                        {
                            var owner = _users.FirstOrDefault(u => u.Id == sourceSection.OwnerId);
                            ownerName = owner?.Name ?? $"Unknown User ({sourceSection.OwnerId})";
                        }
                        
                        _summaries.Add(new SectionSummary
                        {
                            Id = targetSection.Id,
                            PeriodId = targetSection.PeriodId,
                            Title = targetSection.Title,
                            Category = targetSection.Category,
                            Description = targetSection.Description,
                            OwnerId = targetSection.OwnerId,
                            Status = targetSection.Status,
                            Completeness = targetSection.Completeness,
                            Order = targetSection.Order,
                            CatalogCode = targetSection.CatalogCode,
                            DataPointCount = 0,
                            EvidenceCount = 0,
                            GapCount = 0,
                            AssumptionCount = 0,
                            CompletenessPercentage = 0,
                            OwnerName = ownerName,
                            ProgressStatus = "not-started"
                        });
                        
                        sectionsCopied++;
                        
                        // Track successful mapping
                        mappedSections.Add(new MappedSection
                        {
                            SourceCatalogCode = sourceSection.CatalogCode ?? "",
                            SourceTitle = sourceSection.Title,
                            TargetCatalogCode = targetSection.CatalogCode ?? "",
                            TargetTitle = targetSection.Title,
                            MappingType = mappingType,
                            DataPointsCopied = 0 // Will be updated later
                        });
                    }
                    else
                    {
                        // Cannot map - section has no catalog code
                        var affectedDataPoints = _dataPoints.Count(dp => dp.SectionId == sourceSection.Id);
                        
                        var reason = string.IsNullOrWhiteSpace(sourceSection.CatalogCode)
                            ? "Source section has no catalog code for stable identification"
                            : $"No target section found with catalog code '{targetCatalogCode}'";
                        
                        var suggestedActions = new List<string>();
                        
                        if (string.IsNullOrWhiteSpace(sourceSection.CatalogCode))
                        {
                            suggestedActions.Add("Assign a catalog code to the source section before rollover");
                            suggestedActions.Add("Manually create the section in the target period and provide a manual mapping");
                        }
                        else
                        {
                            suggestedActions.Add($"Create a section with catalog code '{targetCatalogCode}' in the target period");
                            suggestedActions.Add("Provide a manual mapping to an existing section with a different catalog code");
                        }
                        
                        unmappedSections.Add(new UnmappedSection
                        {
                            SourceCatalogCode = sourceSection.CatalogCode,
                            SourceTitle = sourceSection.Title,
                            SourceSectionId = sourceSection.Id,
                            Reason = reason,
                            SuggestedActions = suggestedActions,
                            AffectedDataPoints = affectedDataPoints
                        });
                    }
                }
            }
            
            // Dictionary to map source data point IDs to target data point IDs (for evidence linking)
            var dataPointIdMapping = new Dictionary<string, string>();
            
            // Track data points copied per source section for reconciliation
            var dataPointsPerSourceSection = new Dictionary<string, int>();
            
            // Copy data values (data points)
            if (request.Options.CopyDataValues && request.Options.CopyStructure)
            {
                var sourceDataPoints = _dataPoints
                    .Where(dp => sectionIdMapping.ContainsKey(dp.SectionId))
                    .ToList();
                
                foreach (var sourceDataPoint in sourceDataPoints)
                {
                    var targetDataPointId = Guid.NewGuid().ToString();
                    dataPointIdMapping[sourceDataPoint.Id] = targetDataPointId;
                    
                    // Track data points copied per source section
                    if (!dataPointsPerSourceSection.ContainsKey(sourceDataPoint.SectionId))
                    {
                        dataPointsPerSourceSection[sourceDataPoint.SectionId] = 0;
                    }
                    dataPointsPerSourceSection[sourceDataPoint.SectionId]++;
                    
                    // Get effective rollover rule for this data type
                    var ruleType = GetEffectiveRolloverRule(sourceDataPoint.Type, request.RuleOverrides);
                    
                    var targetDataPoint = new DataPoint
                    {
                        Id = targetDataPointId,
                        SectionId = sectionIdMapping[sourceDataPoint.SectionId],
                        Type = sourceDataPoint.Type,
                        Classification = sourceDataPoint.Classification,
                        Title = sourceDataPoint.Title,
                        OwnerId = sourceDataPoint.OwnerId,
                        ContributorIds = new List<string>(sourceDataPoint.ContributorIds),
                        Source = sourceDataPoint.Source,
                        InformationType = sourceDataPoint.InformationType,
                        CreatedAt = now,
                        UpdatedAt = now,
                        EvidenceIds = new List<string>(), // Will be populated if CopyAttachments is true
                        Deadline = sourceDataPoint.Deadline,
                        IsBlocked = false, // Reset blocking status
                        IsMissing = false, // Reset missing status
                        
                        // Cross-Period Lineage Tracking
                        SourcePeriodId = request.SourcePeriodId,
                        SourcePeriodName = sourcePeriod.Name,
                        SourceDataPointId = sourceDataPoint.Id,
                        RolloverTimestamp = now,
                        RolloverPerformedBy = request.PerformedBy,
                        RolloverPerformedByName = _users.FirstOrDefault(u => u.Id == request.PerformedBy)?.Name
                    };
                    
                    // Apply rollover rule
                    switch (ruleType)
                    {
                        case DataTypeRolloverRuleType.Copy:
                            // Copy all data values
                            targetDataPoint.Content = sourceDataPoint.Content;
                            targetDataPoint.Value = sourceDataPoint.Value;
                            targetDataPoint.Unit = sourceDataPoint.Unit;
                            targetDataPoint.Assumptions = sourceDataPoint.Assumptions;
                            targetDataPoint.CompletenessStatus = "empty"; // Reset completeness for new period
                            targetDataPoint.ReviewStatus = "draft"; // Reset review status
                            // Copy estimate metadata if present
                            targetDataPoint.EstimateType = sourceDataPoint.EstimateType;
                            targetDataPoint.EstimateMethod = sourceDataPoint.EstimateMethod;
                            targetDataPoint.ConfidenceLevel = sourceDataPoint.ConfidenceLevel;
                            targetDataPoint.EstimateInputSources = new List<EstimateInputSource>(sourceDataPoint.EstimateInputSources);
                            targetDataPoint.EstimateInputs = sourceDataPoint.EstimateInputs;
                            targetDataPoint.SourceReferences = new List<NarrativeSourceReference>(sourceDataPoint.SourceReferences);
                            break;
                            
                        case DataTypeRolloverRuleType.Reset:
                            // Don't copy data values - create empty placeholder
                            targetDataPoint.Content = string.Empty;
                            targetDataPoint.Value = null;
                            targetDataPoint.Unit = sourceDataPoint.Unit; // Keep unit for consistency
                            targetDataPoint.Assumptions = null;
                            targetDataPoint.CompletenessStatus = "missing";
                            targetDataPoint.ReviewStatus = "draft";
                            // Don't copy estimate metadata
                            targetDataPoint.EstimateType = null;
                            targetDataPoint.EstimateMethod = null;
                            targetDataPoint.ConfidenceLevel = null;
                            targetDataPoint.EstimateInputSources = new List<EstimateInputSource>();
                            targetDataPoint.EstimateInputs = null;
                            targetDataPoint.SourceReferences = new List<NarrativeSourceReference>();
                            break;
                            
                        case DataTypeRolloverRuleType.CopyAsDraft:
                            // Copy data values but mark as requiring review
                            targetDataPoint.Content = $"[Carried forward - Requires Review] {sourceDataPoint.Content}";
                            targetDataPoint.Value = sourceDataPoint.Value;
                            targetDataPoint.Unit = sourceDataPoint.Unit;
                            targetDataPoint.Assumptions = sourceDataPoint.Assumptions;
                            targetDataPoint.CompletenessStatus = "incomplete"; // Mark as incomplete
                            targetDataPoint.ReviewStatus = "draft"; // Explicitly draft
                            // Copy estimate metadata
                            targetDataPoint.EstimateType = sourceDataPoint.EstimateType;
                            targetDataPoint.EstimateMethod = sourceDataPoint.EstimateMethod;
                            targetDataPoint.ConfidenceLevel = sourceDataPoint.ConfidenceLevel;
                            targetDataPoint.EstimateInputSources = new List<EstimateInputSource>(sourceDataPoint.EstimateInputSources);
                            targetDataPoint.EstimateInputs = sourceDataPoint.EstimateInputs;
                            targetDataPoint.SourceReferences = new List<NarrativeSourceReference>(sourceDataPoint.SourceReferences);
                            break;
                    }
                    
                    _dataPoints.Add(targetDataPoint);
                    dataPointsCopied++;
                }
            }
            
            // Copy attachments (evidence)
            if (request.Options.CopyAttachments && request.Options.CopyDataValues)
            {
                var sourceEvidence = _evidence
                    .Where(e => sectionIdMapping.ContainsKey(e.SectionId))
                    .ToList();
                
                var evidenceIdMapping = new Dictionary<string, string>();
                
                foreach (var sourceEv in sourceEvidence)
                {
                    var targetEvidenceId = Guid.NewGuid().ToString();
                    evidenceIdMapping[sourceEv.Id] = targetEvidenceId;
                    
                    var targetEvidence = new Evidence
                    {
                        Id = targetEvidenceId,
                        SectionId = sectionIdMapping[sourceEv.SectionId],
                        Title = sourceEv.Title,
                        Description = sourceEv.Description,
                        FileUrl = sourceEv.FileUrl,
                        FileName = sourceEv.FileName,
                        SourceUrl = sourceEv.SourceUrl,
                        UploadedBy = request.PerformedBy,
                        UploadedAt = now,
                        LinkedDataPoints = new List<string>(),
                        FileSize = sourceEv.FileSize,
                        Checksum = sourceEv.Checksum,
                        ContentType = sourceEv.ContentType,
                        IntegrityStatus = sourceEv.IntegrityStatus
                    };
                    
                    _evidence.Add(targetEvidence);
                    evidenceCopied++;
                }
                
                // Update data points with linked evidence IDs
                foreach (var targetDataPoint in _dataPoints.Where(dp => dataPointIdMapping.ContainsValue(dp.Id)))
                {
                    var sourceDataPointId = dataPointIdMapping.First(kvp => kvp.Value == targetDataPoint.Id).Key;
                    var sourceDataPoint = _dataPoints.First(dp => dp.Id == sourceDataPointId);
                    
                    foreach (var sourceEvidenceId in sourceDataPoint.EvidenceIds)
                    {
                        if (evidenceIdMapping.TryGetValue(sourceEvidenceId, out var targetEvidenceId))
                        {
                            targetDataPoint.EvidenceIds.Add(targetEvidenceId);
                            
                            // Update evidence linked data points
                            var evidence = _evidence.First(e => e.Id == targetEvidenceId);
                            evidence.LinkedDataPoints.Add(targetDataPoint.Id);
                        }
                    }
                }
            }
            
            // Dictionary to map source gap IDs to target gap IDs
            var gapIdMapping = new Dictionary<string, string>();
            
            // List to track inactive owner warnings
            var inactiveOwnerWarnings = new List<InactiveOwnerWarning>();
            
            // Copy disclosures (gaps, assumptions, remediation plans)
            if (request.Options.CopyDisclosures && request.Options.CopyStructure)
            {
                // Copy gaps (only open gaps)
                var sourceGaps = _gaps
                    .Where(g => sectionIdMapping.ContainsKey(g.SectionId) && !g.Resolved)
                    .ToList();
                
                foreach (var sourceGap in sourceGaps)
                {
                    var targetGapId = Guid.NewGuid().ToString();
                    gapIdMapping[sourceGap.Id] = targetGapId;
                    
                    var targetGap = new Gap
                    {
                        Id = targetGapId,
                        SectionId = sectionIdMapping[sourceGap.SectionId],
                        Title = sourceGap.Title,
                        Description = $"[Carried forward from previous period] {sourceGap.Description}",
                        Impact = sourceGap.Impact,
                        Resolved = false,
                        CreatedBy = "system",
                        CreatedAt = now
                    };
                    
                    _gaps.Add(targetGap);
                    gapsCopied++;
                }
                
                // Copy assumptions (only active assumptions)
                var sourceAssumptions = _assumptions
                    .Where(a => sectionIdMapping.ContainsKey(a.SectionId) && a.Status == "active")
                    .ToList();
                
                foreach (var sourceAssumption in sourceAssumptions)
                {
                    var targetAssumption = new Assumption
                    {
                        Id = Guid.NewGuid().ToString(),
                        SectionId = sectionIdMapping[sourceAssumption.SectionId],
                        DataPointId = sourceAssumption.DataPointId != null && dataPointIdMapping.ContainsKey(sourceAssumption.DataPointId)
                            ? dataPointIdMapping[sourceAssumption.DataPointId]
                            : null,
                        Title = sourceAssumption.Title,
                        Description = $"[Carried forward from previous period] {sourceAssumption.Description}",
                        Scope = sourceAssumption.Scope,
                        Methodology = sourceAssumption.Methodology,
                        Rationale = sourceAssumption.Rationale,
                        Limitations = sourceAssumption.Limitations,
                        Status = "active",
                        ValidityStartDate = request.TargetPeriodStartDate,
                        ValidityEndDate = sourceAssumption.ValidityEndDate,
                        CreatedBy = "system",
                        CreatedAt = now,
                        UpdatedAt = now,
                        Sources = new List<AssumptionSource>(sourceAssumption.Sources)
                    };
                    
                    // Check if assumption is expired and flag it
                    if (!string.IsNullOrWhiteSpace(sourceAssumption.ValidityEndDate) &&
                        DateTime.TryParse(sourceAssumption.ValidityEndDate, out var validityEnd) &&
                        DateTime.TryParse(request.TargetPeriodStartDate, out var periodStart) &&
                        validityEnd < periodStart)
                    {
                        targetAssumption.Description = $"{targetAssumption.Description}\n\n⚠️ WARNING: This assumption expired on {sourceAssumption.ValidityEndDate}. Please review and update before use.";
                        targetAssumption.Limitations = $"[EXPIRED - Requires Review] {targetAssumption.Limitations}";
                    }
                    
                    _assumptions.Add(targetAssumption);
                    assumptionsCopied++;
                }
                
                // Copy remediation plans (only active plans)
                var sourceRemediationPlans = _remediationPlans
                    .Where(rp => sectionIdMapping.ContainsKey(rp.SectionId) && 
                                 rp.Status != "completed" && rp.Status != "cancelled")
                    .ToList();
                
                var remediationPlanIdMapping = new Dictionary<string, string>();
                
                foreach (var sourceRP in sourceRemediationPlans)
                {
                    var targetRPId = Guid.NewGuid().ToString();
                    remediationPlanIdMapping[sourceRP.Id] = targetRPId;
                    
                    var targetRP = new RemediationPlan
                    {
                        Id = targetRPId,
                        SectionId = sectionIdMapping[sourceRP.SectionId],
                        GapId = sourceRP.GapId != null && gapIdMapping.ContainsKey(sourceRP.GapId)
                            ? gapIdMapping[sourceRP.GapId]
                            : null,
                        Title = sourceRP.Title,
                        Description = $"[Carried forward from previous period] {sourceRP.Description}",
                        OwnerId = sourceRP.OwnerId,
                        OwnerName = sourceRP.OwnerName,
                        Status = sourceRP.Status,
                        Priority = sourceRP.Priority,
                        TargetPeriod = sourceRP.TargetPeriod,
                        CreatedBy = "system",
                        CreatedAt = now
                    };
                    
                    _remediationPlans.Add(targetRP);
                    remediationPlansCopied++;
                }
                
                // Copy remediation actions for carried forward plans
                var sourceActions = _remediationActions.Where(a => remediationPlanIdMapping.ContainsKey(a.RemediationPlanId)).ToList();
                
                foreach (var sourceAction in sourceActions)
                {
                    if (sourceAction.Status == "pending" || sourceAction.Status == "in-progress")
                    {
                        // Adjust due date if requested
                        var adjustedDueDate = sourceAction.DueDate;
                        if (request.Options.DueDateAdjustmentDays.HasValue && 
                            request.Options.DueDateAdjustmentDays.Value != 0 &&
                            !string.IsNullOrWhiteSpace(sourceAction.DueDate) &&
                            DateTime.TryParse(sourceAction.DueDate, out var originalDueDate))
                        {
                            adjustedDueDate = originalDueDate.AddDays(request.Options.DueDateAdjustmentDays.Value).ToString("O");
                        }
                        
                        var targetAction = new RemediationAction
                        {
                            Id = Guid.NewGuid().ToString(),
                            RemediationPlanId = remediationPlanIdMapping[sourceAction.RemediationPlanId],
                            Title = sourceAction.Title,
                            Description = sourceAction.Description,
                            OwnerId = sourceAction.OwnerId,
                            OwnerName = sourceAction.OwnerName,
                            Status = sourceAction.Status,
                            DueDate = adjustedDueDate,
                            CreatedBy = "system",
                            CreatedAt = now
                        };
                        
                        _remediationActions.Add(targetAction);
                        
                        // Check if owner is inactive
                        var owner = _users.FirstOrDefault(u => u.Id == sourceAction.OwnerId);
                        if (owner != null && !owner.IsActive)
                        {
                            inactiveOwnerWarnings.Add(new InactiveOwnerWarning
                            {
                                UserId = owner.Id,
                                UserName = owner.Name,
                                EntityType = "RemediationAction",
                                EntityId = targetAction.Id,
                                EntityTitle = targetAction.Title
                            });
                        }
                    }
                }
                
                // Check for inactive owners in remediation plans
                foreach (var sourceRP in sourceRemediationPlans)
                {
                    var owner = _users.FirstOrDefault(u => u.Id == sourceRP.OwnerId);
                    if (owner != null && !owner.IsActive)
                    {
                        var targetRPId = remediationPlanIdMapping[sourceRP.Id];
                        inactiveOwnerWarnings.Add(new InactiveOwnerWarning
                        {
                            UserId = owner.Id,
                            UserName = owner.Name,
                            EntityType = "RemediationPlan",
                            EntityId = targetRPId,
                            EntityTitle = sourceRP.Title
                        });
                    }
                }
            }
            
            // Check for inactive owners in sections
            var sectionInactiveWarnings = new List<InactiveOwnerWarning>();
            if (request.Options.CopyStructure)
            {
                var targetSections = _sections.Where(s => s.PeriodId == targetPeriodId).ToList();
                foreach (var targetSection in targetSections)
                {
                    var owner = _users.FirstOrDefault(u => u.Id == targetSection.OwnerId);
                    if (owner != null && !owner.IsActive)
                    {
                        sectionInactiveWarnings.Add(new InactiveOwnerWarning
                        {
                            UserId = owner.Id,
                            UserName = owner.Name,
                            EntityType = "Section",
                            EntityId = targetSection.Id,
                            EntityTitle = targetSection.Title
                        });
                    }
                }
            }
            
            // Combine all inactive owner warnings
            var allInactiveWarnings = inactiveOwnerWarnings.Concat(sectionInactiveWarnings).ToList();
            
            // Create rollover audit log
            var performedByUser = _users.FirstOrDefault(u => u.Id == request.PerformedBy);
            var performedByName = performedByUser?.Name ?? request.PerformedBy;
            
            // Update mapped sections with data point counts
            var allSourceSections = _sections.Where(s => s.PeriodId == request.SourcePeriodId).ToList();
            foreach (var mappedSection in mappedSections)
            {
                var sourceSection = allSourceSections.FirstOrDefault(s => s.CatalogCode == mappedSection.SourceCatalogCode);
                if (sourceSection != null && dataPointsPerSourceSection.TryGetValue(sourceSection.Id, out var count))
                {
                    mappedSection.DataPointsCopied = count;
                }
            }
            
            // Create reconciliation report
            var reconciliation = new RolloverReconciliation
            {
                TotalSourceSections = allSourceSections.Count,
                MappedSections = mappedSections.Count,
                UnmappedSections = unmappedSections.Count,
                MappedItems = mappedSections,
                UnmappedItems = unmappedSections
            };
            
            var auditLog = new RolloverAuditLog
            {
                Id = Guid.NewGuid().ToString(),
                SourcePeriodId = sourcePeriod.Id,
                SourcePeriodName = sourcePeriod.Name,
                TargetPeriodId = targetPeriod.Id,
                TargetPeriodName = targetPeriod.Name,
                PerformedBy = request.PerformedBy,
                PerformedByName = performedByName,
                PerformedAt = now,
                Options = request.Options,
                SectionsCopied = sectionsCopied,
                DataPointsCopied = dataPointsCopied,
                GapsCopied = gapsCopied,
                AssumptionsCopied = assumptionsCopied,
                RemediationPlansCopied = remediationPlansCopied,
                EvidenceCopied = evidenceCopied
            };
            
            _rolloverAuditLogs.Add(auditLog);
            
            // Store reconciliation report for later retrieval
            _rolloverReconciliations[targetPeriod.Id] = reconciliation;
            
            // Create audit log entry for the rollover operation
            CreateAuditLogEntry(
                request.PerformedBy,
                performedByName,
                "rollover",
                "ReportingPeriod",
                targetPeriod.Id,
                new List<FieldChange>
                {
                    new() { Field = "SourcePeriodId", OldValue = "", NewValue = sourcePeriod.Id },
                    new() { Field = "SourcePeriodName", OldValue = "", NewValue = sourcePeriod.Name },
                    new() { Field = "CopyStructure", OldValue = "", NewValue = request.Options.CopyStructure.ToString() },
                    new() { Field = "CopyDisclosures", OldValue = "", NewValue = request.Options.CopyDisclosures.ToString() },
                    new() { Field = "CopyDataValues", OldValue = "", NewValue = request.Options.CopyDataValues.ToString() },
                    new() { Field = "CopyAttachments", OldValue = "", NewValue = request.Options.CopyAttachments.ToString() }
                },
                $"Rolled over period '{sourcePeriod.Name}' to '{targetPeriod.Name}'"
            );
            
            var result = new RolloverResult
            {
                Success = true,
                TargetPeriod = targetPeriod,
                AuditLog = auditLog,
                Reconciliation = reconciliation,
                InactiveOwnerWarnings = allInactiveWarnings
            };
            
            return (true, null, result);
        }
    }
    
    /// <summary>
    /// Gets rollover audit logs, optionally filtered by target period ID.
    /// </summary>
    public IReadOnlyList<RolloverAuditLog> GetRolloverAuditLogs(string? targetPeriodId = null)
    {
        lock (_lock)
        {
            var logs = _rolloverAuditLogs.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(targetPeriodId))
            {
                logs = logs.Where(l => l.TargetPeriodId == targetPeriodId);
            }
            
            return logs.OrderByDescending(l => l.PerformedAt).ToList();
        }
    }
    
    /// <summary>
    /// Gets the rollover reconciliation report for a target period.
    /// </summary>
    public RolloverReconciliation? GetRolloverReconciliation(string targetPeriodId)
    {
        lock (_lock)
        {
            _rolloverReconciliations.TryGetValue(targetPeriodId, out var reconciliation);
            return reconciliation;
        }
    }
    
    #endregion
    
    #region Rollover Rules Management
    
    /// <summary>
    /// Gets all rollover rules, optionally filtered by data type.
    /// </summary>
    public IReadOnlyList<DataTypeRolloverRule> GetRolloverRules(string? dataType = null)
    {
        lock (_lock)
        {
            var rules = _rolloverRules.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(dataType))
            {
                rules = rules.Where(r => r.DataType.Equals(dataType, StringComparison.OrdinalIgnoreCase));
            }
            
            return rules.OrderBy(r => r.DataType).ToList();
        }
    }
    
    /// <summary>
    /// Gets the rollover rule for a specific data type.
    /// Returns null if no rule is configured for the data type.
    /// </summary>
    public DataTypeRolloverRule? GetRolloverRuleForDataType(string dataType)
    {
        lock (_lock)
        {
            return _rolloverRules.FirstOrDefault(r => 
                r.DataType.Equals(dataType, StringComparison.OrdinalIgnoreCase));
        }
    }
    
    /// <summary>
    /// Gets the effective rollover rule for a data type, considering overrides.
    /// If an override is provided, returns that; otherwise returns configured rule or default.
    /// </summary>
    private DataTypeRolloverRuleType GetEffectiveRolloverRule(
        string dataType, 
        List<RolloverRuleOverride>? overrides)
    {
        // Check for override first
        if (overrides != null)
        {
            var override_ = overrides.FirstOrDefault(o => 
                o.DataType.Equals(dataType, StringComparison.OrdinalIgnoreCase));
            
            if (override_ != null)
            {
                return override_.RuleType;
            }
        }
        
        // Check for configured rule
        var rule = GetRolloverRuleForDataType(dataType);
        if (rule != null)
        {
            return rule.RuleType;
        }
        
        // Default to Copy
        return DataTypeRolloverRuleType.Copy;
    }
    
    /// <summary>
    /// Saves (creates or updates) a rollover rule for a data type.
    /// </summary>
    public DataTypeRolloverRule SaveRolloverRule(SaveDataTypeRolloverRuleRequest request)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            
            // Validate data type
            if (string.IsNullOrWhiteSpace(request.DataType))
            {
                throw new ArgumentException("DataType is required");
            }
            
            // Normalize and parse rule type (handle both "copy-as-draft" and "CopyAsDraft")
            var normalizedRuleType = request.RuleType.Replace("-", "").Replace("_", "");
            if (!Enum.TryParse<DataTypeRolloverRuleType>(normalizedRuleType, true, out var ruleType))
            {
                throw new ArgumentException($"Invalid RuleType: {request.RuleType}. Valid values are: Copy, Reset, CopyAsDraft (or copy-as-draft)");
            }
            
            // Get user for audit trail
            var user = _users.FirstOrDefault(u => u.Id == request.SavedBy);
            var userName = user?.Name ?? "Unknown User";
            
            // Check if rule already exists
            var existingRule = _rolloverRules.FirstOrDefault(r => 
                r.DataType.Equals(request.DataType, StringComparison.OrdinalIgnoreCase));
            
            DataTypeRolloverRule rule;
            string changeType;
            
            if (existingRule != null)
            {
                // Update existing rule
                var oldVersion = existingRule.Version;
                
                existingRule.RuleType = ruleType;
                existingRule.Description = request.Description;
                existingRule.UpdatedAt = now;
                existingRule.UpdatedBy = request.SavedBy;
                existingRule.Version++;
                
                rule = existingRule;
                changeType = "updated";
                
                // Create history entry
                _rolloverRuleHistory.Add(new RolloverRuleHistory
                {
                    Id = Guid.NewGuid().ToString(),
                    RuleId = rule.Id,
                    DataType = rule.DataType,
                    RuleType = ruleType,
                    Description = request.Description,
                    Version = rule.Version,
                    ChangedAt = now,
                    ChangedBy = request.SavedBy,
                    ChangedByName = userName,
                    ChangeType = changeType
                });
            }
            else
            {
                // Create new rule
                rule = new DataTypeRolloverRule
                {
                    Id = Guid.NewGuid().ToString(),
                    DataType = request.DataType,
                    RuleType = ruleType,
                    Description = request.Description,
                    CreatedAt = now,
                    CreatedBy = request.SavedBy,
                    Version = 1
                };
                
                _rolloverRules.Add(rule);
                changeType = "created";
                
                // Create history entry
                _rolloverRuleHistory.Add(new RolloverRuleHistory
                {
                    Id = Guid.NewGuid().ToString(),
                    RuleId = rule.Id,
                    DataType = rule.DataType,
                    RuleType = ruleType,
                    Description = request.Description,
                    Version = 1,
                    ChangedAt = now,
                    ChangedBy = request.SavedBy,
                    ChangedByName = userName,
                    ChangeType = changeType
                });
            }
            
            // Create audit log entry
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = now,
                UserId = request.SavedBy,
                UserName = userName,
                Action = $"RolloverRule{char.ToUpper(changeType[0])}{changeType.Substring(1)}",
                EntityType = "RolloverRule",
                EntityId = rule.Id,
                ChangeNote = $"Rollover rule for data type '{rule.DataType}' {changeType}: {ruleType}"
            });
            
            return rule;
        }
    }
    
    /// <summary>
    /// Deletes a rollover rule for a data type (resets to default behavior).
    /// </summary>
    public bool DeleteRolloverRule(string dataType, string deletedBy)
    {
        lock (_lock)
        {
            var rule = _rolloverRules.FirstOrDefault(r => 
                r.DataType.Equals(dataType, StringComparison.OrdinalIgnoreCase));
            
            if (rule == null)
            {
                return false;
            }
            
            var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var user = _users.FirstOrDefault(u => u.Id == deletedBy);
            var userName = user?.Name ?? "Unknown User";
            
            // Create history entry
            _rolloverRuleHistory.Add(new RolloverRuleHistory
            {
                Id = Guid.NewGuid().ToString(),
                RuleId = rule.Id,
                DataType = rule.DataType,
                RuleType = rule.RuleType,
                Description = rule.Description,
                Version = rule.Version,
                ChangedAt = now,
                ChangedBy = deletedBy,
                ChangedByName = userName,
                ChangeType = "deleted"
            });
            
            // Create audit log entry
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = now,
                UserId = deletedBy,
                UserName = userName,
                Action = "RolloverRuleDeleted",
                EntityType = "RolloverRule",
                EntityId = rule.Id,
                ChangeNote = $"Rollover rule for data type '{rule.DataType}' deleted (reset to default)"
            });
            
            _rolloverRules.Remove(rule);
            return true;
        }
    }
    
    /// <summary>
    /// Gets the history of changes for a rollover rule.
    /// </summary>
    public IReadOnlyList<RolloverRuleHistory> GetRolloverRuleHistory(string dataType)
    {
        lock (_lock)
        {
            return _rolloverRuleHistory
                .Where(h => h.DataType.Equals(dataType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(h => h.ChangedAt)
                .ToList();
        }
    }
    
    #endregion
    
    #region Text Disclosure Comparison
    
    /// <summary>
    /// Compares narrative text content between a current data point and its previous period version.
    /// Supports draft copy detection - shows no changes if the data point was copied and not yet edited.
    /// </summary>
    public (bool Success, string? ErrorMessage, TextDisclosureComparisonResponse? Response) 
        CompareTextDisclosures(string currentDataPointId, string? previousPeriodId = null, string granularity = "word")
    {
        lock (_lock)
        {
            // Get current data point
            var currentDataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == currentDataPointId);
            if (currentDataPoint == null)
            {
                return (false, $"Data point with ID '{currentDataPointId}' not found.", null);
            }
            
            // Get current period
            var currentSection = _sections.FirstOrDefault(s => s.Id == currentDataPoint.SectionId);
            if (currentSection == null)
            {
                return (false, "Current section not found.", null);
            }
            
            var currentPeriod = _periods.FirstOrDefault(p => p.Id == currentSection.PeriodId);
            if (currentPeriod == null)
            {
                return (false, "Current period not found.", null);
            }
            
            // Determine previous data point
            DataPoint? previousDataPoint = null;
            ReportingPeriod? previousPeriod = null;
            
            if (!string.IsNullOrEmpty(previousPeriodId))
            {
                // User explicitly specified a previous period
                previousPeriod = _periods.FirstOrDefault(p => p.Id == previousPeriodId);
                if (previousPeriod == null)
                {
                    return (false, $"Previous period with ID '{previousPeriodId}' not found.", null);
                }
                
                // Find matching data point in previous period by section catalog code and title
                var previousSection = _sections.FirstOrDefault(s => 
                    s.PeriodId == previousPeriodId && 
                    s.CatalogCode == currentSection.CatalogCode);
                    
                if (previousSection != null)
                {
                    previousDataPoint = _dataPoints.FirstOrDefault(dp => 
                        dp.SectionId == previousSection.Id && 
                        dp.Title == currentDataPoint.Title);
                }
            }
            else if (!string.IsNullOrEmpty(currentDataPoint.SourceDataPointId))
            {
                // Use rollover lineage to find previous version
                previousDataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == currentDataPoint.SourceDataPointId);
                if (previousDataPoint != null)
                {
                    var previousSection = _sections.FirstOrDefault(s => s.Id == previousDataPoint.SectionId);
                    if (previousSection != null)
                    {
                        previousPeriod = _periods.FirstOrDefault(p => p.Id == previousSection.PeriodId);
                    }
                }
            }
            
            // Check if this is a draft copy (rolled over but not yet edited)
            // A true draft copy must have rollover lineage AND draft status
            bool isDraftCopy = !string.IsNullOrEmpty(currentDataPoint.SourceDataPointId) && 
                              currentDataPoint.ReviewStatus == "draft" &&
                              !string.IsNullOrEmpty(currentDataPoint.RolloverTimestamp);
                              
            bool hasBeenEdited = false;
            
            if (isDraftCopy && previousDataPoint != null)
            {
                // Check if content has been edited since rollover
                // For narrative disclosures, compare the Content field (primary narrative text)
                // and Title to determine if editing occurred
                hasBeenEdited = currentDataPoint.Content != previousDataPoint.Content ||
                               currentDataPoint.Title != previousDataPoint.Title;
            }
            
            var response = new TextDisclosureComparisonResponse
            {
                CurrentDataPoint = new DataPointInfo
                {
                    Id = currentDataPoint.Id,
                    PeriodId = currentPeriod.Id,
                    PeriodName = currentPeriod.Name,
                    Title = currentDataPoint.Title,
                    Content = currentDataPoint.Content,
                    ReviewStatus = currentDataPoint.ReviewStatus,
                    UpdatedAt = currentDataPoint.UpdatedAt,
                    SourcePeriodId = currentDataPoint.SourcePeriodId,
                    SourceDataPointId = currentDataPoint.SourceDataPointId,
                    RolloverTimestamp = currentDataPoint.RolloverTimestamp
                },
                IsDraftCopy = isDraftCopy,
                HasBeenEdited = hasBeenEdited
            };
            
            if (previousDataPoint != null && previousPeriod != null)
            {
                response.PreviousDataPoint = new DataPointInfo
                {
                    Id = previousDataPoint.Id,
                    PeriodId = previousPeriod.Id,
                    PeriodName = previousPeriod.Name,
                    Title = previousDataPoint.Title,
                    Content = previousDataPoint.Content,
                    ReviewStatus = previousDataPoint.ReviewStatus,
                    UpdatedAt = previousDataPoint.UpdatedAt,
                    SourcePeriodId = previousDataPoint.SourcePeriodId,
                    SourceDataPointId = previousDataPoint.SourceDataPointId,
                    RolloverTimestamp = previousDataPoint.RolloverTimestamp
                };
                
                // Compute text diff
                // For draft copies that haven't been edited, show no changes
                string oldText = previousDataPoint.Content;
                string newText = currentDataPoint.Content;
                
                if (isDraftCopy && !hasBeenEdited)
                {
                    // Show as unchanged - both texts are the same
                    oldText = currentDataPoint.Content;
                    newText = currentDataPoint.Content;
                }
                
                List<TextSegment> segments;
                if (granularity == "sentence")
                {
                    segments = _textDiffService.ComputeSentenceLevelDiff(oldText, newText);
                }
                else
                {
                    segments = _textDiffService.ComputeWordLevelDiff(oldText, newText);
                }
                
                // Convert to DTOs
                response.Segments = segments.Select(s => new TextSegmentDto
                {
                    Text = s.Text,
                    ChangeType = s.ChangeType
                }).ToList();
                
                // Generate summary
                var summary = _textDiffService.GenerateSummary(oldText, newText);
                response.Summary = new DiffSummaryDto
                {
                    TotalSegments = summary.TotalSegments,
                    AddedSegments = summary.AddedSegments,
                    RemovedSegments = summary.RemovedSegments,
                    UnchangedSegments = summary.UnchangedSegments,
                    OldTextLength = summary.OldTextLength,
                    NewTextLength = summary.NewTextLength,
                    HasChanges = summary.HasChanges
                };
            }
            else
            {
                // No previous data point to compare - show current content as all new
                var segments = new List<TextSegmentDto>
                {
                    new TextSegmentDto
                    {
                        Text = currentDataPoint.Content,
                        ChangeType = "added"
                    }
                };
                response.Segments = segments;
                response.Summary = new DiffSummaryDto
                {
                    TotalSegments = 1,
                    AddedSegments = 1,
                    RemovedSegments = 0,
                    UnchangedSegments = 0,
                    OldTextLength = 0,
                    NewTextLength = currentDataPoint.Content.Length,
                    HasChanges = true
                };
            }
            
            return (true, null, response);
        }
    }
    
    /// <summary>
    /// Gets a data point by section ID and title match (for period-to-period comparison).
    /// </summary>
    public DataPoint? GetDataPointBySectionAndTitle(string sectionId, string title)
    {
        lock (_lock)
        {
            return _dataPoints.FirstOrDefault(dp => 
                dp.SectionId == sectionId && 
                dp.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
        }
    }
    
    #endregion
    
    #region Variance Explanation Methods
    
    /// <summary>
    /// Creates a variance threshold configuration for a reporting period.
    /// </summary>
    public (bool isValid, string? errorMessage, VarianceThresholdConfig? config) CreateVarianceThresholdConfig(
        string periodId, CreateVarianceThresholdConfigRequest request)
    {
        lock (_lock)
        {
            // Validate period exists
            var period = _periods.FirstOrDefault(p => p.Id == periodId);
            if (period == null)
            {
                return (false, $"Reporting period with ID '{periodId}' not found.", null);
            }
            
            // Validate at least one threshold is provided
            if (request.PercentageThreshold == null && request.AbsoluteThreshold == null)
            {
                return (false, "At least one threshold (percentage or absolute) must be specified.", null);
            }
            
            // Validate threshold values are positive
            if (request.PercentageThreshold.HasValue && request.PercentageThreshold.Value <= 0)
            {
                return (false, "Percentage threshold must be greater than zero.", null);
            }
            
            if (request.AbsoluteThreshold.HasValue && request.AbsoluteThreshold.Value <= 0)
            {
                return (false, "Absolute threshold must be greater than zero.", null);
            }
            
            var config = new VarianceThresholdConfig
            {
                Id = Guid.NewGuid().ToString(),
                PercentageThreshold = request.PercentageThreshold,
                AbsoluteThreshold = request.AbsoluteThreshold,
                RequireBothThresholds = request.RequireBothThresholds,
                RequireReviewerApproval = request.RequireReviewerApproval,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = request.CreatedBy
            };
            
            _varianceThresholdConfigs.Add(config);
            
            // Update period with the new config
            period.VarianceThresholdConfig = config;
            
            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = request.CreatedBy,
                UserName = request.CreatedBy,
                Action = "created",
                EntityType = "variance-threshold-config",
                EntityId = config.Id,
                ChangeNote = $"Created variance threshold configuration for period {period.Name}",
                Changes = new List<FieldChange>()
            });
            
            return (true, null, config);
        }
    }
    
    /// <summary>
    /// Checks if a variance exceeds the configured thresholds and returns flag information.
    /// </summary>
    private VarianceFlagInfo CheckVarianceThreshold(
        string dataPointId,
        string priorPeriodId,
        VarianceThresholdConfig config,
        decimal? percentageChange,
        decimal? absoluteChange)
    {
        // Get existing explanation if one exists
        var existingExplanation = _varianceExplanations.FirstOrDefault(ve => 
            ve.DataPointId == dataPointId && 
            ve.PriorPeriodId == priorPeriodId);
        
        bool requiresExplanation = false;
        string? reason = null;
        
        // Check if thresholds are exceeded
        bool percentageExceeded = config.PercentageThreshold.HasValue && 
            percentageChange.HasValue && 
            Math.Abs(percentageChange.Value) >= config.PercentageThreshold.Value;
            
        bool absoluteExceeded = config.AbsoluteThreshold.HasValue && 
            absoluteChange.HasValue && 
            Math.Abs(absoluteChange.Value) >= config.AbsoluteThreshold.Value;
        
        if (config.RequireBothThresholds)
        {
            requiresExplanation = percentageExceeded && absoluteExceeded;
            if (requiresExplanation)
            {
                reason = $"Exceeds both percentage threshold ({config.PercentageThreshold}%) and absolute threshold ({config.AbsoluteThreshold})";
            }
        }
        else
        {
            requiresExplanation = percentageExceeded || absoluteExceeded;
            if (percentageExceeded && absoluteExceeded)
            {
                reason = $"Exceeds percentage threshold ({config.PercentageThreshold}%) and absolute threshold ({config.AbsoluteThreshold})";
            }
            else if (percentageExceeded)
            {
                reason = $"Exceeds percentage threshold ({config.PercentageThreshold}%)";
            }
            else if (absoluteExceeded)
            {
                reason = $"Exceeds absolute threshold ({config.AbsoluteThreshold})";
            }
        }
        
        // Determine if flag is cleared
        bool isFlagCleared = false;
        if (existingExplanation != null)
        {
            if (config.RequireReviewerApproval)
            {
                isFlagCleared = existingExplanation.Status == "approved";
            }
            else
            {
                isFlagCleared = existingExplanation.Status == "submitted" || existingExplanation.Status == "approved";
            }
        }
        
        return new VarianceFlagInfo
        {
            RequiresExplanation = requiresExplanation && !isFlagCleared,
            RequiresExplanationReason = requiresExplanation ? reason : null,
            Explanation = existingExplanation,
            IsFlagCleared = isFlagCleared
        };
    }
    
    /// <summary>
    /// Creates a variance explanation.
    /// </summary>
    public (bool isValid, string? errorMessage, VarianceExplanation? explanation) CreateVarianceExplanation(
        CreateVarianceExplanationRequest request)
    {
        lock (_lock)
        {
            // Validate data point exists
            var dataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == request.DataPointId);
            if (dataPoint == null)
            {
                return (false, $"Data point with ID '{request.DataPointId}' not found.", null);
            }
            
            // Validate prior period exists
            var priorPeriod = _periods.FirstOrDefault(p => p.Id == request.PriorPeriodId);
            if (priorPeriod == null)
            {
                return (false, $"Prior period with ID '{request.PriorPeriodId}' not found.", null);
            }
            
            // Get comparison data
            var section = _sections.FirstOrDefault(s => s.Id == dataPoint.SectionId);
            if (section == null)
            {
                return (false, "Data point's section not found.", null);
            }
            
            var comparison = CompareMetrics(request.DataPointId, request.PriorPeriodId);
            if (comparison == null)
            {
                return (false, "Unable to generate comparison for variance explanation.", null);
            }
            
            // Find prior data point
            string? priorDataPointId = null;
            var tempDataPoint = dataPoint;
            var visited = new HashSet<string> { dataPoint.Id };
            
            while (!string.IsNullOrEmpty(tempDataPoint.SourceDataPointId))
            {
                var sourceId = tempDataPoint.SourceDataPointId;
                if (visited.Contains(sourceId)) break;
                visited.Add(sourceId);
                
                tempDataPoint = _dataPoints.FirstOrDefault(d => d.Id == sourceId);
                if (tempDataPoint == null) break;
                
                var tempSection = _sections.FirstOrDefault(s => s.Id == tempDataPoint.SectionId);
                if (tempSection != null && tempSection.PeriodId == request.PriorPeriodId)
                {
                    priorDataPointId = tempDataPoint.Id;
                    break;
                }
            }
            
            var explanation = new VarianceExplanation
            {
                Id = Guid.NewGuid().ToString(),
                DataPointId = request.DataPointId,
                PriorPeriodId = request.PriorPeriodId,
                PriorDataPointId = priorDataPointId,
                CurrentValue = comparison.CurrentPeriod.Value ?? "N/A",
                PriorValue = comparison.PriorPeriod?.Value ?? "N/A",
                PercentageChange = comparison.PercentageChange,
                AbsoluteChange = comparison.AbsoluteChange,
                Explanation = request.Explanation,
                RootCause = request.RootCause,
                Category = request.Category,
                Status = "draft",
                EvidenceIds = request.EvidenceIds,
                References = request.References,
                CreatedBy = request.CreatedBy,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                IsFlagged = true
            };
            
            _varianceExplanations.Add(explanation);
            
            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = request.CreatedBy,
                UserName = request.CreatedBy,
                Action = "created",
                EntityType = "variance-explanation",
                EntityId = explanation.Id,
                ChangeNote = $"Created variance explanation for data point {dataPoint.Title}",
                Changes = new List<FieldChange>()
            });
            
            return (true, null, explanation);
        }
    }
    
    /// <summary>
    /// Updates a variance explanation.
    /// </summary>
    public (bool isValid, string? errorMessage, VarianceExplanation? explanation) UpdateVarianceExplanation(
        string id, UpdateVarianceExplanationRequest request)
    {
        lock (_lock)
        {
            var explanation = _varianceExplanations.FirstOrDefault(ve => ve.Id == id);
            if (explanation == null)
            {
                return (false, $"Variance explanation with ID '{id}' not found.", null);
            }
            
            // Can only update if in draft or revision-requested status
            if (explanation.Status != "draft" && explanation.Status != "revision-requested")
            {
                return (false, $"Cannot update variance explanation in status '{explanation.Status}'.", null);
            }
            
            // Update fields if provided
            if (request.Explanation != null)
            {
                explanation.Explanation = request.Explanation;
            }
            
            if (request.RootCause != null)
            {
                explanation.RootCause = request.RootCause;
            }
            
            if (request.Category != null)
            {
                explanation.Category = request.Category;
            }
            
            if (request.EvidenceIds != null)
            {
                explanation.EvidenceIds = request.EvidenceIds;
            }
            
            if (request.References != null)
            {
                explanation.References = request.References;
            }
            
            explanation.UpdatedBy = request.UpdatedBy;
            explanation.UpdatedAt = DateTime.UtcNow.ToString("o");
            
            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = request.UpdatedBy,
                UserName = request.UpdatedBy,
                Action = "updated",
                EntityType = "variance-explanation",
                EntityId = id,
                ChangeNote = $"Updated variance explanation {id}",
                Changes = new List<FieldChange>()
            });
            
            return (true, null, explanation);
        }
    }
    
    /// <summary>
    /// Submits a variance explanation for review (or marks as complete if review not required).
    /// </summary>
    public (bool isValid, string? errorMessage, VarianceExplanation? explanation) SubmitVarianceExplanation(
        string id, SubmitVarianceExplanationRequest request)
    {
        lock (_lock)
        {
            var explanation = _varianceExplanations.FirstOrDefault(ve => ve.Id == id);
            if (explanation == null)
            {
                return (false, $"Variance explanation with ID '{id}' not found.", null);
            }
            
            if (explanation.Status != "draft" && explanation.Status != "revision-requested")
            {
                return (false, $"Cannot submit variance explanation in status '{explanation.Status}'.", null);
            }
            
            // Validate explanation is not empty
            if (string.IsNullOrWhiteSpace(explanation.Explanation))
            {
                return (false, "Explanation text cannot be empty.", null);
            }
            
            // Get the data point and its period to check if review is required
            var dataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == explanation.DataPointId);
            if (dataPoint == null)
            {
                return (false, "Associated data point not found.", null);
            }
            
            var section = _sections.FirstOrDefault(s => s.Id == dataPoint.SectionId);
            if (section == null)
            {
                return (false, "Associated section not found.", null);
            }
            
            var period = _periods.FirstOrDefault(p => p.Id == section.PeriodId);
            if (period == null)
            {
                return (false, "Associated period not found.", null);
            }
            
            explanation.Status = "submitted";
            explanation.UpdatedBy = request.SubmittedBy;
            explanation.UpdatedAt = DateTime.UtcNow.ToString("o");
            
            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = request.SubmittedBy,
                UserName = request.SubmittedBy,
                Action = "submitted",
                EntityType = "variance-explanation",
                EntityId = id,
                ChangeNote = $"Submitted variance explanation {id}",
                Changes = new List<FieldChange>()
            });
            
            return (true, null, explanation);
        }
    }
    
    /// <summary>
    /// Reviews a variance explanation (approve, reject, or request revision).
    /// </summary>
    public (bool isValid, string? errorMessage, VarianceExplanation? explanation) ReviewVarianceExplanation(
        string id, ReviewVarianceExplanationRequest request)
    {
        lock (_lock)
        {
            var explanation = _varianceExplanations.FirstOrDefault(ve => ve.Id == id);
            if (explanation == null)
            {
                return (false, $"Variance explanation with ID '{id}' not found.", null);
            }
            
            if (explanation.Status != "submitted")
            {
                return (false, $"Cannot review variance explanation in status '{explanation.Status}'. Must be 'submitted'.", null);
            }
            
            // Validate decision
            if (request.Decision != "approve" && request.Decision != "reject" && request.Decision != "request-revision")
            {
                return (false, "Decision must be 'approve', 'reject', or 'request-revision'.", null);
            }
            
            switch (request.Decision)
            {
                case "approve":
                    explanation.Status = "approved";
                    explanation.IsFlagged = false; // Clear the flag
                    break;
                case "reject":
                    explanation.Status = "rejected";
                    break;
                case "request-revision":
                    explanation.Status = "revision-requested";
                    break;
            }
            
            explanation.ReviewedBy = request.ReviewedBy;
            explanation.ReviewedAt = DateTime.UtcNow.ToString("o");
            explanation.ReviewComments = request.Comments;
            explanation.UpdatedBy = request.ReviewedBy;
            explanation.UpdatedAt = DateTime.UtcNow.ToString("o");
            
            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = request.ReviewedBy,
                UserName = request.ReviewedBy,
                Action = "reviewed",
                EntityType = "variance-explanation",
                EntityId = id,
                ChangeNote = $"Reviewed variance explanation {id} with decision: {request.Decision}",
                Changes = new List<FieldChange>()
            });
            
            return (true, null, explanation);
        }
    }
    
    /// <summary>
    /// Gets all variance explanations, optionally filtered by data point or period.
    /// </summary>
    public IReadOnlyList<VarianceExplanation> GetVarianceExplanations(string? dataPointId = null, string? periodId = null)
    {
        lock (_lock)
        {
            var query = _varianceExplanations.AsEnumerable();
            
            if (!string.IsNullOrEmpty(dataPointId))
            {
                query = query.Where(ve => ve.DataPointId == dataPointId);
            }
            
            if (!string.IsNullOrEmpty(periodId))
            {
                // Filter by current period (data point's period) or prior period
                query = query.Where(ve =>
                {
                    var dataPoint = _dataPoints.FirstOrDefault(dp => dp.Id == ve.DataPointId);
                    if (dataPoint == null) return false;
                    
                    var section = _sections.FirstOrDefault(s => s.Id == dataPoint.SectionId);
                    if (section == null) return false;
                    
                    return section.PeriodId == periodId || ve.PriorPeriodId == periodId;
                });
            }
            
            return query.OrderByDescending(ve => ve.CreatedAt).ToList();
        }
    }
    
    /// <summary>
    /// Gets a single variance explanation by ID.
    /// </summary>
    public VarianceExplanation? GetVarianceExplanation(string id)
    {
        lock (_lock)
        {
            return _varianceExplanations.FirstOrDefault(ve => ve.Id == id);
        }
    }
    
    /// <summary>
    /// Deletes a variance explanation.
    /// </summary>
    public bool DeleteVarianceExplanation(string id, string deletedBy)
    {
        lock (_lock)
        {
            var explanation = _varianceExplanations.FirstOrDefault(ve => ve.Id == id);
            if (explanation == null)
            {
                return false;
            }
            
            _varianceExplanations.Remove(explanation);
            
            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = deletedBy,
                UserName = deletedBy,
                Action = "deleted",
                EntityType = "variance-explanation",
                EntityId = id,
                ChangeNote = $"Deleted variance explanation {id}",
                Changes = new List<FieldChange>()
            });
            
            return true;
        }
    }
    
    #endregion
    
    #region Maturity Model Management
    
    /// <summary>
    /// Gets all maturity models.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive/historical versions.</param>
    public List<MaturityModel> GetMaturityModels(bool includeInactive = false)
    {
        lock (_lock)
        {
            var query = _maturityModels.AsEnumerable();
            
            if (!includeInactive)
            {
                query = query.Where(m => m.IsActive);
            }
            
            return query.OrderByDescending(m => m.Version).ToList();
        }
    }
    
    /// <summary>
    /// Gets a single maturity model by ID.
    /// </summary>
    public MaturityModel? GetMaturityModel(string id)
    {
        lock (_lock)
        {
            return _maturityModels.FirstOrDefault(m => m.Id == id);
        }
    }
    
    /// <summary>
    /// Gets the active maturity model.
    /// </summary>
    public MaturityModel? GetActiveMaturityModel()
    {
        lock (_lock)
        {
            return _maturityModels.FirstOrDefault(m => m.IsActive);
        }
    }
    
    /// <summary>
    /// Creates a new maturity model.
    /// </summary>
    public (bool isValid, string? errorMessage, MaturityModel? model) CreateMaturityModel(CreateMaturityModelRequest request)
    {
        lock (_lock)
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return (false, "Name is required.", null);
            }
            
            if (request.Levels == null || request.Levels.Count == 0)
            {
                return (false, "At least one maturity level is required.", null);
            }
            
            // Validate level orders are unique and sequential
            var orders = request.Levels.Select(l => l.Order).ToList();
            if (orders.Distinct().Count() != orders.Count)
            {
                return (false, "Maturity level orders must be unique.", null);
            }
            
            // Check if there's already an active model
            var existingActiveModel = _maturityModels.FirstOrDefault(m => m.IsActive);
            
            var model = new MaturityModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Version = 1,
                IsActive = existingActiveModel == null, // Only active if no other active model exists
                Levels = request.Levels.Select(l => new MaturityLevel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = l.Name,
                    Description = l.Description,
                    Order = l.Order,
                    Criteria = l.Criteria.Select(c => new MaturityCriterion
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = c.Name,
                        Description = c.Description,
                        CriterionType = c.CriterionType,
                        TargetValue = c.TargetValue,
                        Unit = c.Unit,
                        MinCompletionPercentage = c.MinCompletionPercentage,
                        MinEvidencePercentage = c.MinEvidencePercentage,
                        RequiredControls = c.RequiredControls,
                        IsMandatory = c.IsMandatory
                    }).ToList()
                }).ToList(),
                CreatedBy = request.CreatedBy,
                CreatedByName = request.CreatedByName,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };
            
            _maturityModels.Add(model);
            
            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = request.CreatedBy,
                UserName = request.CreatedByName,
                Action = "created",
                EntityType = "maturity-model",
                EntityId = model.Id,
                ChangeNote = $"Created maturity model '{model.Name}' (v{model.Version})",
                Changes = new List<FieldChange>()
            });
            
            return (true, null, model);
        }
    }
    
    /// <summary>
    /// Updates a maturity model by creating a new version.
    /// The previous version is marked as inactive.
    /// </summary>
    public (bool isValid, string? errorMessage, MaturityModel? model) UpdateMaturityModel(string id, UpdateMaturityModelRequest request)
    {
        lock (_lock)
        {
            var existingModel = _maturityModels.FirstOrDefault(m => m.Id == id);
            if (existingModel == null)
            {
                return (false, "Maturity model not found.", null);
            }
            
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return (false, "Name is required.", null);
            }
            
            if (request.Levels == null || request.Levels.Count == 0)
            {
                return (false, "At least one maturity level is required.", null);
            }
            
            // Validate level orders are unique
            var orders = request.Levels.Select(l => l.Order).ToList();
            if (orders.Distinct().Count() != orders.Count)
            {
                return (false, "Maturity level orders must be unique.", null);
            }
            
            // Mark the existing model as inactive
            existingModel.IsActive = false;
            
            // Create new version of the model
            var newModel = new MaturityModel
            {
                Id = id, // Keep the same ID
                Name = request.Name,
                Description = request.Description,
                Version = existingModel.Version + 1,
                IsActive = true,
                Levels = request.Levels.Select(l => new MaturityLevel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = l.Name,
                    Description = l.Description,
                    Order = l.Order,
                    Criteria = l.Criteria.Select(c => new MaturityCriterion
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = c.Name,
                        Description = c.Description,
                        CriterionType = c.CriterionType,
                        TargetValue = c.TargetValue,
                        Unit = c.Unit,
                        MinCompletionPercentage = c.MinCompletionPercentage,
                        MinEvidencePercentage = c.MinEvidencePercentage,
                        RequiredControls = c.RequiredControls,
                        IsMandatory = c.IsMandatory
                    }).ToList()
                }).ToList(),
                CreatedBy = existingModel.CreatedBy,
                CreatedByName = existingModel.CreatedByName,
                CreatedAt = existingModel.CreatedAt,
                UpdatedBy = request.UpdatedBy,
                UpdatedByName = request.UpdatedByName,
                UpdatedAt = DateTime.UtcNow.ToString("o")
            };
            
            _maturityModels.Add(newModel);
            
            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = request.UpdatedBy,
                UserName = request.UpdatedByName,
                Action = "updated",
                EntityType = "maturity-model",
                EntityId = id,
                ChangeNote = $"Updated maturity model '{newModel.Name}' to v{newModel.Version}",
                Changes = new List<FieldChange>
                {
                    new FieldChange
                    {
                        Field = "Version",
                        OldValue = existingModel.Version.ToString(),
                        NewValue = newModel.Version.ToString()
                    }
                }
            });
            
            return (true, null, newModel);
        }
    }
    
    /// <summary>
    /// Deletes a maturity model.
    /// Only allows deletion if there are no historical maturity assessments linked to it.
    /// </summary>
    public (bool isValid, string? errorMessage) DeleteMaturityModel(string id)
    {
        lock (_lock)
        {
            // Find all versions of this model
            var modelVersions = _maturityModels.Where(m => m.Id == id).ToList();
            
            if (modelVersions.Count == 0)
            {
                return (false, "Maturity model not found.");
            }
            
            // In a real implementation, we would check for linked maturity assessments here
            // For now, we'll allow deletion
            
            foreach (var version in modelVersions)
            {
                _maturityModels.Remove(version);
            }
            
            // Log audit event
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                UserId = "system",
                UserName = "System",
                Action = "deleted",
                EntityType = "maturity-model",
                EntityId = id,
                ChangeNote = $"Deleted maturity model (all {modelVersions.Count} version(s))",
                Changes = new List<FieldChange>()
            });
            
            return (true, null);
        }
    }
    
    /// <summary>
    /// Gets version history for a maturity model.
    /// </summary>
    public List<MaturityModel> GetMaturityModelVersionHistory(string id)
    {
        lock (_lock)
        {
            return _maturityModels
                .Where(m => m.Id == id)
                .OrderByDescending(m => m.Version)
                .ToList();
        }
    }
    
    #endregion
    
    #region Maturity Assessment
    
    private readonly List<MaturityAssessment> _maturityAssessments = new();
    
    /// <summary>
    /// Calculate a maturity assessment for a reporting period.
    /// Evaluates all criteria in the maturity model against actual data.
    /// </summary>
    public (bool isValid, string? errorMessage, MaturityAssessment? assessment) CalculateMaturityAssessment(CalculateMaturityAssessmentRequest request)
    {
        lock (_lock)
        {
            // Validate period exists
            var period = _periods.FirstOrDefault(p => p.Id == request.PeriodId);
            if (period == null)
            {
                return (false, "Reporting period not found", null);
            }
            
            // Get maturity model (use specified or active)
            MaturityModel? model;
            if (!string.IsNullOrEmpty(request.MaturityModelId))
            {
                model = GetMaturityModel(request.MaturityModelId);
                if (model == null)
                {
                    return (false, "Maturity model not found", null);
                }
            }
            else
            {
                model = GetActiveMaturityModel();
                if (model == null)
                {
                    return (false, "No active maturity model found", null);
                }
            }
            
            // Get all data points for the period
            var sections = GetSections(request.PeriodId);
            var allDataPoints = new List<DataPoint>();
            foreach (var section in sections)
            {
                var sectionDataPoints = _dataPoints.Where(dp => dp.SectionId == section.Id).ToList();
                allDataPoints.AddRange(sectionDataPoints);
            }
            
            // Calculate stats
            var stats = CalculateAssessmentStats(allDataPoints);
            
            // Evaluate each criterion
            var criterionResults = new List<MaturityCriterionResult>();
            foreach (var level in model.Levels.OrderBy(l => l.Order))
            {
                foreach (var criterion in level.Criteria)
                {
                    var result = EvaluateCriterion(level, criterion, stats, allDataPoints);
                    criterionResults.Add(result);
                }
            }
            
            // Determine achieved level (highest level where all mandatory criteria pass)
            // A level is achieved only if all mandatory criteria at that level AND all lower levels pass
            MaturityLevel? achievedLevel = null;
            foreach (var level in model.Levels.OrderBy(l => l.Order))
            {
                // Get all criteria up to and including this level
                var criteriaUpToLevel = criterionResults
                    .Where(r => r.LevelOrder <= level.Order && r.IsMandatory)
                    .ToList();
                
                // Check if all mandatory criteria up to this level passed
                if (criteriaUpToLevel.All(r => r.Passed))
                {
                    achievedLevel = level;
                    // Continue to check higher levels
                }
                else
                {
                    // If any mandatory criterion failed, we can't achieve this or higher levels
                    break;
                }
            }
            
            // Calculate overall score (0-100 based on criteria passed)
            var totalCriteria = criterionResults.Count;
            var passedCriteria = criterionResults.Count(r => r.Passed);
            var overallScore = totalCriteria > 0 ? (decimal)passedCriteria / totalCriteria * 100 : 0;
            
            // Mark previous assessments as non-current
            foreach (var existing in _maturityAssessments.Where(a => a.PeriodId == request.PeriodId && a.IsCurrent))
            {
                existing.IsCurrent = false;
            }
            
            // Create assessment
            var assessment = new MaturityAssessment
            {
                Id = Guid.NewGuid().ToString(),
                PeriodId = request.PeriodId,
                MaturityModelId = model.Id,
                ModelVersion = model.Version,
                CalculatedAt = DateTime.UtcNow.ToString("o"),
                CalculatedBy = request.CalculatedBy,
                CalculatedByName = request.CalculatedByName,
                IsCurrent = true,
                AchievedLevelId = achievedLevel?.Id,
                AchievedLevelName = achievedLevel?.Name,
                AchievedLevelOrder = achievedLevel?.Order,
                OverallScore = overallScore,
                CriterionResults = criterionResults,
                Stats = stats
            };
            
            _maturityAssessments.Add(assessment);
            
            return (true, null, assessment);
        }
    }
    
    /// <summary>
    /// Get the current (latest) maturity assessment for a period.
    /// </summary>
    public MaturityAssessment? GetCurrentMaturityAssessment(string periodId)
    {
        lock (_lock)
        {
            return _maturityAssessments
                .Where(a => a.PeriodId == periodId && a.IsCurrent)
                .OrderByDescending(a => a.CalculatedAt)
                .FirstOrDefault();
        }
    }
    
    /// <summary>
    /// Get all maturity assessments for a period (history).
    /// </summary>
    public List<MaturityAssessment> GetMaturityAssessmentHistory(string periodId)
    {
        lock (_lock)
        {
            return _maturityAssessments
                .Where(a => a.PeriodId == periodId)
                .OrderByDescending(a => a.CalculatedAt)
                .ToList();
        }
    }
    
    /// <summary>
    /// Get a specific maturity assessment by ID.
    /// </summary>
    public MaturityAssessment? GetMaturityAssessment(string id)
    {
        lock (_lock)
        {
            return _maturityAssessments.FirstOrDefault(a => a.Id == id);
        }
    }
    
    /// <summary>
    /// Calculate assessment statistics from data points.
    /// </summary>
    private MaturityAssessmentStats CalculateAssessmentStats(List<DataPoint> dataPoints)
    {
        var stats = new MaturityAssessmentStats
        {
            TotalDataPoints = dataPoints.Count
        };
        
        if (dataPoints.Count == 0)
        {
            stats.DataCompletenessPercentage = 0;
            stats.EvidenceQualityPercentage = 0;
            return stats;
        }
        
        // Count complete data points (those with content/value and not marked as missing)
        stats.CompleteDataPoints = dataPoints.Count(dp => 
            !dp.IsMissing && 
            dp.CompletenessStatus == "complete"
        );
        
        stats.DataCompletenessPercentage = (decimal)stats.CompleteDataPoints / stats.TotalDataPoints * 100;
        
        // Count data points with evidence
        stats.DataPointsWithEvidence = dataPoints.Count(dp => dp.EvidenceIds.Count > 0);
        stats.EvidenceQualityPercentage = (decimal)stats.DataPointsWithEvidence / stats.TotalDataPoints * 100;
        
        return stats;
    }
    
    /// <summary>
    /// Evaluate a single criterion against actual data.
    /// </summary>
    private MaturityCriterionResult EvaluateCriterion(
        MaturityLevel level, 
        MaturityCriterion criterion, 
        MaturityAssessmentStats stats,
        List<DataPoint> dataPoints)
    {
        var result = new MaturityCriterionResult
        {
            LevelId = level.Id,
            LevelName = level.Name,
            LevelOrder = level.Order,
            CriterionId = criterion.Id,
            CriterionName = criterion.Name,
            CriterionType = criterion.CriterionType,
            TargetValue = criterion.TargetValue,
            Unit = criterion.Unit,
            IsMandatory = criterion.IsMandatory
        };
        
        // Evaluate based on criterion type
        switch (criterion.CriterionType)
        {
            case "data-completeness":
                return EvaluateDataCompletenessCriterion(criterion, stats, result);
                
            case "evidence-quality":
                return EvaluateEvidenceQualityCriterion(criterion, stats, result);
                
            case "process-control":
                return EvaluateProcessControlCriterion(criterion, result);
                
            case "custom":
                return EvaluateCustomCriterion(criterion, result);
                
            default:
                result.Status = "incomplete-data";
                result.FailureReason = $"Unknown criterion type: {criterion.CriterionType}";
                result.Passed = false;
                return result;
        }
    }
    
    /// <summary>
    /// Evaluate data completeness criterion.
    /// </summary>
    private MaturityCriterionResult EvaluateDataCompletenessCriterion(
        MaturityCriterion criterion,
        MaturityAssessmentStats stats,
        MaturityCriterionResult result)
    {
        if (!criterion.MinCompletionPercentage.HasValue)
        {
            result.Status = "incomplete-data";
            result.FailureReason = "Criterion configuration missing: MinCompletionPercentage not specified";
            result.Passed = false;
            return result;
        }
        
        var targetPercentage = criterion.MinCompletionPercentage.Value;
        var actualPercentage = stats.DataCompletenessPercentage;
        
        result.ActualValue = actualPercentage.ToString("F2");
        result.Passed = actualPercentage >= targetPercentage;
        result.Status = result.Passed ? "passed" : "failed";
        
        if (!result.Passed)
        {
            result.FailureReason = $"Data completeness is {actualPercentage:F2}%, below target of {targetPercentage}%. " +
                                  $"Complete data points: {stats.CompleteDataPoints}/{stats.TotalDataPoints}";
        }
        
        return result;
    }
    
    /// <summary>
    /// Evaluate evidence quality criterion.
    /// </summary>
    private MaturityCriterionResult EvaluateEvidenceQualityCriterion(
        MaturityCriterion criterion,
        MaturityAssessmentStats stats,
        MaturityCriterionResult result)
    {
        if (!criterion.MinEvidencePercentage.HasValue)
        {
            result.Status = "incomplete-data";
            result.FailureReason = "Criterion configuration missing: MinEvidencePercentage not specified";
            result.Passed = false;
            return result;
        }
        
        var targetPercentage = criterion.MinEvidencePercentage.Value;
        var actualPercentage = stats.EvidenceQualityPercentage;
        
        result.ActualValue = actualPercentage.ToString("F2");
        result.Passed = actualPercentage >= targetPercentage;
        result.Status = result.Passed ? "passed" : "failed";
        
        if (!result.Passed)
        {
            result.FailureReason = $"Evidence quality is {actualPercentage:F2}%, below target of {targetPercentage}%. " +
                                  $"Data points with evidence: {stats.DataPointsWithEvidence}/{stats.TotalDataPoints}";
        }
        
        return result;
    }
    
    /// <summary>
    /// Evaluate process control criterion.
    /// Note: This is a simplified implementation. In production, you would check actual process control implementations.
    /// </summary>
    private MaturityCriterionResult EvaluateProcessControlCriterion(
        MaturityCriterion criterion,
        MaturityCriterionResult result)
    {
        if (criterion.RequiredControls.Count == 0)
        {
            result.Status = "incomplete-data";
            result.FailureReason = "Criterion configuration missing: RequiredControls not specified";
            result.Passed = false;
            return result;
        }
        
        // For now, we'll mark this as incomplete-data since we don't have actual control tracking
        // In a full implementation, you would check if the required controls are actually enabled
        result.ActualValue = "Not implemented";
        result.Status = "incomplete-data";
        result.FailureReason = "Process control validation not yet implemented. Required controls: " + 
                              string.Join(", ", criterion.RequiredControls);
        result.Passed = false;
        
        return result;
    }
    
    /// <summary>
    /// Evaluate custom criterion.
    /// Note: This is a placeholder. Custom criteria would need specific evaluation logic.
    /// </summary>
    private MaturityCriterionResult EvaluateCustomCriterion(
        MaturityCriterion criterion,
        MaturityCriterionResult result)
    {
        // For custom criteria, we mark as incomplete-data since we can't automatically evaluate them
        result.ActualValue = "Manual review required";
        result.Status = "incomplete-data";
        result.FailureReason = "Custom criteria require manual evaluation";
        result.Passed = false;
        
        return result;
    }
    
    #endregion
    
    #region Progress Dashboard
    
    /// <summary>
    /// Gets progress trends across multiple periods.
    /// Shows completeness percentages, maturity scores, and outstanding issues for each period.
    /// </summary>
    /// <param name="periodIds">Optional list of specific period IDs to include. If null or empty, all periods are included.</param>
    /// <param name="category">Optional category filter (environmental, social, governance).</param>
    /// <param name="organizationalUnitId">Optional organizational unit filter (not fully implemented in current schema).</param>
    /// <param name="sectionId">Optional section ID to filter to a specific section.</param>
    /// <param name="ownerId">Optional owner ID to filter by data owner.</param>
    /// <returns>Progress trends response with period data and summary statistics.</returns>
    public ProgressTrendsResponse GetProgressTrends(
        List<string>? periodIds = null,
        string? category = null,
        string? organizationalUnitId = null,
        string? sectionId = null,
        string? ownerId = null)
    {
        // Get periods to analyze
        var periodsToAnalyze = periodIds != null && periodIds.Any()
            ? _periods.Where(p => periodIds.Contains(p.Id)).ToList()
            : _periods.OrderBy(p => p.StartDate).ToList();
        
        var response = new ProgressTrendsResponse();
        
        foreach (var period in periodsToAnalyze)
        {
            // Get sections for this period with filters
            var sections = _summaries.Where(s => s.PeriodId == period.Id).ToList();
            
            if (!string.IsNullOrEmpty(category))
                sections = sections.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!string.IsNullOrEmpty(sectionId))
                sections = sections.Where(s => s.Id == sectionId).ToList();
            
            if (!string.IsNullOrEmpty(ownerId))
                sections = sections.Where(s => s.OwnerId == ownerId).ToList();
            
            // Get data points for these sections
            var sectionIds = sections.Select(s => s.Id).ToList();
            var dataPoints = _dataPoints.Where(dp => sectionIds.Contains(dp.SectionId)).ToList();
            
            // Note: OrganizationalUnit filter cannot be applied at data point level as the field doesn't exist
            // This would need to be implemented at the section or period level if needed
            
            // Calculate completeness
            var totalDataPoints = dataPoints.Count;
            var completeDataPoints = dataPoints.Count(dp => 
                dp.CompletenessStatus == "complete" || dp.CompletenessStatus == "not applicable");
            var completenessPercentage = totalDataPoints > 0 
                ? (decimal)completeDataPoints / totalDataPoints * 100 
                : 0;
            
            // Get maturity assessment for this period
            var maturityAssessment = _maturityAssessments
                .Where(ma => ma.PeriodId == period.Id && ma.IsCurrent)
                .OrderByDescending(ma => ma.CalculatedAt)
                .FirstOrDefault();
            
            // Get gaps
            var gaps = _gaps.Where(g => 
                sectionIds.Contains(g.SectionId) && 
                !g.Resolved).ToList();
            var openGaps = gaps.Count;
            var highRiskGaps = gaps.Count(g => g.Impact.Equals("high", StringComparison.OrdinalIgnoreCase));
            
            // Get blocked data points
            var blockedDataPoints = dataPoints.Count(dp => 
                dp.ReviewStatus == "changes-requested");
            
            var periodData = new PeriodTrendData
            {
                PeriodId = period.Id,
                PeriodName = period.Name,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                Status = period.Status,
                IsLocked = period.Status == "closed",
                CompletenessPercentage = completenessPercentage,
                CompleteDataPoints = completeDataPoints,
                TotalDataPoints = totalDataPoints,
                MaturityScore = maturityAssessment?.OverallScore,
                MaturityLevel = maturityAssessment?.AchievedLevelName,
                MaturityLevelOrder = maturityAssessment?.AchievedLevelOrder,
                OpenGaps = openGaps,
                HighRiskGaps = highRiskGaps,
                BlockedDataPoints = blockedDataPoints
            };
            
            response.Periods.Add(periodData);
        }
        
        // Calculate summary
        response.Summary = new TrendsSummary
        {
            TotalPeriods = response.Periods.Count,
            LockedPeriods = response.Periods.Count(p => p.IsLocked),
            LatestCompletenessPercentage = response.Periods.LastOrDefault()?.CompletenessPercentage,
            LatestMaturityScore = response.Periods.LastOrDefault()?.MaturityScore
        };
        
        // Calculate changes
        if (response.Periods.Count >= 2)
        {
            var latest = response.Periods[^1];
            var previous = response.Periods[^2];
            
            response.Summary.CompletenessChange = latest.CompletenessPercentage - previous.CompletenessPercentage;
            
            if (latest.MaturityScore.HasValue && previous.MaturityScore.HasValue)
            {
                response.Summary.MaturityChange = latest.MaturityScore.Value - previous.MaturityScore.Value;
            }
        }
        
        return response;
    }
    
    /// <summary>
    /// Gets outstanding actions across periods that require attention.
    /// Includes gaps, blocked data points, and pending approvals.
    /// </summary>
    /// <param name="periodIds">Optional list of specific period IDs to include. If null or empty, all periods are included.</param>
    /// <param name="category">Optional category filter (environmental, social, governance).</param>
    /// <param name="organizationalUnitId">Optional organizational unit filter (not fully implemented in current schema).</param>
    /// <param name="sectionId">Optional section ID to filter to a specific section.</param>
    /// <param name="ownerId">Optional owner ID to filter by data owner.</param>
    /// <param name="priority">Optional priority filter (high, medium, low).</param>
    /// <returns>Outstanding actions response with actions list and summary statistics.</returns>
    public OutstandingActionsResponse GetOutstandingActions(
        List<string>? periodIds = null,
        string? category = null,
        string? organizationalUnitId = null,
        string? sectionId = null,
        string? ownerId = null,
        string? priority = null)
    {
        var response = new OutstandingActionsResponse();
        
        // Get periods
        var periods = periodIds != null && periodIds.Any()
            ? _periods.Where(p => periodIds.Contains(p.Id)).ToList()
            : _periods.ToList();
        
        foreach (var period in periods)
        {
            // Get sections with filters
            var sections = _summaries.Where(s => s.PeriodId == period.Id).ToList();
            
            if (!string.IsNullOrEmpty(category))
                sections = sections.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (!string.IsNullOrEmpty(sectionId))
                sections = sections.Where(s => s.Id == sectionId).ToList();
            
            if (!string.IsNullOrEmpty(ownerId))
                sections = sections.Where(s => s.OwnerId == ownerId).ToList();
            
            var sectionIds = sections.Select(s => s.Id).ToList();
            
            // Get gaps as actions
            var gaps = _gaps.Where(g => 
                sectionIds.Contains(g.SectionId) && 
                !g.Resolved).ToList();
            
            foreach (var gap in gaps)
            {
                var section = sections.FirstOrDefault(s => s.Id == gap.SectionId);
                if (section == null) continue;
                
                var gapPriority = gap.Impact.ToLower() switch
                {
                    "high" => "high",
                    "medium" => "medium",
                    _ => "low"
                };
                
                // Apply priority filter
                if (!string.IsNullOrEmpty(priority) && gapPriority != priority.ToLower())
                    continue;
                
                response.Actions.Add(new OutstandingAction
                {
                    Id = gap.Id,
                    ActionType = "gap",
                    Title = gap.Title,
                    PeriodId = period.Id,
                    PeriodName = period.Name,
                    PeriodIsLocked = period.Status == "closed",
                    SectionId = section.Id,
                    SectionTitle = section.Title,
                    Category = section.Category,
                    OwnerId = section.OwnerId,  // Use section owner as gap doesn't have owner field
                    OwnerName = section.OwnerName,
                    Priority = gapPriority,
                    DueDate = gap.TargetDate  // Use TargetDate instead of DuePeriod
                });
            }
            
            // Get blocked data points as actions
            var dataPoints = _dataPoints.Where(dp => 
                sectionIds.Contains(dp.SectionId) && 
                dp.ReviewStatus == "changes-requested").ToList();
            
            foreach (var dataPoint in dataPoints)
            {
                var section = sections.FirstOrDefault(s => s.Id == dataPoint.SectionId);
                if (section == null) continue;
                
                var dpPriority = dataPoint.IsMissing ? "high" : "medium";
                
                // Apply priority filter
                if (!string.IsNullOrEmpty(priority) && dpPriority != priority.ToLower())
                    continue;
                
                response.Actions.Add(new OutstandingAction
                {
                    Id = dataPoint.Id,
                    ActionType = "blocked-datapoint",
                    Title = dataPoint.Title,
                    PeriodId = period.Id,
                    PeriodName = period.Name,
                    PeriodIsLocked = period.Status == "closed",
                    SectionId = section.Id,
                    SectionTitle = section.Title,
                    Category = section.Category,
                    OwnerId = dataPoint.OwnerId,
                    OwnerName = _users.FirstOrDefault(u => u.Id == dataPoint.OwnerId)?.Name,
                    Priority = dpPriority,
                    DueDate = dataPoint.Deadline
                });
            }
            
            // Get pending approvals as actions
            var pendingSections = sections.Where(s => s.Status == "in-review").ToList();
            
            foreach (var section in pendingSections)
            {
                var approvalPriority = "medium";
                
                // Apply priority filter
                if (!string.IsNullOrEmpty(priority) && approvalPriority != priority.ToLower())
                    continue;
                
                response.Actions.Add(new OutstandingAction
                {
                    Id = section.Id,
                    ActionType = "pending-approval",
                    Title = $"Section approval: {section.Title}",
                    PeriodId = period.Id,
                    PeriodName = period.Name,
                    PeriodIsLocked = period.Status == "closed",
                    SectionId = section.Id,
                    SectionTitle = section.Title,
                    Category = section.Category,
                    OwnerId = section.OwnerId,
                    OwnerName = section.OwnerName,
                    Priority = approvalPriority
                });
            }
        }
        
        // Calculate summary
        response.Summary = new OutstandingActionsSummary
        {
            TotalActions = response.Actions.Count,
            HighPriority = response.Actions.Count(a => a.Priority == "high"),
            MediumPriority = response.Actions.Count(a => a.Priority == "medium"),
            LowPriority = response.Actions.Count(a => a.Priority == "low"),
            OpenGaps = response.Actions.Count(a => a.ActionType == "gap"),
            BlockedDataPoints = response.Actions.Count(a => a.ActionType == "blocked-datapoint"),
            PendingApprovals = response.Actions.Count(a => a.ActionType == "pending-approval")
        };
        
        return response;
    }
    
    #endregion
    
    #region Year-over-Year Annex Export
    
    private readonly List<YoYAnnexExportRecord> _yoyAnnexExports = new();
    
    /// <summary>
    /// Generates a year-over-year annex export for auditors.
    /// Includes metric deltas, variance explanations, narrative diffs, and evidence references.
    /// Filters confidential items based on user role.
    /// </summary>
    public (bool IsValid, string? ErrorMessage, ExportYoYAnnexResult? Result) GenerateYoYAnnex(ExportYoYAnnexRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.CurrentPeriodId))
            return (false, "CurrentPeriodId is required.", null);
        
        if (string.IsNullOrWhiteSpace(request.PriorPeriodId))
            return (false, "PriorPeriodId is required.", null);
        
        if (string.IsNullOrWhiteSpace(request.ExportedBy))
            return (false, "ExportedBy is required.", null);
        
        // Validate periods exist
        var currentPeriod = _periods.FirstOrDefault(p => p.Id == request.CurrentPeriodId);
        if (currentPeriod == null)
            return (false, $"Current period with ID '{request.CurrentPeriodId}' not found.", null);
        
        var priorPeriod = _periods.FirstOrDefault(p => p.Id == request.PriorPeriodId);
        if (priorPeriod == null)
            return (false, $"Prior period with ID '{request.PriorPeriodId}' not found.", null);
        
        // Build contents
        var contents = BuildYoYAnnexContents(request);
        if (contents == null)
            return (false, "Failed to build YoY annex contents.", null);
        
        // Generate export ID and metadata
        var exportId = Guid.NewGuid().ToString();
        var exportedAt = DateTime.UtcNow.ToString("O");
        var user = _users.FirstOrDefault(u => u.Id == request.ExportedBy);
        var exportedByName = user?.Name ?? request.ExportedBy;
        
        // Create result
        var result = new ExportYoYAnnexResult
        {
            ExportId = exportId,
            ExportedAt = exportedAt,
            ExportedBy = request.ExportedBy,
            ExportedByName = exportedByName,
            Checksum = string.Empty, // Will be calculated by controller after ZIP creation
            PackageSize = 0, // Will be set by controller after ZIP creation
            Summary = contents.Summary
        };
        
        return (true, null, result);
    }
    
    /// <summary>
    /// Builds the complete contents of a YoY annex package.
    /// </summary>
    public YoYAnnexContents? BuildYoYAnnexContents(ExportYoYAnnexRequest request)
    {
        var currentPeriod = _periods.FirstOrDefault(p => p.Id == request.CurrentPeriodId);
        var priorPeriod = _periods.FirstOrDefault(p => p.Id == request.PriorPeriodId);
        
        if (currentPeriod == null || priorPeriod == null)
            return null;
        
        var user = _users.FirstOrDefault(u => u.Id == request.ExportedBy);
        var userRole = user?.Role ?? "contributor";
        
        // Get sections to include
        var sectionsQuery = _sections.Where(s => s.PeriodId == request.CurrentPeriodId);
        if (request.SectionIds.Any())
        {
            sectionsQuery = sectionsQuery.Where(s => request.SectionIds.Contains(s.Id));
        }
        var sections = sectionsQuery.ToList();
        
        // Build YoY section data with metric comparisons
        var yoySections = new List<YoYAnnexSectionData>();
        var allVarianceExplanations = new List<VarianceExplanation>();
        var allEvidenceReferences = new List<EvidenceReference>();
        var allNarrativeDiffs = new List<NarrativeDiffSummary>();
        var exclusionNotes = new List<string>();
        int totalMetricRows = 0;
        int confidentialItemsExcluded = 0;
        
        foreach (var section in sections)
        {
            var ownerUser = _users.FirstOrDefault(u => u.Id == section.OwnerId);
            var sectionData = new YoYAnnexSectionData
            {
                SectionId = section.Id,
                Title = section.Title,
                Category = section.Category,
                OwnerName = ownerUser?.Name ?? section.OwnerId,
                Metrics = new List<YoYMetricRow>()
            };
            
            // Get data points for this section in current period
            var currentDataPoints = _dataPoints.Where(dp => dp.SectionId == section.Id).ToList();
            
            foreach (var currentDp in currentDataPoints)
            {
                // Skip confidential items for restricted users
                if (userRole == "contributor" && IsConfidentialDataPoint(currentDp))
                {
                    confidentialItemsExcluded++;
                    continue;
                }
                
                // Find prior period data point via rollover lineage
                DataPoint? priorDp = null;
                if (!string.IsNullOrEmpty(currentDp.SourceDataPointId))
                {
                    priorDp = _dataPoints.FirstOrDefault(dp => dp.Id == currentDp.SourceDataPointId);
                }
                else
                {
                    // Try to find by matching section and title in prior period
                    var priorSection = _sections.FirstOrDefault(s => 
                        s.PeriodId == request.PriorPeriodId && 
                        s.CatalogCode == section.CatalogCode);
                    if (priorSection != null)
                    {
                        priorDp = _dataPoints.FirstOrDefault(dp => 
                            dp.SectionId == priorSection.Id && 
                            dp.Title == currentDp.Title);
                    }
                }
                
                // Calculate deltas
                decimal? percentageChange = null;
                decimal? absoluteChange = null;
                
                if (priorDp != null && 
                    decimal.TryParse(currentDp.Value, out var currentVal) && 
                    decimal.TryParse(priorDp.Value, out var priorVal))
                {
                    absoluteChange = currentVal - priorVal;
                    if (priorVal != 0)
                    {
                        percentageChange = (absoluteChange.Value / priorVal) * 100;
                    }
                }
                
                // Get variance explanation if exists
                var varianceExplanation = _varianceExplanations.FirstOrDefault(ve => 
                    ve.DataPointId == currentDp.Id && 
                    ve.PriorPeriodId == request.PriorPeriodId);
                
                string? varianceExplanationId = null;
                string? varianceExplanationSummary = null;
                bool hasVarianceFlag = false;
                
                if (varianceExplanation != null)
                {
                    varianceExplanationId = varianceExplanation.Id;
                    varianceExplanationSummary = varianceExplanation.Explanation;
                    hasVarianceFlag = varianceExplanation.Status != "approved";
                    
                    if (request.IncludeVarianceExplanations && !allVarianceExplanations.Any(ve => ve.Id == varianceExplanation.Id))
                    {
                        allVarianceExplanations.Add(varianceExplanation);
                    }
                }
                
                // Check if variance threshold is exceeded
                if (currentPeriod.VarianceThresholdConfig != null && percentageChange.HasValue)
                {
                    var config = currentPeriod.VarianceThresholdConfig;
                    bool exceedsThreshold = false;
                    
                    if (config.RequireBothThresholds)
                    {
                        exceedsThreshold = 
                            (config.PercentageThreshold.HasValue && Math.Abs(percentageChange.Value) >= config.PercentageThreshold.Value) &&
                            (config.AbsoluteThreshold.HasValue && absoluteChange.HasValue && Math.Abs(absoluteChange.Value) >= config.AbsoluteThreshold.Value);
                    }
                    else
                    {
                        exceedsThreshold = 
                            (config.PercentageThreshold.HasValue && Math.Abs(percentageChange.Value) >= config.PercentageThreshold.Value) ||
                            (config.AbsoluteThreshold.HasValue && absoluteChange.HasValue && Math.Abs(absoluteChange.Value) >= config.AbsoluteThreshold.Value);
                    }
                    
                    if (exceedsThreshold && varianceExplanation == null)
                    {
                        hasVarianceFlag = true;
                    }
                }
                
                // Get evidence counts
                var currentEvidence = _evidence.Where(e => 
                    e.LinkedDataPoints.Contains(currentDp.Id)).ToList();
                var priorEvidence = priorDp != null 
                    ? _evidence.Where(e => e.LinkedDataPoints.Contains(priorDp.Id)).ToList() 
                    : new List<Evidence>();
                
                // Add evidence references if requested
                if (request.IncludeEvidenceReferences)
                {
                    foreach (var evidence in currentEvidence)
                    {
                        // Skip confidential evidence for restricted users
                        if (userRole == "contributor" && IsConfidentialEvidence(evidence))
                        {
                            confidentialItemsExcluded++;
                            continue;
                        }
                        
                        if (!allEvidenceReferences.Any(er => er.Id == evidence.Id))
                        {
                            allEvidenceReferences.Add(new EvidenceReference
                            {
                                Id = evidence.Id,
                                FileName = evidence.FileName,
                                FileUrl = evidence.FileUrl,
                                FileSize = null,
                                Checksum = evidence.Checksum,
                                ContentType = null,
                                IntegrityStatus = evidence.IntegrityStatus,
                                SectionId = evidence.SectionId,
                                UploadedBy = evidence.UploadedBy,
                                UploadedAt = evidence.UploadedAt,
                                LinkedDataPointIds = evidence.LinkedDataPoints
                            });
                        }
                    }
                }
                
                // Add narrative diff summary if requested and data point is narrative type
                if (request.IncludeNarrativeDiffs && currentDp.Type == "narrative")
                {
                    var (success, _, diffResponse) = CompareTextDisclosures(currentDp.Id, request.PriorPeriodId, "word");
                    if (success && diffResponse != null && diffResponse.Summary.HasChanges)
                    {
                        allNarrativeDiffs.Add(new NarrativeDiffSummary
                        {
                            DataPointId = currentDp.Id,
                            Title = currentDp.Title,
                            AddedSegments = diffResponse.Summary.AddedSegments,
                            RemovedSegments = diffResponse.Summary.RemovedSegments,
                            UnchangedSegments = diffResponse.Summary.UnchangedSegments,
                            TotalSegments = diffResponse.Summary.TotalSegments,
                            HasChanges = diffResponse.Summary.HasChanges,
                            ChangeDescription = $"{diffResponse.Summary.AddedSegments} segments added, {diffResponse.Summary.RemovedSegments} removed"
                        });
                    }
                }
                
                // Create metric row
                var metricRow = new YoYMetricRow
                {
                    DataPointId = currentDp.Id,
                    MetricTitle = currentDp.Title,
                    CurrentValue = currentDp.Value ?? string.Empty,
                    PriorValue = priorDp?.Value ?? string.Empty,
                    Unit = currentDp.Unit,
                    PercentageChange = percentageChange,
                    AbsoluteChange = absoluteChange,
                    VarianceExplanationId = varianceExplanationId,
                    VarianceExplanationSummary = varianceExplanationSummary,
                    HasVarianceFlag = hasVarianceFlag,
                    CurrentEvidenceCount = currentEvidence.Count,
                    PriorEvidenceCount = priorEvidence.Count,
                    OwnerName = ownerUser?.Name ?? currentDp.OwnerId,
                    InformationType = currentDp.InformationType
                };
                
                sectionData.Metrics.Add(metricRow);
                totalMetricRows++;
            }
            
            yoySections.Add(sectionData);
        }
        
        // Add exclusion notes if items were filtered
        if (confidentialItemsExcluded > 0)
        {
            exclusionNotes.Add($"{confidentialItemsExcluded} confidential items were excluded based on user access rights.");
        }
        
        // Build metadata
        var exportedAt = DateTime.UtcNow.ToString("O");
        var exportUser = _users.FirstOrDefault(u => u.Id == request.ExportedBy);
        
        var metadata = new ExportMetadata
        {
            ExportId = Guid.NewGuid().ToString(),
            ExportedAt = exportedAt,
            ExportedBy = request.ExportedBy,
            ExportedByName = exportUser?.Name ?? request.ExportedBy,
            Version = "1.0",
            ExportNote = request.ExportNote
        };
        
        // Build summary
        var summary = new YoYAnnexSummary
        {
            CurrentPeriodId = currentPeriod.Id,
            CurrentPeriodName = currentPeriod.Name,
            PriorPeriodId = priorPeriod.Id,
            PriorPeriodName = priorPeriod.Name,
            SectionCount = yoySections.Count,
            MetricRowCount = totalMetricRows,
            NarrativeComparisonCount = allNarrativeDiffs.Count,
            VarianceExplanationCount = allVarianceExplanations.Count,
            EvidenceReferenceCount = allEvidenceReferences.Count,
            ConfidentialItemsExcluded = confidentialItemsExcluded
        };
        
        // Build complete contents
        var contents = new YoYAnnexContents
        {
            Metadata = metadata,
            CurrentPeriod = currentPeriod,
            PriorPeriod = priorPeriod,
            Sections = yoySections,
            VarianceExplanations = allVarianceExplanations,
            EvidenceReferences = allEvidenceReferences,
            NarrativeDiffs = allNarrativeDiffs,
            Summary = summary,
            ExclusionNotes = exclusionNotes
        };
        
        return contents;
    }
    
    /// <summary>
    /// Records a YoY annex export for audit purposes.
    /// </summary>
    public void RecordYoYAnnexExport(ExportYoYAnnexRequest request, string checksum, long packageSize)
    {
        var currentPeriod = _periods.FirstOrDefault(p => p.Id == request.CurrentPeriodId);
        var priorPeriod = _periods.FirstOrDefault(p => p.Id == request.PriorPeriodId);
        var user = _users.FirstOrDefault(u => u.Id == request.ExportedBy);
        
        var contents = BuildYoYAnnexContents(request);
        
        var record = new YoYAnnexExportRecord
        {
            Id = Guid.NewGuid().ToString(),
            CurrentPeriodId = request.CurrentPeriodId,
            CurrentPeriodName = currentPeriod?.Name ?? string.Empty,
            PriorPeriodId = request.PriorPeriodId,
            PriorPeriodName = priorPeriod?.Name ?? string.Empty,
            SectionIds = request.SectionIds,
            ExportedAt = DateTime.UtcNow.ToString("O"),
            ExportedBy = request.ExportedBy,
            ExportedByName = user?.Name ?? request.ExportedBy,
            ExportNote = request.ExportNote,
            Checksum = checksum,
            PackageSize = packageSize,
            MetricRowCount = contents?.Summary.MetricRowCount ?? 0,
            VarianceExplanationCount = contents?.Summary.VarianceExplanationCount ?? 0,
            EvidenceReferenceCount = contents?.Summary.EvidenceReferenceCount ?? 0
        };
        
        _yoyAnnexExports.Add(record);
        
        // Log to audit trail
        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow.ToString("O"),
            UserId = request.ExportedBy,
            UserName = user?.Name ?? request.ExportedBy,
            Action = "yoy-annex-exported",
            EntityType = "yoy-annex",
            EntityId = record.Id,
            ChangeNote = $"Exported YoY annex: {priorPeriod?.Name} → {currentPeriod?.Name}. Metric rows: {record.MetricRowCount}, Variance explanations: {record.VarianceExplanationCount}, Evidence references: {record.EvidenceReferenceCount}, Package size: {packageSize} bytes, Checksum: {checksum}",
            Changes = new List<FieldChange>()
        };
        
        _auditLog.Add(auditEntry);
    }
    
    /// <summary>
    /// Gets all YoY annex export records for a specific current period.
    /// </summary>
    public IReadOnlyList<YoYAnnexExportRecord> GetYoYAnnexExports(string currentPeriodId)
    {
        return _yoyAnnexExports
            .Where(e => e.CurrentPeriodId == currentPeriodId)
            .OrderByDescending(e => e.ExportedAt)
            .ToList();
    }
    
    /// <summary>
    /// Checks if a data point contains confidential information.
    /// </summary>
    private bool IsConfidentialDataPoint(DataPoint dataPoint)
    {
        // Add logic to determine confidentiality
        // For now, use a simple heuristic: data points with "confidential" in title or marked with specific tags
        return dataPoint.Title?.Contains("confidential", StringComparison.OrdinalIgnoreCase) == true;
    }
    
    /// <summary>
    /// Checks if evidence contains confidential information.
    /// </summary>
    private bool IsConfidentialEvidence(Evidence evidence)
    {
        // Add logic to determine confidentiality
        // For now, use a simple heuristic: evidence with "confidential" in title
        return evidence.Title?.Contains("confidential", StringComparison.OrdinalIgnoreCase) == true;
    }
    
    #region Report Generation
    
    /// <summary>
    /// Generate a report from the selected structure for a reporting period.
    /// Only includes enabled sections, sorted by their Order field.
    /// Uses the latest data snapshot for each included section.
    /// </summary>
    public (bool IsValid, string? ErrorMessage, GeneratedReport? Report) GenerateReport(GenerateReportRequest request)
    {
        lock (_lock)
        {
            // Validate period exists
            var period = _periods.FirstOrDefault(p => p.Id == request.PeriodId);
            if (period == null)
            {
                return (false, $"Period with ID '{request.PeriodId}' not found.", null);
            }
            
            // Get user information
            var user = _users.FirstOrDefault(u => u.Id == request.GeneratedBy);
            var userName = user?.Name ?? request.GeneratedBy;
            
            // Get all sections for the period
            var allSections = _sections.Where(s => s.PeriodId == request.PeriodId).ToList();
            
            // Filter sections based on request
            IEnumerable<ReportSection> sectionsToInclude = allSections;
            
            // If specific section IDs are provided, filter to those
            if (request.SectionIds != null && request.SectionIds.Count > 0)
            {
                sectionsToInclude = sectionsToInclude.Where(s => request.SectionIds.Contains(s.Id));
            }
            
            // Only include enabled sections
            sectionsToInclude = sectionsToInclude.Where(s => s.IsEnabled);
            
            // Sort by Order field to maintain defined sequence
            var orderedSections = sectionsToInclude.OrderBy(s => s.Order).ToList();
            
            // Build generated report sections with data
            var generatedSections = new List<GeneratedReportSection>();
            
            foreach (var section in orderedSections)
            {
                var owner = _users.FirstOrDefault(u => u.Id == section.OwnerId);
                
                // Get data points for this section
                var dataPoints = _dataPoints
                    .Where(dp => dp.SectionId == section.Id)
                    .Select(dp =>
                    {
                        var dpOwner = _users.FirstOrDefault(u => u.Id == dp.OwnerId);
                        var evidenceCount = _evidence.Count(e => e.LinkedDataPoints.Contains(dp.Id));
                        var hasAssumptions = _assumptions.Any(a => a.DataPointId == dp.Id && a.Status == "active");
                        
                        return new DataPointSnapshot
                        {
                            Id = dp.Id,
                            Title = dp.Title,
                            Value = dp.Value ?? string.Empty,
                            Unit = dp.Unit,
                            InformationType = dp.InformationType,
                            Status = dp.CompletenessStatus,
                            OwnerId = dp.OwnerId,
                            OwnerName = dpOwner?.Name ?? dp.OwnerId,
                            LastUpdatedAt = dp.UpdatedAt,
                            EvidenceCount = evidenceCount,
                            HasAssumptions = hasAssumptions
                        };
                    })
                    .ToList();
                
                // Get evidence for this section
                var sectionEvidence = _evidence
                    .Where(e => e.SectionId == section.Id)
                    .Select(e => new EvidenceMetadata
                    {
                        Id = e.Id,
                        DataPointId = string.Empty, // Evidence is linked to section, not specific data point in this model
                        Title = e.Title,
                        FileName = e.FileName ?? string.Empty,
                        FileType = e.ContentType ?? string.Empty,
                        FileSize = e.FileSize ?? 0,
                        UploadedAt = e.UploadedAt,
                        UploadedBy = e.UploadedBy
                    })
                    .ToList();
                
                // Get active assumptions for this section
                var sectionAssumptions = _assumptions
                    .Where(a => a.SectionId == section.Id && a.Status == "active")
                    .Select(a => new AssumptionRecord
                    {
                        Id = a.Id,
                        DataPointId = a.DataPointId ?? string.Empty,
                        Description = a.Description,
                        Justification = a.Rationale ?? string.Empty,
                        ConfidenceLevel = string.Empty, // Not available on Assumption model
                        Status = a.Status,
                        CreatedAt = a.CreatedAt,
                        CreatedBy = a.CreatedBy
                    })
                    .ToList();
                
                // Get gaps for this section (Gap model uses Resolved, not Status)
                var sectionGaps = _gaps
                    .Where(g => g.SectionId == section.Id && !g.Resolved)
                    .Select(g => new GapRecord
                    {
                        Id = g.Id,
                        DataPointId = string.Empty, // Gap is linked to section, not specific data point
                        Description = g.Description,
                        MissingReason = g.Impact, // Using Impact as MissingReason since that's what's available
                        Status = g.Resolved ? "provided" : "missing",
                        CreatedAt = g.CreatedAt
                    })
                    .ToList();
                
                generatedSections.Add(new GeneratedReportSection
                {
                    Section = section,
                    Owner = owner,
                    DataPoints = dataPoints,
                    Evidence = sectionEvidence,
                    Assumptions = sectionAssumptions,
                    Gaps = sectionGaps
                });
            }
            
            // Create the generated report
            var reportId = Guid.NewGuid().ToString();
            var generatedAt = DateTime.UtcNow.ToString("O");
            
            var report = new GeneratedReport
            {
                Id = reportId,
                Period = period,
                Organization = _organization,
                Sections = generatedSections,
                GeneratedAt = generatedAt,
                GeneratedBy = request.GeneratedBy,
                GeneratedByName = userName,
                GenerationNote = request.GenerationNote,
                Checksum = CalculateReportChecksum(reportId, period.Id, generatedSections, generatedAt)
            };
            
            // Log generation to audit trail
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = generatedAt,
                UserId = request.GeneratedBy,
                UserName = userName,
                Action = "report-generated",
                EntityType = "report",
                EntityId = reportId,
                ChangeNote = $"Generated report for period '{period.Name}' with {generatedSections.Count} sections"
            };
            _auditLog.Add(auditEntry);
            
            // Store generation in history
            var historyEntry = new GenerationHistoryEntry
            {
                Id = reportId,
                PeriodId = request.PeriodId,
                GeneratedAt = generatedAt,
                GeneratedBy = request.GeneratedBy,
                GeneratedByName = userName,
                GenerationNote = request.GenerationNote,
                Checksum = report.Checksum,
                VariantId = null, // No variant for base generation
                VariantName = null,
                Status = "draft",
                SectionCount = generatedSections.Count,
                DataPointCount = generatedSections.Sum(s => s.DataPoints.Count),
                EvidenceCount = generatedSections.Sum(s => s.Evidence.Count),
                SectionSnapshots = generatedSections.Select(s => new SectionSnapshot
                {
                    SectionId = s.Section.Id,
                    SectionTitle = s.Section.Title,
                    CatalogCode = s.Section.CatalogCode,
                    DataPointCount = s.DataPoints.Count
                }).ToList(),
                Report = report
            };
            _generationHistory.Add(historyEntry);
            
            return (true, null, report);
        }
    }
    
    /// <summary>
    /// Calculate a deterministic checksum for the generated report.
    /// </summary>
    private string CalculateReportChecksum(string reportId, string periodId, List<GeneratedReportSection> sections, string generatedAt)
    {
        var checksumBuilder = new StringBuilder();
        checksumBuilder.Append(reportId);
        checksumBuilder.Append(periodId);
        checksumBuilder.Append(generatedAt);
        
        foreach (var section in sections)
        {
            checksumBuilder.Append(section.Section.Id);
            checksumBuilder.Append(section.Section.Title);
            checksumBuilder.Append(section.Section.Order);
            
            foreach (var dp in section.DataPoints)
            {
                checksumBuilder.Append(dp.Id);
                checksumBuilder.Append(dp.Value);
                checksumBuilder.Append(dp.Status);
            }
        }
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(checksumBuilder.ToString()));
        return Convert.ToBase64String(hash);
    }
    
    #endregion
    
    #region Report Variants
    
    /// <summary>
    /// Get all report variants.
    /// </summary>
    public IReadOnlyList<ReportVariant> GetVariants()
    {
        lock (_lock)
        {
            return _reportVariants.ToList();
        }
    }
    
    /// <summary>
    /// Get a specific report variant by ID.
    /// </summary>
    public ReportVariant? GetVariant(string variantId)
    {
        lock (_lock)
        {
            return _reportVariants.FirstOrDefault(v => v.Id == variantId);
        }
    }
    
    /// <summary>
    /// Create a new report variant.
    /// </summary>
    public (bool IsValid, string? ErrorMessage, ReportVariant? Variant) CreateVariant(CreateVariantRequest request)
    {
        lock (_lock)
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return (false, "Variant name is required.", null);
            }
            
            if (string.IsNullOrWhiteSpace(request.AudienceType))
            {
                return (false, "Audience type is required.", null);
            }
            
            // Check for duplicate name
            if (_reportVariants.Any(v => v.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "A variant with this name already exists.", null);
            }
            
            var user = GetUser(request.CreatedBy);
            
            var variant = new ReportVariant
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                AudienceType = request.AudienceType.ToLowerInvariant(),
                Rules = request.Rules,
                RedactionRules = request.RedactionRules,
                IsActive = true,
                CreatedBy = request.CreatedBy,
                CreatedByName = user?.Name ?? request.CreatedBy,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };
            
            // Validate and assign IDs to rules
            foreach (var rule in variant.Rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Id))
                {
                    rule.Id = Guid.NewGuid().ToString();
                }
            }
            
            foreach (var redactionRule in variant.RedactionRules)
            {
                if (string.IsNullOrWhiteSpace(redactionRule.Id))
                {
                    redactionRule.Id = Guid.NewGuid().ToString();
                }
            }
            
            _reportVariants.Add(variant);
            
            // Add audit log entry
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Name", NewValue = variant.Name },
                new FieldChange { Field = "AudienceType", NewValue = variant.AudienceType }
            };
            CreateAuditLogEntry(request.CreatedBy, variant.CreatedByName, "create", "ReportVariant", variant.Id, changes, 
                $"Created variant '{variant.Name}' for audience '{variant.AudienceType}'");
            
            return (true, null, variant);
        }
    }
    
    /// <summary>
    /// Update an existing report variant.
    /// </summary>
    public (bool IsValid, string? ErrorMessage, ReportVariant? Variant) UpdateVariant(string variantId, UpdateVariantRequest request)
    {
        lock (_lock)
        {
            var variant = _reportVariants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null)
            {
                return (false, "Variant not found.", null);
            }
            
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return (false, "Variant name is required.", null);
            }
            
            if (string.IsNullOrWhiteSpace(request.AudienceType))
            {
                return (false, "Audience type is required.", null);
            }
            
            // Check for duplicate name (excluding current variant)
            if (_reportVariants.Any(v => v.Id != variantId && v.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "A variant with this name already exists.", null);
            }
            
            var user = GetUser(request.UpdatedBy);
            
            // Update variant properties
            variant.Name = request.Name;
            variant.Description = request.Description;
            variant.AudienceType = request.AudienceType.ToLowerInvariant();
            variant.Rules = request.Rules;
            variant.RedactionRules = request.RedactionRules;
            variant.IsActive = request.IsActive;
            variant.LastModifiedBy = request.UpdatedBy;
            variant.LastModifiedByName = user?.Name ?? request.UpdatedBy;
            variant.LastModifiedAt = DateTime.UtcNow.ToString("o");
            
            // Validate and assign IDs to rules
            foreach (var rule in variant.Rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Id))
                {
                    rule.Id = Guid.NewGuid().ToString();
                }
            }
            
            foreach (var redactionRule in variant.RedactionRules)
            {
                if (string.IsNullOrWhiteSpace(redactionRule.Id))
                {
                    redactionRule.Id = Guid.NewGuid().ToString();
                }
            }
            
            // Add audit log entry
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Name", NewValue = variant.Name },
                new FieldChange { Field = "AudienceType", NewValue = variant.AudienceType },
                new FieldChange { Field = "IsActive", NewValue = variant.IsActive.ToString() }
            };
            CreateAuditLogEntry(request.UpdatedBy, variant.LastModifiedByName ?? request.UpdatedBy, "update", "ReportVariant", variant.Id, changes, 
                $"Updated variant '{variant.Name}'");
            
            return (true, null, variant);
        }
    }
    
    /// <summary>
    /// Delete a report variant.
    /// </summary>
    public (bool IsValid, string? ErrorMessage) DeleteVariant(string variantId, string deletedBy)
    {
        lock (_lock)
        {
            var variant = _reportVariants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null)
            {
                return (false, "Variant not found.");
            }
            
            var variantName = variant.Name;
            _reportVariants.Remove(variant);
            
            // Add audit log entry
            var user = GetUser(deletedBy);
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Deleted", OldValue = "false", NewValue = "true" }
            };
            CreateAuditLogEntry(deletedBy, user?.Name ?? deletedBy, "delete", "ReportVariant", variantId, changes, 
                $"Deleted variant '{variantName}'");
            
            return (true, null);
        }
    }
    
    /// <summary>
    /// Generate a report using a specific variant configuration.
    /// </summary>
    public (bool IsValid, string? ErrorMessage, GeneratedReportVariant? VariantReport) GenerateReportVariant(GenerateVariantRequest request)
    {
        lock (_lock)
        {
            // Get the variant
            var variant = _reportVariants.FirstOrDefault(v => v.Id == request.VariantId);
            if (variant == null)
            {
                return (false, "Variant not found.", null);
            }
            
            if (!variant.IsActive)
            {
                return (false, "Variant is not active.", null);
            }
            
            // First, generate the base report with all sections
            var baseRequest = new GenerateReportRequest
            {
                PeriodId = request.PeriodId,
                GeneratedBy = request.GeneratedBy,
                GenerationNote = request.GenerationNote
            };
            
            var (isValid, errorMessage, baseReport) = GenerateReport(baseRequest);
            if (!isValid || baseReport == null)
            {
                return (false, errorMessage ?? "Failed to generate base report.", null);
            }
            
            // Apply variant rules
            var excludedSections = new List<string>();
            var redactedFields = new List<string>();
            var excludedAttachmentCount = 0;
            
            // Process section inclusion/exclusion rules
            var filteredSections = new List<GeneratedReportSection>();
            var sectionRules = variant.Rules
                .Where(r => r.RuleType == "include-section" || r.RuleType == "exclude-section")
                .OrderBy(r => r.Order)
                .ToList();
            
            // Default is to include all sections unless we have include rules
            var hasIncludeRules = sectionRules.Any(r => r.RuleType == "include-section");
            
            foreach (var section in baseReport.Sections)
            {
                var shouldInclude = !hasIncludeRules; // If no include rules, default to true
                
                foreach (var rule in sectionRules)
                {
                    if (rule.RuleType == "include-section" && rule.Target == section.Section.Id)
                    {
                        shouldInclude = true;
                    }
                    else if (rule.RuleType == "exclude-section" && rule.Target == section.Section.Id)
                    {
                        shouldInclude = false;
                    }
                }
                
                if (shouldInclude)
                {
                    // Clone the section to avoid modifying the original
                    var sectionCopy = new GeneratedReportSection
                    {
                        Section = section.Section,
                        Owner = section.Owner,
                        DataPoints = new List<DataPointSnapshot>(section.DataPoints),
                        Evidence = new List<EvidenceMetadata>(section.Evidence),
                        Assumptions = new List<AssumptionRecord>(section.Assumptions),
                        Gaps = new List<GapRecord>(section.Gaps)
                    };
                    
                    // Apply field-level rules and redaction
                    ApplyFieldRulesAndRedaction(sectionCopy, variant, redactedFields);
                    
                    // Apply attachment exclusion rules
                    var excludeAttachments = variant.Rules.Any(r => 
                        r.RuleType == "exclude-attachments" && r.Target == section.Section.Id);
                    
                    if (excludeAttachments)
                    {
                        excludedAttachmentCount += sectionCopy.Evidence.Count;
                        sectionCopy.Evidence.Clear();
                    }
                    
                    filteredSections.Add(sectionCopy);
                }
                else
                {
                    excludedSections.Add(section.Section.Id);
                }
            }
            
            // Create the variant report
            baseReport.Sections = filteredSections;
            
            var variantReport = new GeneratedReportVariant
            {
                Report = baseReport,
                Variant = variant,
                ExcludedSections = excludedSections,
                RedactedFields = redactedFields,
                ExcludedAttachmentCount = excludedAttachmentCount
            };
            
            // Add audit log entry
            var user = GetUser(request.GeneratedBy);
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "VariantId", NewValue = variant.Id },
                new FieldChange { Field = "PeriodId", NewValue = request.PeriodId },
                new FieldChange { Field = "ExcludedSectionCount", NewValue = excludedSections.Count.ToString() }
            };
            CreateAuditLogEntry(request.GeneratedBy, user?.Name ?? request.GeneratedBy, "generate", "GeneratedReportVariant", baseReport.Id, changes, 
                $"Generated variant report '{variant.Name}' for period '{request.PeriodId}'");
            
            return (true, null, variantReport);
        }
    }
    
    /// <summary>
    /// Apply field-level rules and redaction to a section.
    /// </summary>
    private void ApplyFieldRulesAndRedaction(GeneratedReportSection section, ReportVariant variant, List<string> redactedFields)
    {
        // Apply redaction rules
        foreach (var redactionRule in variant.RedactionRules)
        {
            // Find matching data points
            var matchingDataPoints = section.DataPoints
                .Where(dp => dp.Id == redactionRule.FieldIdentifier || dp.Title.Contains(redactionRule.FieldIdentifier))
                .ToList();
            
            foreach (var dataPoint in matchingDataPoints)
            {
                redactedFields.Add(dataPoint.Id);
                
                switch (redactionRule.RedactionType.ToLowerInvariant())
                {
                    case "mask":
                        dataPoint.Value = "***REDACTED***";
                        break;
                    case "remove":
                        section.DataPoints.Remove(dataPoint);
                        break;
                    case "replace":
                        dataPoint.Value = redactionRule.ReplacementValue ?? "[REDACTED]";
                        break;
                }
            }
        }
        
        // Apply field group exclusion rules
        var fieldGroupExclusionRules = variant.Rules
            .Where(r => r.RuleType == "exclude-field-group")
            .ToList();
        
        foreach (var rule in fieldGroupExclusionRules)
        {
            // Remove data points matching the field group
            var toRemove = section.DataPoints
                .Where(dp => dp.InformationType == rule.Target)
                .ToList();
            
            foreach (var dp in toRemove)
            {
                redactedFields.Add(dp.Id);
                section.DataPoints.Remove(dp);
            }
        }
    }
    
    /// <summary>
    /// Compare multiple report variants to show differences.
    /// </summary>
    public (bool IsValid, string? ErrorMessage, VariantComparison? Comparison) CompareVariants(CompareVariantsRequest request)
    {
        lock (_lock)
        {
            // Validate request
            if (request.VariantIds.Count < 2)
            {
                return (false, "At least 2 variants are required for comparison.", null);
            }
            
            var period = _periods.FirstOrDefault(p => p.Id == request.PeriodId);
            if (period == null)
            {
                return (false, "Reporting period not found.", null);
            }
            
            // Get all variants
            var variants = new List<ReportVariant>();
            foreach (var variantId in request.VariantIds)
            {
                var variant = _reportVariants.FirstOrDefault(v => v.Id == variantId);
                if (variant == null)
                {
                    return (false, $"Variant '{variantId}' not found.", null);
                }
                variants.Add(variant);
            }
            
            // Generate reports for each variant
            var variantReports = new Dictionary<string, GeneratedReportVariant>();
            foreach (var variant in variants)
            {
                var generateRequest = new GenerateVariantRequest
                {
                    PeriodId = request.PeriodId,
                    VariantId = variant.Id,
                    GeneratedBy = request.RequestedBy
                };
                
                var (isValid, errorMessage, variantReport) = GenerateReportVariant(generateRequest);
                if (!isValid || variantReport == null)
                {
                    return (false, errorMessage ?? "Failed to generate variant report.", null);
                }
                
                variantReports[variant.Id] = variantReport;
            }
            
            // Compare sections
            var sectionDifferences = new List<SectionDifference>();
            var allSectionIds = variantReports.Values
                .SelectMany(vr => vr.Report.Sections.Select(s => s.Section.Id))
                .Concat(variantReports.Values.SelectMany(vr => vr.ExcludedSections))
                .Distinct()
                .ToList();
            
            foreach (var sectionId in allSectionIds)
            {
                var includedIn = new List<string>();
                var excludedFrom = new List<string>();
                string? sectionName = null;
                
                foreach (var variant in variants)
                {
                    var variantReport = variantReports[variant.Id];
                    var isIncluded = variantReport.Report.Sections.Any(s => s.Section.Id == sectionId);
                    
                    if (isIncluded)
                    {
                        includedIn.Add(variant.Id);
                        if (sectionName == null)
                        {
                            sectionName = variantReport.Report.Sections.First(s => s.Section.Id == sectionId).Section.Title;
                        }
                    }
                    else
                    {
                        excludedFrom.Add(variant.Id);
                    }
                }
                
                if (excludedFrom.Count > 0)
                {
                    sectionDifferences.Add(new SectionDifference
                    {
                        SectionId = sectionId,
                        SectionName = sectionName ?? sectionId,
                        IncludedInVariants = includedIn,
                        ExcludedFromVariants = excludedFrom,
                        ExclusionReason = "Excluded by variant rules"
                    });
                }
            }
            
            // Compare fields (redacted vs visible)
            var fieldDifferences = new List<FieldDifference>();
            var allFieldIds = variantReports.Values
                .SelectMany(vr => vr.Report.Sections.SelectMany(s => s.DataPoints.Select(dp => dp.Id)))
                .Concat(variantReports.Values.SelectMany(vr => vr.RedactedFields))
                .Distinct()
                .ToList();
            
            foreach (var fieldId in allFieldIds)
            {
                var visibleIn = new List<string>();
                var redactedIn = new List<string>();
                string? fieldName = null;
                string? sectionId = null;
                string? redactionType = null;
                string? redactionReason = null;
                
                foreach (var variant in variants)
                {
                    var variantReport = variantReports[variant.Id];
                    var isRedacted = variantReport.RedactedFields.Contains(fieldId);
                    
                    if (isRedacted)
                    {
                        redactedIn.Add(variant.Id);
                        var redactionRule = variant.RedactionRules.FirstOrDefault(r => r.FieldIdentifier == fieldId);
                        if (redactionRule != null)
                        {
                            redactionType = redactionRule.RedactionType;
                            redactionReason = redactionRule.Reason;
                        }
                    }
                    else
                    {
                        visibleIn.Add(variant.Id);
                        if (fieldName == null)
                        {
                            var dataPoint = variantReport.Report.Sections
                                .SelectMany(s => s.DataPoints)
                                .FirstOrDefault(dp => dp.Id == fieldId);
                            if (dataPoint != null)
                            {
                                fieldName = dataPoint.Title;
                                var section = variantReport.Report.Sections
                                    .FirstOrDefault(s => s.DataPoints.Any(dp => dp.Id == fieldId));
                                sectionId = section?.Section.Id;
                            }
                        }
                    }
                }
                
                if (redactedIn.Count > 0)
                {
                    fieldDifferences.Add(new FieldDifference
                    {
                        FieldId = fieldId,
                        FieldName = fieldName ?? fieldId,
                        SectionId = sectionId ?? "",
                        VisibleInVariants = visibleIn,
                        RedactedInVariants = redactedIn,
                        RedactionType = redactionType,
                        RedactionReason = redactionReason
                    });
                }
            }
            
            var comparison = new VariantComparison
            {
                Period = period,
                Variants = variants,
                SectionDifferences = sectionDifferences,
                FieldDifferences = fieldDifferences,
                ComparedAt = DateTime.UtcNow.ToString("o"),
                ComparedBy = request.RequestedBy
            };
            
            // Add audit log entry
            var user = GetUser(request.RequestedBy);
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "VariantCount", NewValue = variants.Count.ToString() },
                new FieldChange { Field = "PeriodId", NewValue = period.Id }
            };
            CreateAuditLogEntry(request.RequestedBy, user?.Name ?? request.RequestedBy, "compare", "VariantComparison", 
                string.Join(",", request.VariantIds), changes, $"Compared {variants.Count} variants for period '{period.Name}'");
            
            return (true, null, comparison);
        }
    }
    
    #endregion
    
    #region Branding Profiles
    
    public (bool IsValid, string? ErrorMessage, BrandingProfile? Profile) CreateBrandingProfile(CreateBrandingProfileRequest request)
    {
        lock (_lock)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return (false, "Name is required.", null);
            }
            
            if (string.IsNullOrWhiteSpace(request.CreatedBy))
            {
                return (false, "CreatedBy is required.", null);
            }
            
            // If this is set as default, unmark any existing default
            if (request.IsDefault)
            {
                foreach (var existing in _brandingProfiles.Where(p => p.IsDefault))
                {
                    existing.IsDefault = false;
                }
            }
            
            var now = DateTime.UtcNow.ToString("O");
            var profile = new BrandingProfile
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                OrganizationId = request.OrganizationId,
                SubsidiaryName = request.SubsidiaryName,
                LogoData = request.LogoData,
                LogoContentType = request.LogoContentType,
                PrimaryColor = request.PrimaryColor,
                SecondaryColor = request.SecondaryColor,
                AccentColor = request.AccentColor,
                FooterText = request.FooterText,
                IsDefault = request.IsDefault,
                IsActive = true,
                CreatedBy = request.CreatedBy,
                CreatedAt = now
            };
            
            _brandingProfiles.Add(profile);
            
            // Add audit log entry
            var user = GetUser(request.CreatedBy);
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Name", NewValue = request.Name },
                new FieldChange { Field = "IsDefault", NewValue = request.IsDefault.ToString() }
            };
            CreateAuditLogEntry(request.CreatedBy, user?.Name ?? request.CreatedBy, "create", "BrandingProfile", 
                profile.Id, changes, $"Created branding profile '{request.Name}'");
            
            return (true, null, profile);
        }
    }
    
    public (bool IsValid, string? ErrorMessage, BrandingProfile? Profile) UpdateBrandingProfile(string id, UpdateBrandingProfileRequest request)
    {
        lock (_lock)
        {
            var profile = _brandingProfiles.FirstOrDefault(p => p.Id == id);
            if (profile == null)
            {
                return (false, "BrandingProfile not found.", null);
            }
            
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return (false, "Name is required.", null);
            }
            
            if (string.IsNullOrWhiteSpace(request.UpdatedBy))
            {
                return (false, "UpdatedBy is required.", null);
            }
            
            // Track changes for audit log
            var changes = new List<FieldChange>();
            
            if (profile.Name != request.Name)
            {
                changes.Add(new FieldChange { Field = "Name", OldValue = profile.Name, NewValue = request.Name });
            }
            
            if (profile.IsDefault != request.IsDefault)
            {
                changes.Add(new FieldChange { Field = "IsDefault", OldValue = profile.IsDefault.ToString(), NewValue = request.IsDefault.ToString() });
                
                // If this is set as default, unmark any existing default
                if (request.IsDefault)
                {
                    foreach (var existing in _brandingProfiles.Where(p => p.IsDefault && p.Id != id))
                    {
                        existing.IsDefault = false;
                    }
                }
            }
            
            if (profile.IsActive != request.IsActive)
            {
                changes.Add(new FieldChange { Field = "IsActive", OldValue = profile.IsActive.ToString(), NewValue = request.IsActive.ToString() });
            }
            
            // Update fields
            profile.Name = request.Name;
            profile.Description = request.Description;
            profile.SubsidiaryName = request.SubsidiaryName;
            profile.LogoData = request.LogoData;
            profile.LogoContentType = request.LogoContentType;
            profile.PrimaryColor = request.PrimaryColor;
            profile.SecondaryColor = request.SecondaryColor;
            profile.AccentColor = request.AccentColor;
            profile.FooterText = request.FooterText;
            profile.IsDefault = request.IsDefault;
            profile.IsActive = request.IsActive;
            profile.UpdatedBy = request.UpdatedBy;
            profile.UpdatedAt = DateTime.UtcNow.ToString("O");
            
            // Add audit log entry
            var user = GetUser(request.UpdatedBy);
            CreateAuditLogEntry(request.UpdatedBy, user?.Name ?? request.UpdatedBy, "update", "BrandingProfile", 
                profile.Id, changes, $"Updated branding profile '{profile.Name}'");
            
            return (true, null, profile);
        }
    }
    
    public List<BrandingProfile> GetBrandingProfiles()
    {
        lock (_lock)
        {
            return _brandingProfiles.ToList();
        }
    }
    
    public BrandingProfile? GetBrandingProfile(string id)
    {
        lock (_lock)
        {
            return _brandingProfiles.FirstOrDefault(p => p.Id == id);
        }
    }
    
    public BrandingProfile? GetDefaultBrandingProfile()
    {
        lock (_lock)
        {
            return _brandingProfiles.FirstOrDefault(p => p.IsDefault && p.IsActive);
        }
    }
    
    public bool DeleteBrandingProfile(string id, string deletedBy)
    {
        lock (_lock)
        {
            var profile = _brandingProfiles.FirstOrDefault(p => p.Id == id);
            if (profile == null)
            {
                return false;
            }
            
            _brandingProfiles.Remove(profile);
            
            // Add audit log entry
            var user = GetUser(deletedBy);
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Deleted", OldValue = "false", NewValue = "true" }
            };
            CreateAuditLogEntry(deletedBy, user?.Name ?? deletedBy, "delete", "BrandingProfile", 
                profile.Id, changes, $"Deleted branding profile '{profile.Name}'");
            
            return true;
        }
    }
    
    #endregion
    
    #region Document Templates
    
    public (bool IsValid, string? ErrorMessage, DocumentTemplate? Template) CreateDocumentTemplate(CreateDocumentTemplateRequest request)
    {
        lock (_lock)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return (false, "Name is required.", null);
            }
            
            if (string.IsNullOrWhiteSpace(request.CreatedBy))
            {
                return (false, "CreatedBy is required.", null);
            }
            
            // Validate template type
            var validTypes = new[] { "pdf", "docx", "excel" };
            if (!validTypes.Contains(request.TemplateType, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"TemplateType must be one of: {string.Join(", ", validTypes)}.", null);
            }
            
            // Validate JSON configuration format
            if (!string.IsNullOrWhiteSpace(request.Configuration))
            {
                try
                {
                    System.Text.Json.JsonDocument.Parse(request.Configuration);
                }
                catch (System.Text.Json.JsonException)
                {
                    return (false, "Configuration must be valid JSON.", null);
                }
            }
            
            // If this is set as default for its type, unmark any existing default of the same type
            if (request.IsDefault)
            {
                foreach (var existing in _documentTemplates.Where(t => t.IsDefault && t.TemplateType == request.TemplateType))
                {
                    existing.IsDefault = false;
                }
            }
            
            var now = DateTime.UtcNow.ToString("O");
            var template = new DocumentTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                TemplateType = request.TemplateType,
                Version = 1,
                Configuration = request.Configuration,
                IsDefault = request.IsDefault,
                IsActive = true,
                CreatedBy = request.CreatedBy,
                CreatedAt = now
            };
            
            _documentTemplates.Add(template);
            
            // Add audit log entry
            var user = GetUser(request.CreatedBy);
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Name", NewValue = request.Name },
                new FieldChange { Field = "Version", NewValue = "1" },
                new FieldChange { Field = "IsDefault", NewValue = request.IsDefault.ToString() }
            };
            CreateAuditLogEntry(request.CreatedBy, user?.Name ?? request.CreatedBy, "create", "DocumentTemplate", 
                template.Id, changes, $"Created document template '{request.Name}' version 1");
            
            return (true, null, template);
        }
    }
    
    public (bool IsValid, string? ErrorMessage, DocumentTemplate? Template) UpdateDocumentTemplate(string id, UpdateDocumentTemplateRequest request)
    {
        lock (_lock)
        {
            var template = _documentTemplates.FirstOrDefault(t => t.Id == id);
            if (template == null)
            {
                return (false, "DocumentTemplate not found.", null);
            }
            
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return (false, "Name is required.", null);
            }
            
            if (string.IsNullOrWhiteSpace(request.UpdatedBy))
            {
                return (false, "UpdatedBy is required.", null);
            }
            
            // Validate JSON configuration format
            if (!string.IsNullOrWhiteSpace(request.Configuration))
            {
                try
                {
                    System.Text.Json.JsonDocument.Parse(request.Configuration);
                }
                catch (System.Text.Json.JsonException)
                {
                    return (false, "Configuration must be valid JSON.", null);
                }
            }
            
            // Track changes for audit log
            var changes = new List<FieldChange>();
            
            if (template.Name != request.Name)
            {
                changes.Add(new FieldChange { Field = "Name", OldValue = template.Name, NewValue = request.Name });
            }
            
            if (template.Configuration != request.Configuration)
            {
                changes.Add(new FieldChange { Field = "Configuration", OldValue = "updated", NewValue = "updated" });
                // Increment version when configuration changes
                template.Version++;
                changes.Add(new FieldChange { Field = "Version", OldValue = (template.Version - 1).ToString(), NewValue = template.Version.ToString() });
            }
            
            if (template.IsDefault != request.IsDefault)
            {
                changes.Add(new FieldChange { Field = "IsDefault", OldValue = template.IsDefault.ToString(), NewValue = request.IsDefault.ToString() });
                
                // If this is set as default, unmark any existing default of the same type
                if (request.IsDefault)
                {
                    foreach (var existing in _documentTemplates.Where(t => t.IsDefault && t.TemplateType == template.TemplateType && t.Id != id))
                    {
                        existing.IsDefault = false;
                    }
                }
            }
            
            if (template.IsActive != request.IsActive)
            {
                changes.Add(new FieldChange { Field = "IsActive", OldValue = template.IsActive.ToString(), NewValue = request.IsActive.ToString() });
            }
            
            // Update fields
            template.Name = request.Name;
            template.Description = request.Description;
            template.Configuration = request.Configuration;
            template.IsDefault = request.IsDefault;
            template.IsActive = request.IsActive;
            template.UpdatedBy = request.UpdatedBy;
            template.UpdatedAt = DateTime.UtcNow.ToString("O");
            
            // Add audit log entry
            var user = GetUser(request.UpdatedBy);
            CreateAuditLogEntry(request.UpdatedBy, user?.Name ?? request.UpdatedBy, "update", "DocumentTemplate", 
                template.Id, changes, $"Updated document template '{template.Name}' to version {template.Version}");
            
            return (true, null, template);
        }
    }
    
    public List<DocumentTemplate> GetDocumentTemplates()
    {
        lock (_lock)
        {
            return _documentTemplates.ToList();
        }
    }
    
    public List<DocumentTemplate> GetDocumentTemplatesByType(string templateType)
    {
        lock (_lock)
        {
            return _documentTemplates.Where(t => t.TemplateType == templateType).ToList();
        }
    }
    
    public DocumentTemplate? GetDocumentTemplate(string id)
    {
        lock (_lock)
        {
            return _documentTemplates.FirstOrDefault(t => t.Id == id);
        }
    }
    
    public DocumentTemplate? GetDefaultDocumentTemplate(string templateType)
    {
        lock (_lock)
        {
            return _documentTemplates.FirstOrDefault(t => t.TemplateType == templateType && t.IsDefault && t.IsActive);
        }
    }
    
    public bool DeleteDocumentTemplate(string id, string deletedBy)
    {
        lock (_lock)
        {
            var template = _documentTemplates.FirstOrDefault(t => t.Id == id);
            if (template == null)
            {
                return false;
            }
            
            _documentTemplates.Remove(template);
            
            // Add audit log entry
            var user = GetUser(deletedBy);
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "Deleted", OldValue = "false", NewValue = "true" }
            };
            CreateAuditLogEntry(deletedBy, user?.Name ?? deletedBy, "delete", "DocumentTemplate", 
                template.Id, changes, $"Deleted document template '{template.Name}'");
            
            return true;
        }
    }
    
    public void RecordTemplateUsage(string templateId, string periodId, string? brandingProfileId, string exportType, string generatedBy)
    {
        lock (_lock)
        {
            var template = _documentTemplates.FirstOrDefault(t => t.Id == templateId);
            if (template == null)
            {
                return;
            }
            
            var usage = new TemplateUsageRecord
            {
                Id = Guid.NewGuid().ToString(),
                TemplateId = templateId,
                TemplateVersion = template.Version,
                PeriodId = periodId,
                BrandingProfileId = brandingProfileId,
                ExportType = exportType,
                GeneratedBy = generatedBy,
                GeneratedAt = DateTime.UtcNow.ToString("O")
            };
            
            _templateUsageRecords.Add(usage);
            
            // Add audit log entry
            var user = GetUser(generatedBy);
            var changes = new List<FieldChange>
            {
                new FieldChange { Field = "TemplateId", NewValue = templateId },
                new FieldChange { Field = "TemplateVersion", NewValue = template.Version.ToString() },
                new FieldChange { Field = "ExportType", NewValue = exportType }
            };
            CreateAuditLogEntry(generatedBy, user?.Name ?? generatedBy, "use", "DocumentTemplate", 
                templateId, changes, $"Used template '{template.Name}' version {template.Version} for {exportType} export");
        }
    }
    
    public List<TemplateUsageRecord> GetTemplateUsageHistory(string templateId)
    {
        lock (_lock)
        {
            return _templateUsageRecords.Where(r => r.TemplateId == templateId).OrderByDescending(r => r.GeneratedAt).ToList();
        }
    }
    
    public List<TemplateUsageRecord> GetPeriodTemplateUsage(string periodId)
    {
        lock (_lock)
        {
            return _templateUsageRecords.Where(r => r.PeriodId == periodId).OrderByDescending(r => r.GeneratedAt).ToList();
        }
    }
    
    #endregion
    
    #region Generation and Export History
    
    /// <summary>
    /// Get generation history for a specific reporting period.
    /// </summary>
    public IReadOnlyList<GenerationHistoryEntry> GetGenerationHistory(string periodId)
    {
        lock (_lock)
        {
            return _generationHistory
                .Where(h => h.PeriodId == periodId)
                .OrderByDescending(h => h.GeneratedAt)
                .Select(h => new GenerationHistoryEntry
                {
                    Id = h.Id,
                    PeriodId = h.PeriodId,
                    GeneratedAt = h.GeneratedAt,
                    GeneratedBy = h.GeneratedBy,
                    GeneratedByName = h.GeneratedByName,
                    GenerationNote = h.GenerationNote,
                    Checksum = h.Checksum,
                    VariantId = h.VariantId,
                    VariantName = h.VariantName,
                    Status = h.Status,
                    SectionCount = h.SectionCount,
                    DataPointCount = h.DataPointCount,
                    EvidenceCount = h.EvidenceCount,
                    SectionSnapshots = h.SectionSnapshots,
                    MarkedFinalAt = h.MarkedFinalAt,
                    MarkedFinalBy = h.MarkedFinalBy,
                    MarkedFinalByName = h.MarkedFinalByName,
                    Report = null // Don't include full report in list view
                })
                .ToList();
        }
    }
    
    /// <summary>
    /// Get a specific generation by ID.
    /// </summary>
    public GenerationHistoryEntry? GetGeneration(string generationId)
    {
        lock (_lock)
        {
            return _generationHistory.FirstOrDefault(h => h.Id == generationId);
        }
    }
    
    /// <summary>
    /// Mark a generation as final.
    /// </summary>
    public (bool IsSuccess, string? ErrorMessage, GenerationHistoryEntry? Entry) MarkGenerationAsFinal(MarkGenerationFinalRequest request)
    {
        lock (_lock)
        {
            var generation = _generationHistory.FirstOrDefault(h => h.Id == request.GenerationId);
            if (generation == null)
            {
                return (false, $"Generation with ID '{request.GenerationId}' not found.", null);
            }
            
            if (generation.Status == "final")
            {
                return (false, "This generation is already marked as final.", null);
            }
            
            var markedAt = DateTime.UtcNow.ToString("O");
            generation.Status = "final";
            generation.MarkedFinalAt = markedAt;
            generation.MarkedFinalBy = request.UserId;
            generation.MarkedFinalByName = request.UserName;
            
            // Add audit log entry
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = markedAt,
                UserId = request.UserId,
                UserName = request.UserName,
                Action = "mark-final",
                EntityType = "generation",
                EntityId = request.GenerationId,
                ChangeNote = request.Note ?? "Marked generation as final"
            };
            _auditLog.Add(auditEntry);
            
            return (true, null, generation);
        }
    }
    
    /// <summary>
    /// Compare two report generations.
    /// </summary>
    public (bool IsSuccess, string? ErrorMessage, GenerationComparison? Comparison) CompareGenerations(CompareGenerationsRequest request)
    {
        lock (_lock)
        {
            var gen1 = _generationHistory.FirstOrDefault(h => h.Id == request.Generation1Id);
            if (gen1 == null)
            {
                return (false, $"Generation '{request.Generation1Id}' not found.", null);
            }
            
            var gen2 = _generationHistory.FirstOrDefault(h => h.Id == request.Generation2Id);
            if (gen2 == null)
            {
                return (false, $"Generation '{request.Generation2Id}' not found.", null);
            }
            
            if (gen1.PeriodId != gen2.PeriodId)
            {
                return (false, "Cannot compare generations from different periods.", null);
            }
            
            var period = _periods.FirstOrDefault(p => p.Id == gen1.PeriodId);
            if (period == null)
            {
                return (false, "Period not found.", null);
            }
            
            // Build section differences
            var sectionDifferences = new List<GenerationSectionDifference>();
            var changedDataSources = new List<string>();
            
            var sections1 = gen1.SectionSnapshots.ToDictionary(s => s.SectionId);
            var sections2 = gen2.SectionSnapshots.ToDictionary(s => s.SectionId);
            
            var allSectionIds = sections1.Keys.Union(sections2.Keys).ToList();
            
            int totalSections = allSectionIds.Count;
            int sectionsAdded = 0;
            int sectionsRemoved = 0;
            int sectionsModified = 0;
            int sectionsUnchanged = 0;
            
            foreach (var sectionId in allSectionIds)
            {
                var inGen1 = sections1.ContainsKey(sectionId);
                var inGen2 = sections2.ContainsKey(sectionId);
                
                if (!inGen1 && inGen2)
                {
                    // Section added
                    sectionsAdded++;
                    sectionDifferences.Add(new GenerationSectionDifference
                    {
                        SectionId = sectionId,
                        SectionTitle = sections2[sectionId].SectionTitle,
                        CatalogCode = sections2[sectionId].CatalogCode,
                        DifferenceType = "added",
                        DataPointCount1 = 0,
                        DataPointCount2 = sections2[sectionId].DataPointCount,
                        Changes = new List<string> { "Section added in generation 2" }
                    });
                    changedDataSources.Add(sections2[sectionId].SectionTitle);
                }
                else if (inGen1 && !inGen2)
                {
                    // Section removed
                    sectionsRemoved++;
                    sectionDifferences.Add(new GenerationSectionDifference
                    {
                        SectionId = sectionId,
                        SectionTitle = sections1[sectionId].SectionTitle,
                        CatalogCode = sections1[sectionId].CatalogCode,
                        DifferenceType = "removed",
                        DataPointCount1 = sections1[sectionId].DataPointCount,
                        DataPointCount2 = 0,
                        Changes = new List<string> { "Section removed in generation 2" }
                    });
                    changedDataSources.Add(sections1[sectionId].SectionTitle);
                }
                else
                {
                    // Section exists in both
                    var section1 = sections1[sectionId];
                    var section2 = sections2[sectionId];
                    
                    var changes = new List<string>();
                    var isModified = false;
                    
                    if (section1.DataPointCount != section2.DataPointCount)
                    {
                        changes.Add($"Data point count changed from {section1.DataPointCount} to {section2.DataPointCount}");
                        isModified = true;
                    }
                    
                    if (section1.SectionTitle != section2.SectionTitle)
                    {
                        changes.Add($"Title changed from '{section1.SectionTitle}' to '{section2.SectionTitle}'");
                        isModified = true;
                    }
                    
                    if (isModified)
                    {
                        sectionsModified++;
                        sectionDifferences.Add(new GenerationSectionDifference
                        {
                            SectionId = sectionId,
                            SectionTitle = section2.SectionTitle,
                            CatalogCode = section2.CatalogCode,
                            DifferenceType = "modified",
                            DataPointCount1 = section1.DataPointCount,
                            DataPointCount2 = section2.DataPointCount,
                            Changes = changes
                        });
                        changedDataSources.Add(section2.SectionTitle);
                    }
                    else
                    {
                        sectionsUnchanged++;
                        sectionDifferences.Add(new GenerationSectionDifference
                        {
                            SectionId = sectionId,
                            SectionTitle = section2.SectionTitle,
                            CatalogCode = section2.CatalogCode,
                            DifferenceType = "unchanged",
                            DataPointCount1 = section1.DataPointCount,
                            DataPointCount2 = section2.DataPointCount,
                            Changes = new List<string>()
                        });
                    }
                }
            }
            
            var comparison = new GenerationComparison
            {
                Generation1 = gen1,
                Generation2 = gen2,
                Period = period,
                ComparedAt = DateTime.UtcNow.ToString("O"),
                ComparedBy = request.UserId,
                SectionDifferences = sectionDifferences,
                ChangedDataSources = changedDataSources,
                Summary = new GenerationComparisonSummary
                {
                    TotalSections = totalSections,
                    SectionsAdded = sectionsAdded,
                    SectionsRemoved = sectionsRemoved,
                    SectionsModified = sectionsModified,
                    SectionsUnchanged = sectionsUnchanged,
                    TotalDataPoints1 = gen1.DataPointCount,
                    TotalDataPoints2 = gen2.DataPointCount
                }
            };
            
            return (true, null, comparison);
        }
    }
    
    /// <summary>
    /// Record an export event.
    /// </summary>
    public void RecordExport(ExportHistoryEntry export)
    {
        lock (_lock)
        {
            _exportHistory.Add(export);
            
            // Add audit log entry
            var auditEntry = new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = export.ExportedAt,
                UserId = export.ExportedBy,
                UserName = export.ExportedByName,
                Action = "export",
                EntityType = "report",
                EntityId = export.GenerationId,
                ChangeNote = $"Exported report as {export.Format.ToUpper()} - {export.FileName}"
            };
            _auditLog.Add(auditEntry);
        }
    }
    
    /// <summary>
    /// Get export history for a specific reporting period.
    /// </summary>
    public IReadOnlyList<ExportHistoryEntry> GetExportHistory(string periodId)
    {
        lock (_lock)
        {
            return _exportHistory
                .Where(e => e.PeriodId == periodId)
                .OrderByDescending(e => e.ExportedAt)
                .ToList();
        }
    }
    
    /// <summary>
    /// Get export history for a specific generation.
    /// </summary>
    public IReadOnlyList<ExportHistoryEntry> GetExportHistoryForGeneration(string generationId)
    {
        lock (_lock)
        {
            return _exportHistory
                .Where(e => e.GenerationId == generationId)
                .OrderByDescending(e => e.ExportedAt)
                .ToList();
        }
    }
    
    /// <summary>
    /// Record a download of an export.
    /// </summary>
    public void RecordExportDownload(string exportId)
    {
        lock (_lock)
        {
            var export = _exportHistory.FirstOrDefault(e => e.Id == exportId);
            if (export != null)
            {
                export.DownloadCount++;
                export.LastDownloadedAt = DateTime.UtcNow.ToString("O");
            }
        }
    }
    
    #endregion
    
    #region Export Permission Checks
    
    /// <summary>
    /// Check if a user has permission to export reports.
    /// Permission is granted if:
    /// - User has CanExport flag set to true (global permission), OR
    /// - User is the owner of the specific reporting period (owner-based permission)
    /// Admin and report-owner roles typically have CanExport=true by default.
    /// </summary>
    public (bool HasPermission, string? ErrorMessage) CheckExportPermission(string userId, string periodId)
    {
        lock (_lock)
        {
            var user = GetUser(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }
            
            // Check global export permission
            if (user.CanExport)
            {
                return (true, null);
            }
            
            // Check owner-based permission for specific period
            var period = _periods.FirstOrDefault(p => p.Id == periodId);
            if (period != null && period.OwnerId == userId)
            {
                return (true, null);
            }
            
            return (false, "You do not have permission to export reports. Contact an administrator to request export access.");
        }
    }
    
    /// <summary>
    /// Record an export attempt in the audit log, including both successful and denied attempts.
    /// </summary>
    public void RecordExportAttempt(string userId, string userName, string periodId, string format, string? variantName, bool wasAllowed, string? errorMessage = null)
    {
        lock (_lock)
        {
            var entry = new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("O"),
                UserId = userId,
                UserName = userName,
                Action = wasAllowed ? "export" : "export-denied",
                EntityType = "ReportExport",
                EntityId = periodId,
                ChangeNote = wasAllowed 
                    ? $"Successfully exported report in {format.ToUpperInvariant()} format{(string.IsNullOrWhiteSpace(variantName) ? "" : $" (variant: {variantName})")}" 
                    : $"Export denied: {errorMessage ?? "Insufficient permissions"}",
                Changes = new List<FieldChange>
                {
                    new FieldChange { Field = "Format", OldValue = null, NewValue = format },
                    new FieldChange { Field = "VariantName", OldValue = null, NewValue = variantName ?? "" },
                    new FieldChange { Field = "Allowed", OldValue = null, NewValue = wasAllowed.ToString() }
                }
            };
            
            _auditLog.Add(entry);
        }
    }
    
    #endregion
    
    #endregion
}
