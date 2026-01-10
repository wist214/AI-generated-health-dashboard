# Project Locations - CRITICAL READ FIRST

## Overview

This document specifies where to implement the new refactored system to ensure the **existing working application remains untouched**.

---

## Existing System (DO NOT MODIFY)

**Location:** `HealthAggregatorApi/`

**Contents:**
- Current Azure Functions implementation
- Working dashboard (HTML/JS/CSS)
- Existing sync services (Oura, Picooc, Cronometer)
- Current Program.cs and infrastructure

**Status:** Keep operational during migration. This is your rollback safety net.

---

## New Refactored System (Implementation Location)

### Root Structure

```
HealthAggregator/                          # Repository root
├── HealthAggregatorApi/                   # EXISTING - DO NOT MODIFY (V1)
│   ├── dashboard/                         # Current working UI
│   ├── Functions/                         # Current Azure Functions
│   └── ...
│
├── HealthAggregatorV2/                    # NEW - Create this folder (V2)
│   ├── src/
│   │   ├── Api/                          # Plan 01 - .NET 8 Minimal API
│   │   ├── Domain/                       # Plan 02 - Shared domain models
│   │   ├── Infrastructure/               # Plan 02 - EF Core, repositories
│   │   ├── Functions/                    # Plan 04 - Azure Functions sync
│   │   └── Spa/                          # Plan 03 - React SPA
│   │
│   └── tests/
│       ├── Api.Tests/                    # Unit tests for API
│       ├── Api.E2E/                      # E2E tests for API
│       ├── Functions.Tests/              # Unit tests for Functions
│       ├── Spa.Tests/                    # Unit tests for React
│       └── Spa.E2E/                      # E2E tests for React
│
├── docs/                                  # EXISTING - Documentation
│   ├── plans/                             # Implementation plans
│   └── ...
│
└── HealthAggregator.sln                   # Update to include new projects
```

---

## Implementation Plan Mapping

| Plan | Component | Location | Purpose |
|------|-----------|----------|---------|
| **Plan 01** | Backend API | `HealthAggregatorV2/src/Api/` | .NET 8 Minimal API |
| **Plan 02** | Database/EF Core | `HealthAggregatorV2/src/Domain/`<br>`HealthAggregatorV2/src/Infrastructure/` | Shared data layer |
| **Plan 03** | React SPA | `HealthAggregatorV2/src/Spa/` | React TypeScript UI |
| **Plan 04** | Azure Functions | `HealthAggregatorV2/src/Functions/` | Azure Functions sync |
| **Plan 05** | Integration | *(Skipped for now)* | Plugin architecture (future) |
| **Plan 06** | Testing | `HealthAggregatorV2/tests/*.Tests/`<br>`HealthAggregatorV2/tests/*.E2E/` | Test projects |

---

## Project Naming Convention

### V2 Folder Structure - Clean Naming

- ✅ `HealthAggregatorV2/src/Api/` - Clean project name, V2 folder distinguishes version
- ✅ `HealthAggregatorV2/src/Functions/` - Clean project name, no "New" prefix needed
- ✅ `HealthAggregatorV2/src/Spa/` - Clean project name
- ✅ `HealthAggregatorV2/src/Domain/` - Shared domain models
- ✅ `HealthAggregatorV2/src/Infrastructure/` - Shared infrastructure

### Why HealthAggregatorV2 Folder?

1. **Clarity**: V1 vs V2 - obvious version separation
2. **Clean naming**: No "New" prefix cluttering project names
3. **Isolation**: Entire V2 system in one self-contained folder
4. **Side-by-side**: Both versions can run simultaneously
5. **Future-proof**: Can create V3, V4, etc. if needed
6. **Easy deployment**: Single folder to deploy/archive

---

## Database Strategy

### Option 1: Shared Database (Recommended for Initial Development)

Both old and new systems use the same Azure SQL database:

- Old system: Existing tables (no changes)
- New system: New tables with EF Core migrations
- Allow data comparison and validation

### Option 2: Separate Databases (Production Deployment)

- Old system: Keep using existing database
- New system: Fresh database with clean schema
- Migrate data when confident in new system

### Option 3: Table Prefix Approach (Testing Phase)

- V1 tables: `Sources`, `Measurements`, `DailySummary`
- V2 tables: `V2_Sources`, `V2_Measurements`, `V2_DailySummary`
- Easy to compare data side-by-side

---

## Migration Workflow

### Phase 1: Parallel Implementation (Current)
```
HealthAggregatorApi/  ← V1: Still running, users accessing
HealthAggregatorV2/   ← V2: Being built, tested independently
```

### Phase 2: Testing & Validation
```
HealthAggregatorApi/  ← V1: Production
HealthAggregatorV2/   ← V2: Staging/testing environment
```

### Phase 3: Gradual Cutover
```
HealthAggregatorApi/  ← V1: Fallback available
HealthAggregatorV2/   ← V2: Primary system
```

### Phase 4: Decommission V1 (Future)
```
HealthAggregatorApi/  ← V1: Archive or delete
HealthAggregatorV2/   ← V2: Becomes primary (can rename if desired)
```

---

## URLs and Ports

### Development Environment

**Old System:**
- Dashboard: `http://localhost:7071/dashboard/`
- Functions: `http://localhost:7071/api/*`

**V2 System:**
- API: `http://localhost:5000/api/*` (configurable)
- SPA: `http://localhost:5173/` (Vite default)
- Functions: `http://localhost:7072/api/*` (different port to avoid conflict)

### Azure Deployment

**Old System:**
- Keep existing Azure Functions app
- Keep existing dashboard hosting

**V2 System:**
- New Azure App Service for API (or separate deployment slot)
- Azure Static Web Apps for SPA
- New Azure Functions app for sync (or separate deployment slot)

---

## Solution File Updates

Add new projects to `HealthAggregator.sln`:

```bash
# Navigate to repository root
cd HealthAggregator

# Create V2 solution file
cd HealthAggregatorV2
dotnet new sln -n HealthAggregatorV2

# Add projects to V2 solution
dotnet sln add src/Api/Api.csproj
dotnet sln add src/Domain/Domain.csproj
dotnet sln add src/Infrastructure/Infrastructure.csproj
dotnet sln add src/Functions/Functions.csproj

# Add test projects
dotnet sln add tests/Api.Tests/Api.Tests.csproj
dotnet sln add tests/Functions.Tests/Functions.Tests.csproj
# ... etc
```

---

## Implementation Order

1. **Create folder structure:**
   ```bash
   mkdir HealthAggregatorV2
   cd HealthAggregatorV2
   mkdir src
   mkdir tests
   ```

2. **Start with Domain layer** (Plan 02):
   - `HealthAggregatorV2/src/Domain/` - Entity models
   - `HealthAggregatorV2/src/Infrastructure/` - EF Core, repositories

3. **Build API** (Plan 01):
   - `HealthAggregatorV2/src/Api/` - Minimal API

4. **Build SPA** (Plan 03):
   - `HealthAggregatorV2/src/Spa/` - React application

5. **Build Functions** (Plan 04):
   - `HealthAggregatorV2/src/Functions/` - Sync services

6. **Add Tests** (Plan 06):
   - `HealthAggregatorV2/tests/*.Tests/` - Unit tests
   - `HealthAggregatorV2/tests/*.E2E/` - E2E tests

---

## Key Principles

### ✅ DO:
- Create all V2 code in `HealthAggregatorV2/` folder
- Use clean project names (Api, Functions, Spa, Domain, Infrastructure)
- Keep existing V1 system (`HealthAggregatorApi/`) running
- Test both V1 and V2 systems side-by-side
- Document migration progress

### ❌ DON'T:
- Modify anything in `HealthAggregatorApi/` (V1 folder)
- Delete or rename existing files in V1
- Change existing V1 database tables without strategy
- Deploy V2 over existing V1 Azure resources
- Mix V1 and V2 code

---

## Questions?

If unclear about where to put something:

1. **Is it part of the V2 refactored system?** → Put in `HealthAggregatorV2/src/`
2. **Is it a test for V2?** → Put in `HealthAggregatorV2/tests/`
3. **Is it documentation?** → Put in root `docs/` folder
4. **When in doubt** → It goes in `HealthAggregatorV2/`, NOT `HealthAggregatorApi/`

---

## Summary

**Golden Rule:** Never touch `HealthAggregatorApi/` (V1) - it's your safety net.

All V2 implementation work happens in the `HealthAggregatorV2/` folder with clean project names. The V2 folder itself distinguishes it from the V1 legacy codebase.
