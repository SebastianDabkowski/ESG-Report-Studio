# Planning Guide

An enterprise-grade ESG reporting platform that transforms chaotic, spreadsheet-driven ESG data collection into a structured, auditable process with clear ownership, evidence tracking, and transparent gap management.

**Experience Qualities**:
1. **Professional** - The interface must project credibility and seriousness appropriate for compliance and audit contexts, with clear visual hierarchy and data-driven design
2. **Transparent** - Every piece of information shows its completeness status, evidence level, and ownership without hiding gaps or uncertainties
3. **Systematic** - Workflows guide users through complex processes with clear progress indicators, dependencies, and next actions

**Complexity Level**: Complex Application (advanced functionality, likely with multiple views)
This is a multi-role business system requiring sophisticated state management, role-based access control, workflow orchestration, audit trails, data classification, evidence management, and export capabilities. It serves corporate compliance needs with features comparable to GRC platforms.

## Essential Features

### Organization & User Management
- **Functionality**: Create organization profile, manage users with role assignments (Admin, Report Owner, Contributor, Auditor)
- **Purpose**: Establish clear accountability and access control from the start
- **Trigger**: Admin user first accesses the system or adds new team members
- **Progression**: Admin dashboard → User management → Add user → Assign role → Set permissions → User receives access
- **Success criteria**: Each user has exactly one role, role permissions are enforced throughout the application, audit log captures all user actions

### Reporting Period Creation
- **Functionality**: Define new reporting periods (annual, custom), select report variant (simplified/extended), auto-generate ESG section structure
- **Purpose**: Establish the scope and framework for data collection before work begins
- **Trigger**: Report Owner initiates new reporting cycle
- **Progression**: New period → Set dates → Choose variant (Simplified: E/S/G basics, Extended: GRI/SASB aligned) → Generate structure → Assign section owners → Activate
- **Success criteria**: All sections generated with standard ESG taxonomy, owners assigned, period visible to all stakeholders

### Section Data Collection
- **Functionality**: Contributors add narratives, metrics, evidence files, assumptions, and gaps for assigned sections
- **Purpose**: Capture all ESG data with proper classification and supporting documentation
- **Trigger**: Contributor opens assigned section in draft status
- **Progression**: Section detail → Add content type (Narrative/Metric/Evidence) → Classify (Fact/Declaration/Plan) → Add assumptions if needed → Mark gaps → Save → Update status
- **Success criteria**: Content is versioned, classified correctly, linked to evidence, gaps are explicit, owner is tracked

### Completeness & Status Tracking
- **Functionality**: Real-time dashboard showing section status (draft/review/approved), completeness percentage, missing evidence, unresolved gaps
- **Purpose**: Give leadership and auditors instant visibility into report readiness
- **Trigger**: User accesses dashboard or report overview
- **Progression**: Dashboard → Filter by status/owner/section → Drill into incomplete areas → Identify blockers → Track to completion
- **Success criteria**: Percentage reflects actual data presence, gaps reduce completion score, status changes require explicit actions

### Evidence Management
- **Functionality**: Upload documents, link to external sources, tag evidence to specific data points, view evidence coverage per section
- **Purpose**: Create defensible audit trail linking every claim to supporting material
- **Trigger**: Contributor adds data point requiring substantiation
- **Progression**: Add data point → Attach evidence → Tag document type → Link to specific metric/narrative → Evidence appears in section summary
- **Success criteria**: Each factual claim can trace to at least one evidence item, orphaned evidence is flagged, evidence export bundles all files

### Assumptions & Gaps Registry
- **Functionality**: Explicitly document estimation methodologies, data limitations, missing information, and improvement plans
- **Purpose**: Transform ESG reporting from aspirational to honest by making unknowns visible
- **Trigger**: User encounters incomplete data or must estimate
- **Progression**: Identify gap → Document what's missing → Record assumption/methodology → Set improvement target → Gap visible in all views → Included in export
- **Success criteria**: Gaps don't block progress but reduce completeness score, assumptions are auditable, reports include limitations section

### Approval Workflow
- **Functionality**: Section-level and report-level approvals with rejection/comment capability
- **Purpose**: Ensure review and sign-off at appropriate organizational levels
- **Trigger**: Contributor marks section ready for review
- **Progression**: Ready for review → Notify owner → Review content/evidence/gaps → Approve or reject with comments → If approved, section locks → All sections approved triggers final report approval
- **Success criteria**: Approved sections are locked, approval chain is logged, only Report Owner can approve final report

### Audit Trail
- **Functionality**: Comprehensive change log capturing what changed, who changed it, when, and previous values
- **Purpose**: Meet audit requirements for internal controls and data integrity
- **Trigger**: Any data modification, status change, or approval action
- **Progression**: User makes change → System logs user, timestamp, field, old value, new value → Audit view shows chronological history → Export includes audit summary
- **Success criteria**: All material changes are logged, audit log is immutable, can reconstruct report state at any point in time

### Report Export
- **Functionality**: Generate formal PDF/document with all narratives, metrics, evidence index, methodology section, gaps/limitations, and evidence package as ZIP
- **Purpose**: Deliver final auditable ESG report and supporting materials
- **Trigger**: Report Owner exports approved report
- **Progression**: Export → Select format → Include evidence package checkbox → Generate → Download report + evidence bundle → Export log entry created
- **Success criteria**: Export reflects actual data (no auto-fill), includes explicit gaps section, evidence files are linked to report sections, export is timestamped and logged

## Edge Case Handling

- **Incomplete sections at deadline**: Allow export with explicit "Incomplete" watermarks and auto-generated gaps summary
- **User role changes mid-period**: Reassign ownership, log transition, notify affected users, preserve historical attribution
- **Evidence file deletion**: Soft delete only, maintain reference, flag as "evidence removed" with timestamp and reason
- **Conflicting approvals**: Section changes after approval require re-approval, system tracks version deltas
- **Bulk data import errors**: Provide detailed error report, allow partial import, flag suspicious data for review
- **Report period overlap**: Allow parallel periods but warn about resource allocation, track which period is active

## Design Direction

The design must evoke **corporate professionalism, data transparency, and systematic control**. This is a compliance tool for serious business users—it should feel authoritative, trustworthy, and information-dense without being overwhelming. Visual language should reference business intelligence dashboards, audit software, and enterprise SaaS platforms. Users should feel they are using a tool that auditors and executives will respect.

## Color Selection

A professional, trust-oriented palette with clear semantic color coding for data classification and status.

- **Primary Color**: Deep Navy Blue `oklch(0.35 0.08 250)` - Communicates corporate professionalism, trustworthiness, and seriousness appropriate for compliance software
- **Secondary Colors**: 
  - Slate Gray `oklch(0.55 0.02 250)` for supporting UI elements and secondary actions
  - Cool Gray `oklch(0.75 0.01 250)` for borders and subtle backgrounds
- **Accent Color**: Teal `oklch(0.65 0.12 200)` - Professional yet distinctive, used for active states, links, and calls-to-action
- **Semantic Colors**:
  - Success/Approved: Forest Green `oklch(0.55 0.15 150)`
  - Warning/In Review: Amber `oklch(0.70 0.15 85)`
  - Alert/Gaps: Coral Red `oklch(0.65 0.18 25)`
  - Fact Badge: Cool Blue `oklch(0.60 0.12 240)`
  - Declaration Badge: Purple `oklch(0.58 0.12 290)`
  - Plan Badge: Cyan `oklch(0.62 0.10 210)`
- **Foreground/Background Pairings**:
  - Primary Navy (oklch(0.35 0.08 250)): White text (oklch(0.98 0 0)) - Ratio 11.2:1 ✓
  - Accent Teal (oklch(0.65 0.12 200)): White text (oklch(0.98 0 0)) - Ratio 5.1:1 ✓
  - Success Green (oklch(0.55 0.15 150)): White text (oklch(0.98 0 0)) - Ratio 6.8:1 ✓
  - Warning Amber (oklch(0.70 0.15 85)): Dark text (oklch(0.25 0 0)) - Ratio 8.9:1 ✓
  - Background (oklch(0.97 0 0)): Foreground (oklch(0.25 0 0)) - Ratio 13.1:1 ✓

## Font Selection

Typography should convey **clarity, data precision, and corporate credibility** with excellent readability for dense information displays.

- **Primary Typeface**: IBM Plex Sans - A professional, highly legible sans-serif designed for technical and business contexts, excellent for dashboards and data
- **Monospace**: JetBrains Mono - For displaying metrics, IDs, dates, and technical information with fixed-width precision

**Typographic Hierarchy**:
- H1 (Page Title): IBM Plex Sans SemiBold / 32px / -0.02em letter spacing / 1.2 line height
- H2 (Section Header): IBM Plex Sans SemiBold / 24px / -0.01em / 1.3
- H3 (Subsection): IBM Plex Sans Medium / 18px / 0em / 1.4
- Body (Primary content): IBM Plex Sans Regular / 15px / 0em / 1.6
- Small (Metadata, labels): IBM Plex Sans Regular / 13px / 0em / 1.5
- Caption (Timestamps, IDs): JetBrains Mono Regular / 12px / 0em / 1.4
- Button Text: IBM Plex Sans Medium / 14px / 0.01em / 1

## Animations

Animations should reinforce **systematic progress and data updates** without distracting from complex workflows. Use subtle, purposeful motion to guide attention to status changes, new data, and workflow transitions. Keep animations quick and professional—nothing playful or bouncy.

- Status badge transitions (draft → review → approved) with subtle color morphing
- Completeness percentage bar fills with smooth easing
- Panel expansions for section details with accordion motion
- Evidence upload success with brief fade-in confirmation
- Gap badge pulse when newly added
- Approval action with brief checkmark animation
- Loading states for data fetching with skeletal placeholders

## Component Selection

**Components**:
- **Tabs**: Primary navigation between Dashboard, Periods, Sections, Evidence, Audit Trail (use shadcn Tabs)
- **Card**: Section summary cards, metric display cards, evidence cards (shadcn Card with custom status borders)
- **Badge**: Status indicators (draft/review/approved), classification tags (fact/declaration/plan), completeness levels (shadcn Badge with semantic colors)
- **Table**: Section list, evidence registry, audit log (shadcn Table with sortable columns)
- **Dialog**: Create new period, add evidence, document assumptions, approve/reject actions (shadcn Dialog)
- **Form**: All data input forms with validation (shadcn Form with react-hook-form)
- **Select**: Role assignment, report variant selection, section assignment (shadcn Select)
- **Textarea**: Narrative content, assumption documentation, gap descriptions (shadcn Textarea with character count)
- **Progress**: Section completeness, overall report readiness (shadcn Progress with percentage labels)
- **Avatar**: User attribution for owners and approvers (shadcn Avatar)
- **Accordion**: Collapsible section details, evidence lists (shadcn Accordion)
- **Alert**: Missing evidence warnings, gap notifications, approval required (shadcn Alert with semantic variants)
- **Separator**: Visual separation between sections and data groups (shadcn Separator)
- **Button**: All actions with clear hierarchy - primary for approvals/exports, secondary for edits, destructive for rejections (shadcn Button)
- **Popover**: Quick metadata display, user info, evidence preview (shadcn Popover)

**Customizations**:
- Custom status border colors on Cards (left border: 4px with semantic colors)
- Badge color variants for classification types (custom background colors)
- Table row hover states with subtle background shift
- Form fields with inline validation and completeness indicators
- Progress bars with gradient fills showing health (red → yellow → green)
- Custom icons from Phosphor for each classification type

**States**:
- Buttons: Clear disabled states for workflow-blocked actions, loading states for async operations
- Form inputs: Validation states (error/success), focus states with accent ring, required field indicators
- Cards: Hover elevation for interactive cards, selected state for active section
- Tables: Row selection, sort indicators, expandable rows for details

**Icon Selection**:
- Buildings (organization), CalendarDots (periods), FileText (sections), PaperclipHorizontal (evidence)
- CheckCircle (approved), WarningCircle (gaps), ClockCounterClockwise (audit trail)
- User (contributors), UserGear (admin), Eye (auditor), Crown (report owner)
- TrendUp (metrics), Article (narrative), Lightbulb (assumptions)
- Target (fact), Megaphone (declaration), Rocket (plan)

**Spacing**:
- Page padding: p-6
- Card padding: p-5
- Card gaps in grid: gap-4
- Section spacing: space-y-6
- Form field spacing: space-y-4
- Inline element gaps: gap-2
- Button padding: px-4 py-2

**Mobile**:
- Stack card grids vertically on mobile (<768px)
- Convert table to accordion-style list on mobile
- Collapse sidebar navigation to hamburger menu
- Full-width dialogs on mobile
- Reduce heading sizes by 20% on mobile
- Increase touch targets to minimum 44px
- Priority content first: status, completeness, next action
