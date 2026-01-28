using System.Text;

namespace ARP.ESG_ReportStudio.API.Reporting;

public sealed class InMemoryReportStore
{
    private readonly object _lock = new();
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

    public InMemoryReportStore()
    {
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
            new User { Id = "user-1", Name = "Sarah Chen", Email = "sarah.chen@company.com", Role = "report-owner" },
            new User { Id = "user-2", Name = "Admin User", Email = "admin@company.com", Role = "admin" },
            new User { Id = "user-3", Name = "John Smith", Email = "john.smith@company.com", Role = "contributor" },
            new User { Id = "user-4", Name = "Emily Johnson", Email = "emily.johnson@company.com", Role = "contributor" },
            new User { Id = "user-5", Name = "Michael Brown", Email = "michael.brown@company.com", Role = "contributor" },
            new User { Id = "user-6", Name = "Lisa Anderson", Email = "lisa.anderson@company.com", Role = "auditor" },
            new User { Id = "owner-1", Name = "Test Owner", Email = "owner@company.com", Role = "report-owner" }
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

            return (true, null, period);
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
                var period = _periods.FirstOrDefault(p => p.Id == section.PeriodId);
                if (period == null || period.OwnerId != updatingUser.Id)
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
                ProvenanceNeedsReview = false
            };

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
            
            // Update review status if provided
            if (!string.IsNullOrWhiteSpace(request.ReviewStatus))
            {
                dataPoint.ReviewStatus = request.ReviewStatus;
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

            // Calculate counts by severity
            var errorCount = issues.Count(i => i.Severity == "error");
            var warningCount = issues.Count(i => i.Severity == "warning");
            var infoCount = issues.Count(i => i.Severity == "info");

            var status = errorCount > 0 ? "failed" : (warningCount > 0 ? "warning" : "passed");
            var canPublish = errorCount == 0;

            var summary = canPublish
                ? $"Validation passed. {warningCount} warnings, {infoCount} informational messages."
                : $"Validation failed with {errorCount} errors, {warningCount} warnings.";

            return new ValidationResult
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

    #endregion
}
