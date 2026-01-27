# Architecture.md — ESG Responsibility Report Platform

## 1. Purpose

This document describes the target architecture for a platform that helps organizations prepare an auditable ESG Responsibility Report as a controlled, repeatable business process (not just a one-off document generator).

Key product intent:
- organize data, process, ownership, and evidence
- separate facts from declarations/plans
- make outputs defensible, traceable, and exportable

## 2. Goals and Non‑Goals

### 2.1 Goals
- Provide a guided workflow to configure reporting scope, collect disclosures, track completeness, and generate/export the report.
- Maintain auditability: each disclosure links to source data, assumptions, evidence, and change history.
- Support multiple report “structures”: simplified SME model and mapping to CSRD/ESRS-like standards.
- Enable iterative reporting across periods with versioning of report structure and content.

### 2.2 Non‑Goals
- Replacing management responsibility for final statements and sign-off.
- Full automation of ESG content creation without user control.
- Building a generic ESG data lake for all enterprise systems in the first iteration.

## 3. System Context

Actors:
- Administrator: manages organization setup, reference data, structure templates, access control.
- Report Owner: defines scope, assigns owners, monitors completeness, exports report.
- Contributor: provides data, uploads evidence, adds assumptions/notes.
- Reviewer/Approver: reviews disclosures, requests changes, approves sections.

External dependencies (future-ready, optional by phase):
- HR system (headcount, training, safety)
- Finance / ERP (energy costs, suppliers, spending)
- Metering / utilities (energy consumption)
- File storage (evidence attachments)
- Identity provider (OIDC/OAuth2)

## 4. Architecture Style and Principles

Recommended style: modular architecture with clear boundaries and versioned API contracts.

Principles:
- API-first boundaries between frontend and backend.
- Clear separation of concerns: Presentation/API, Application (use cases), Domain, Infrastructure.
- Auditable by default: provenance, history, and correlation IDs in logs.
- Explicit decisions: ADRs for architectural choices that affect structure, persistence, export, security.

## 5. High-Level Containers

### 5.1 Frontend (Web App)
Responsibilities:
- Configure organization and reporting scope.
- Manage report structure and sections (enable/disable).
- Data entry for ESG disclosures with evidence uploads.
- Completeness checks and review workflow UI.
- Export initiation and export download.

Suggested frontend shape:
- app shell, routing
- feature modules: scope, structure, disclosures, review, export
- shared API client + error handling + typed DTOs

### 5.2 Backend API (.NET)
Responsibilities:
- AuthN/AuthZ, role/ownership rules.
- Use-case orchestration (commands/queries).
- Domain validation (invariants) and workflow state transitions.
- Export pipeline orchestration (report assembly to DOCX/PDF).
- Audit trail + provenance storage.
- Reference data management.

Layering:
- Api -> Application -> Domain
- Infrastructure implements ports and is wired by DI.

### 5.3 Persistence
Primary store:
- Relational database (SQL Server or PostgreSQL recommended).
Key storage patterns:
- transactional tables for current state
- append-only change history for key disclosures (or dedicated audit tables)
- attachments metadata in DB; binaries in object storage

### 5.4 File Storage (Evidence)
- Object storage (Azure Blob recommended) for evidence files, exports, and generated artifacts.
- Virus scanning and content-type validation where required.

### 5.5 Background Jobs (Optional but likely)
- Export generation (async, retryable).
- Scheduled completeness reminders.
- Integrations data sync (future).

## 6. Core Domain Modules (Bounded Contexts)

### 6.1 Organization & Reporting Scope
- Organization profile, legal entity info, reporting period, organizational structure.
- Report “mode”: simplified (SME) vs extended.
- Assign users to roles and scope.

### 6.2 Report Structure Model
- Manage report sections and hierarchy.
- Enable/disable sections per report.
- Versioning of structure.
- Mapping: section <-> standard reference (e.g., CSRD/ESRS identifiers).

### 6.3 Disclosures & Evidence
- Disclosures for E/S/G with:
  - type: fact / declaration / plan
  - completeness status
  - owner + contributors
  - sources, assumptions, estimations, boundaries
  - evidence attachments and references
- Validation rules and “defensibility checks”.

### 6.4 Review & Approval Workflow
- Section-level workflow:
  - Draft -> In Review -> Changes Requested -> Approved
- Comments, tasks, and traceable approvals.

### 6.5 Export & Publishing
- Report assembly:
  - selected structure version
  - included sections
  - approved content snapshot
- Export formats:
  - DOCX first (more controllable), PDF as derived output
- Watermarks or status labels: Draft/Final; reporting period.

### 6.6 Reference Data (Admin)
- Company metadata dictionaries as needed.
- Units, currencies (if applicable), KPI definitions.
- Templates for simplified reporting.

## 7. Key Data Concepts

Core entities (conceptual):
- Organization
- ReportingPeriod
- Report (per period)
- StructureTemplate, StructureVersion, Section
- Disclosure (per section)
- Evidence (file + metadata)
- Assumption / Estimation
- MappingToStandard (e.g., ESRS reference)
- ReviewState, Comment, ApprovalRecord
- AuditEvent (who/when/what changed; reason)

Auditability:
- created/updated timestamps and user IDs everywhere
- append-only events for critical disclosures and approvals
- immutable “Export Snapshot” for final output

## 8. API and Contracting

- Versioned API path (/api/v1).
- DTOs owned by API/Application boundary (no domain entities exposed).
- Standard error model (ProblemDetails) + correlation ID.
- OpenAPI published for non-prod and CI artifact.

## 9. Security

Authentication:
- OIDC/OAuth2 (Azure Entra ID recommended).

Authorization:
- Roles: Admin, Owner, Contributor, Reviewer.
- Ownership rules at report/section/disclosure level.
- Evidence access aligned with report membership.

Data protection:
- Secrets in secret manager (never in repo).
- Encryption at rest (DB and storage) and TLS in transit.
- PII minimization and GDPR-aware data retention.

## 10. Observability and Audit

- Structured logs with correlation ID, user ID, report ID.
- Metrics: export duration, error rate, API latency.
- Tracing (OpenTelemetry) where available.
- Separate audit log that is user-visible for accountability (not just technical logs).

## 11. Performance and Scalability

Expected hotspots:
- large evidence uploads
- export generation
- completeness checks across many sections

Approach:
- async exports and background jobs
- pagination and server-side filtering
- caching of structure templates and standard mappings

## 12. Deployment and Environments

Baseline environments:
- dev, test, prod

Recommended deployment options (Azure):
- Backend: App Service or Container Apps
- Frontend: Static Web Apps or CDN hosting
- DB: Azure SQL / PostgreSQL
- Storage: Azure Blob
- Identity: Entra ID

CI/CD checks:
- build, tests, formatting
- OpenAPI generation
- security scanning (SAST/dep scanning)
- migration validation

## 13. Risks and Mitigations

- Incomplete data leading to misleading statements:
  - enforce “fact vs declaration vs plan”
  - mandatory assumptions and boundaries fields
- Legal defensibility:
  - immutable snapshots and approval records
  - explicit limitations section generation
- Scope creep (standards complexity):
  - keep CSRD/ESRS mapping as metadata first
  - structure versioning to manage evolution
- Export complexity:
  - start with DOCX templating and deterministic assembly
  - add PDF conversion later

## 14. Open Decisions

- Database choice: SQL Server vs PostgreSQL.
- Export engine: DOCX templating library and PDF conversion approach.
- Evidence storage: local (dev) vs Blob (prod) + scanning.
- Workflow strictness: section-only vs per-disclosure approvals.
- Integrations roadmap: HR/ERP utilities; data import formats (CSV/API).

## 15. ADR Index (to be created)

Recommended ADRs to write early:
- ADR-001: Architecture style and layering rules
- ADR-002: Persistence model and audit trail strategy
- ADR-003: Export approach (DOCX/PDF) and templating
- ADR-004: Identity provider and authorization model
- ADR-005: Evidence storage and retention policy
