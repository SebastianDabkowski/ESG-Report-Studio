# Copilot Instructions for ESG Report Studio

## Repository Overview

ESG Report Studio is an enterprise-grade ESG (Environmental, Social, Governance) reporting platform that transforms spreadsheet-driven ESG data collection into a structured, auditable process. The system supports both simplified SME reporting and extended CSRD/ESRS-aligned reporting.

**Key capabilities:**
- Reporting scope configuration and structure management
- ESG data collection with evidence tracking
- Ownership assignment and completeness tracking
- Assumptions and gap documentation
- Approval workflows and audit trails
- Report generation and export

## Project Structure

```
ESG-Report-Studio/
├── src/
│   ├── backend/                    # .NET 9 API backend
│   │   ├── Application/
│   │   │   └── ARP.ESG_ReportStudio.API/   # Web API project
│   │   ├── Modules/
│   │   │   └── SD.ProjectName.Modules.Products/  # Feature module
│   │   ├── Tests/
│   │   │   └── SD.ProjectName.Tests.Products/    # Unit tests
│   │   └── SD.ProjectName.sln      # Solution file
│   └── frontend/                   # React 19 + TypeScript frontend
│       ├── src/                    # Application source
│       ├── package.json            # npm dependencies
│       └── vite.config.ts          # Vite build config
├── docs/
│   └── adr/                        # Architecture Decision Records
├── architecture.md                 # High-level architecture documentation
└── README.md                       # Project overview
```

## Technology Stack

### Backend (.NET)
- **Runtime:** .NET 9, C# 13
- **Framework:** ASP.NET Core Web API
- **Architecture:** N-Layer (Api → Application → Domain, Infrastructure implements ports)
- **Database:** Entity Framework Core (SQL Server or PostgreSQL)
- **Testing:** xUnit with Moq

### Frontend (React)
- **Runtime:** Node.js with npm
- **Framework:** React 19 with TypeScript 5.7
- **Build Tool:** Vite 7
- **Styling:** Tailwind CSS 4
- **UI Components:** Radix UI primitives, shadcn/ui patterns
- **State Management:** TanStack Query
- **Form Handling:** React Hook Form with Zod validation

## Build and Test Commands

### Frontend

```bash
# Navigate to frontend directory
cd src/frontend

# Install dependencies (always run first)
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Run linting
npm run lint

# Preview production build
npm run preview
```

### Backend

```bash
# Navigate to backend directory
cd src/backend

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run specific project
dotnet run --project Application/ARP.ESG_ReportStudio.API
```

### Database Migrations (Backend)

```bash
# Add migration for Products module
dotnet ef migrations add <MigrationName> \
  -p Modules/SD.ProjectName.Modules.Products \
  -s Application/ARP.ESG_ReportStudio.API \
  -c ProductDbContext

# Apply migrations
dotnet ef database update \
  -p Modules/SD.ProjectName.Modules.Products \
  -s Application/ARP.ESG_ReportStudio.API \
  -c ProductDbContext
```

## Architecture Rules

### Backend Layering

1. **Api (Presentation):** HTTP endpoints, authentication, validation, DTOs only
2. **Application:** Use cases, orchestration, transaction boundaries, defines interfaces
3. **Domain:** Pure business logic, entities, value objects, no framework dependencies
4. **Infrastructure:** EF Core, repositories, external integrations, implements interfaces

**Dependency rules:**
- Api → Application → Domain
- Infrastructure → Application / Domain
- Domain has no outward dependencies
- Never expose Domain entities via API; use DTOs

### Frontend Structure

- Feature components in `src/components/` (e.g., Dashboard.tsx, SectionsView.tsx)
- Reusable UI primitives in `src/components/ui/` (shadcn/ui-based components)
- Hooks in `src/hooks/`
- Utilities in `src/lib/`
- Use typed DTOs matching backend contracts

## Coding Conventions

### Backend (.NET)
- Use async/await everywhere
- Repository pattern with interface abstractions
- ProblemDetails for error responses
- Keep business logic in Domain layer
- Application services should be small and testable

### Frontend (TypeScript)
- Prefer functional components with hooks
- Use TypeScript strict mode
- Validate forms with Zod schemas
- Use TanStack Query for server state

## Key Files to Know

- `src/backend/SD.ProjectName.sln` - .NET solution entry point
- `src/backend/Application/ARP.ESG_ReportStudio.API/Program.cs` - API startup and DI
- `src/frontend/package.json` - npm scripts and dependencies
- `src/frontend/vite.config.ts` - Vite configuration
- `architecture.md` - Detailed architecture documentation
- `docs/adr/` - Architecture Decision Records

## Working with This Repository

1. **Before making changes:** Read relevant ADRs and architecture documentation
2. **Backend changes:** Follow N-Layer patterns; update tests for new code
3. **Frontend changes:** Use existing component patterns from `src/components/`
4. **Schema changes:** Create EF Core migrations in the correct module
5. **New features:** Add Application service, register in DI, create tests

## Common Tasks

### Adding a Backend Feature
1. Add/update Domain entities and interfaces
2. Implement repository in Infrastructure
3. Create Application service (use case)
4. Add API endpoint with DTO mapping
5. Register services in DI (Program.cs)
6. Write unit tests with mocked repositories

### Adding a Frontend Feature
1. Create feature component in `src/components/`
2. Define TypeScript types matching API DTOs
3. Implement API hooks with TanStack Query
4. Build UI using existing primitives from `src/components/ui/`
5. Add form validation with Zod if needed
