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
    private readonly List<User> _users = new();
    private readonly List<ValidationRule> _validationRules = new();
    private readonly List<AuditLogEntry> _auditLog = new();
    private readonly List<ReminderConfiguration> _reminderConfigurations = new();
    private readonly List<ReminderHistory> _reminderHistory = new();

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
            new User { Id = "user-6", Name = "Lisa Anderson", Email = "lisa.anderson@company.com", Role = "auditor" }
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

            foreach (var catalogItem in sectionsToInclude)
            {
                var section = new ReportSection
                {
                    Id = Guid.NewGuid().ToString(),
                    PeriodId = newPeriod.Id,
                    Title = catalogItem.Title,
                    Category = catalogItem.Category,
                    Description = catalogItem.Description,
                    OwnerId = request.OwnerId,
                    Status = "draft",
                    Completeness = "empty",
                    Order = order++
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
                    DataPointCount = 0,
                    EvidenceCount = 0,
                    GapCount = 0,
                    AssumptionCount = 0,
                    CompletenessPercentage = 0,
                    OwnerName = request.OwnerName
                });
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
            return string.IsNullOrWhiteSpace(periodId)
                ? _summaries.ToList()
                : _summaries.Where(summary => summary.PeriodId == periodId).ToList();
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
        
        // Check if evidence is linked (at least one evidence ID)
        bool hasEvidence = dataPoint.EvidenceIds != null && dataPoint.EvidenceIds.Count > 0;
        
        // Complete: has all required fields AND evidence
        if (hasTitle && hasContent && hasSource && hasInformationType && hasEvidence)
        {
            return "complete";
        }
        
        // Incomplete: has basic data but missing required metadata or evidence
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

            if (string.IsNullOrWhiteSpace(request.OwnerId))
            {
                return (false, "OwnerId is required.", null);
            }

            // Validate owner exists
            var owner = _users.FirstOrDefault(u => u.Id == request.OwnerId);
            if (owner == null)
            {
                return (false, $"Owner with ID '{request.OwnerId}' not found.", null);
            }

            // Validate contributors exist and are not the owner
            if (request.ContributorIds != null && request.ContributorIds.Any())
            {
                if (request.ContributorIds.Contains(request.OwnerId))
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
                Deadline = request.Deadline
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
                    request.OwnerId == dataPoint.OwnerId &&
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

            // Auto-calculate completeness status if not provided or if it's empty
            string completenessStatus = request.CompletenessStatus;
            if (string.IsNullOrWhiteSpace(completenessStatus))
            {
                // Create temporary data point to calculate status (keeping existing evidence IDs)
                var statusCalculationDataPoint = new DataPoint
                {
                    Title = request.Title,
                    Content = request.Content,
                    Source = request.Source,
                    InformationType = request.InformationType,
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
            
            if (dataPoint.OwnerId != request.OwnerId)
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
            dataPoint.OwnerId = request.OwnerId;
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

    public IReadOnlyList<AuditLogEntry> GetAuditLog(string? entityType = null, string? entityId = null, string? userId = null, string? startDate = null, string? endDate = null)
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

    private sealed record SectionTemplate(string Title, string Category, string Description);
}
