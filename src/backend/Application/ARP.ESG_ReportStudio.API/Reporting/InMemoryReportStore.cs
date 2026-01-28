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

            unit.Name = request.Name;
            unit.ParentId = request.ParentId;
            unit.Description = request.Description;

            return unit;
        }
    }

    public bool DeleteOrganizationalUnit(string id)
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
                    ? now : null
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
            
            // Update estimate provenance fields
            dataPoint.EstimateInputSources = request.EstimateInputSources ?? new List<EstimateInputSource>();
            dataPoint.EstimateInputs = request.EstimateInputs;
            
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

    public bool DeleteDataPoint(string id)
    {
        lock (_lock)
        {
            var dataPoint = _dataPoints.FirstOrDefault(d => d.Id == id);
            if (dataPoint == null)
            {
                return false;
            }

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
        string uploadedBy)
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
                LinkedDataPoints = new List<string>()
            };

            _evidence.Add(newEvidence);
            return (true, null, newEvidence);
        }
    }

    public (bool IsValid, string? ErrorMessage) LinkEvidenceToDataPoint(string evidenceId, string dataPointId)
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

            return (true, null);
        }
    }

    public (bool IsValid, string? ErrorMessage) UnlinkEvidenceFromDataPoint(string evidenceId, string dataPointId)
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

            return (true, null);
        }
    }

    public bool DeleteEvidence(string id)
    {
        lock (_lock)
        {
            var evidence = _evidence.FirstOrDefault(e => e.Id == id);
            if (evidence == null)
            {
                return false;
            }

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

            return (true, null, assumption);
        }
    }

    public (bool IsValid, string? ErrorMessage) DeprecateAssumption(
        string id,
        string? replacementAssumptionId,
        string? justification)
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
            }
            else
            {
                assumption.DeprecationJustification = justification;
                assumption.Status = "invalid";
            }

            return (true, null);
        }
    }

    public (bool IsValid, string? ErrorMessage) LinkAssumptionToDataPoint(string assumptionId, string dataPointId)
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
            return (true, null);
        }
    }

    public (bool IsValid, string? ErrorMessage) UnlinkAssumptionFromDataPoint(string assumptionId, string dataPointId)
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
            return (true, null);
        }
    }

    public (bool IsValid, string? ErrorMessage) DeleteAssumption(string id)
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

            _assumptions.Remove(assumption);
            return (true, null);
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
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("O"),
                UserId = createdBy,
                UserName = createdBy,
                Action = "create",
                EntityType = "simplification",
                EntityId = simplification.Id,
                ChangeNote = $"Created simplification '{title}' for section {sectionId}"
            });

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
                _auditLog.Add(new AuditLogEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow.ToString("O"),
                    UserId = updatedBy,
                    UserName = updatedBy,
                    Action = "update",
                    EntityType = "simplification",
                    EntityId = simplification.Id,
                    ChangeNote = $"Updated simplification '{title}'",
                    Changes = changes
                });
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
            _auditLog.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow.ToString("O"),
                UserId = deletedBy,
                UserName = deletedBy,
                Action = "delete",
                EntityType = "simplification",
                EntityId = simplification.Id,
                ChangeNote = $"Removed simplification '{simplification.Title}'"
            });

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

            // Validate rule type
            var validRuleTypes = new[] { "non-negative", "required-unit", "allowed-units", "value-within-period" };
            if (!validRuleTypes.Contains(request.RuleType, StringComparer.OrdinalIgnoreCase))
            {
                return (false, $"RuleType must be one of: {string.Join(", ", validRuleTypes)}.", null);
            }

            rule.RuleType = request.RuleType;
            rule.TargetField = request.TargetField;
            rule.Parameters = request.Parameters;
            rule.ErrorMessage = request.ErrorMessage;
            rule.IsActive = request.IsActive;

            return (true, null, rule);
        }
    }

    public bool DeleteValidationRule(string id)
    {
        lock (_lock)
        {
            var rule = _validationRules.FirstOrDefault(r => r.Id == id);
            if (rule == null)
            {
                return false;
            }

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

    public IReadOnlyList<AuditLogEntry> GetAuditLog(string? entityType = null, string? entityId = null, string? userId = null, string? action = null, string? startDate = null, string? endDate = null)
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

            return query.OrderByDescending(e => e.Timestamp).ToList();
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

            plan.Title = title;
            plan.Description = description;
            plan.TargetPeriod = targetPeriod;
            plan.OwnerId = ownerId;
            plan.OwnerName = ownerName;
            plan.Priority = priority;
            plan.Status = status;
            plan.UpdatedBy = updatedBy;
            plan.UpdatedAt = DateTime.UtcNow.ToString("O");

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

    public bool DeleteRemediationPlan(string id)
    {
        lock (_lock)
        {
            var plan = _remediationPlans.FirstOrDefault(p => p.Id == id);
            if (plan == null)
            {
                return false;
            }

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

            action.Title = title;
            action.Description = description;
            action.OwnerId = ownerId;
            action.OwnerName = ownerName;
            action.DueDate = dueDate;
            action.Status = status;
            action.UpdatedBy = updatedBy;
            action.UpdatedAt = DateTime.UtcNow.ToString("O");

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

    public bool DeleteRemediationAction(string id)
    {
        lock (_lock)
        {
            var action = _remediationActions.FirstOrDefault(a => a.Id == id);
            if (action == null)
            {
                return false;
            }

            _remediationActions.Remove(action);
            return true;
        }
    }
}
