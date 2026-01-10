# HealthAggregatorV2 - Clean Folder Structure

## Overview

The V2 refactored system uses a **clean, self-contained folder structure** with no "New" prefixes. Version separation is achieved through the `HealthAggregatorV2` root folder.

---

## Complete Folder Structure

```
HealthAggregator/                      # Git repository root
│
├── HealthAggregatorApi/               # V1 - EXISTING (DO NOT MODIFY)
│   ├── dashboard/
│   ├── Functions/
│   └── ...
│
├── HealthAggregatorV2/                # V2 - NEW (Your implementation)
│   │
│   ├── HealthAggregatorV2.sln        # V2 Solution file
│   │
│   ├── src/
│   │   │
│   │   ├── Api/                      # .NET 8 Minimal API
│   │   │   ├── Api.csproj
│   │   │   ├── Program.cs
│   │   │   ├── Endpoints/
│   │   │   ├── Services/
│   │   │   ├── DTOs/
│   │   │   ├── Extensions/
│   │   │   └── Middleware/
│   │   │
│   │   ├── Domain/                   # Shared domain models
│   │   │   ├── Domain.csproj
│   │   │   ├── Entities/
│   │   │   │   ├── Source.cs
│   │   │   │   ├── MetricType.cs
│   │   │   │   ├── Measurement.cs
│   │   │   │   └── DailySummary.cs
│   │   │   ├── Common/
│   │   │   └── Enums/
│   │   │
│   │   ├── Infrastructure/           # EF Core, repositories
│   │   │   ├── Infrastructure.csproj
│   │   │   ├── Data/
│   │   │   │   ├── HealthDbContext.cs
│   │   │   │   └── Configurations/
│   │   │   └── Repositories/
│   │   │       ├── Interfaces/
│   │   │       └── (implementations)
│   │   │
│   │   ├── Functions/                # Azure Functions sync
│   │   │   ├── Functions.csproj
│   │   │   ├── Program.cs
│   │   │   ├── host.json
│   │   │   ├── Functions/
│   │   │   │   └── SyncTimerFunction.cs
│   │   │   ├── Application/
│   │   │   │   └── Services/
│   │   │   └── Extensions/
│   │   │
│   │   └── Spa/                      # React TypeScript SPA
│   │       ├── package.json
│   │       ├── vite.config.ts
│   │       ├── tsconfig.json
│   │       ├── src/
│   │       │   ├── main.tsx
│   │       │   ├── App.tsx
│   │       │   ├── features/
│   │       │   ├── shared/
│   │       │   └── styles/
│   │       └── public/
│   │
│   └── tests/
│       ├── Api.Tests/                # API unit tests
│       │   └── Api.Tests.csproj
│       ├── Api.E2E/                  # API E2E tests
│       │   └── Api.E2E.csproj
│       ├── Functions.Tests/          # Functions unit tests
│       │   └── Functions.Tests.csproj
│       ├── Spa.Tests/                # React unit tests
│       │   └── package.json
│       └── Spa.E2E/                  # React E2E tests
│           └── package.json
│
├── docs/                              # Documentation (shared)
│   ├── IMPLEMENTATION-GUIDE.md
│   ├── QUICK-START.md
│   ├── V2-STRUCTURE.md               # This file
│   └── plans/
│       ├── 00-PROJECT-LOCATIONS.md
│       ├── 01-backend-api-implementation.md
│       └── ...
│
└── HealthAggregator.sln              # Original V1 solution (optional to keep)
```

---

## Project Names

### Namespace Convention

All V2 projects use the `HealthAggregatorV2` namespace prefix:

- `HealthAggregatorV2.Api`
- `HealthAggregatorV2.Domain`
- `HealthAggregatorV2.Infrastructure`
- `HealthAggregatorV2.Functions`
- (Spa uses Node/React, no .NET namespace)

### Example Namespaces in Code

```csharp
// In Api project
namespace HealthAggregatorV2.Api.Services;

// In Domain project
namespace HealthAggregatorV2.Domain.Entities;

// In Infrastructure project
namespace HealthAggregatorV2.Infrastructure.Data;

// In Functions project
namespace HealthAggregatorV2.Functions.Application.Services;
```

---

## Creating the V2 Structure

### Step 1: Create Root Folder

```bash
cd d:\Work\My\HealthAggregator
mkdir HealthAggregatorV2
cd HealthAggregatorV2
```

### Step 2: Create Subfolders

```bash
mkdir src
mkdir tests
```

### Step 3: Create Solution

```bash
dotnet new sln -n HealthAggregatorV2
```

### Step 4: Create Projects

```bash
# Domain project
cd src
dotnet new classlib -n Domain -f net8.0

# Infrastructure project
dotnet new classlib -n Infrastructure -f net8.0

# API project
dotnet new web -n Api -f net8.0

# Functions project
dotnet new func -n Functions --worker-runtime dotnet-isolated

# Spa project
npm create vite@latest Spa -- --template react-ts

cd ..
```

### Step 5: Add Projects to Solution

```bash
dotnet sln add src/Domain/Domain.csproj
dotnet sln add src/Infrastructure/Infrastructure.csproj
dotnet sln add src/Api/Api.csproj
dotnet sln add src/Functions/Functions.csproj
```

### Step 6: Add Project References

```bash
# Infrastructure references Domain
cd src/Infrastructure
dotnet add reference ../Domain/Domain.csproj

# API references Domain and Infrastructure
cd ../Api
dotnet add reference ../Domain/Domain.csproj
dotnet add reference ../Infrastructure/Infrastructure.csproj

# Functions references Domain and Infrastructure
cd ../Functions
dotnet add reference ../Domain/Domain.csproj
dotnet add reference ../Infrastructure/Infrastructure.csproj

cd ../..
```

---

## Benefits of V2 Folder Structure

### ✅ Advantages

1. **Clean Naming**
   - No "New" prefix cluttering project names
   - `Api.csproj` instead of `HealthAggregator.NewApi.csproj`
   - Professional, production-ready names

2. **Clear Version Separation**
   - V1 = `HealthAggregatorApi/`
   - V2 = `HealthAggregatorV2/`
   - No confusion about which version

3. **Self-Contained**
   - Entire V2 system in one folder
   - Can be deployed, archived, or moved independently
   - Own solution file

4. **Future-Proof**
   - Can create `HealthAggregatorV3/` if needed
   - Consistent versioning strategy
   - Easy to understand for new developers

5. **Easy Transition**
   - When V2 becomes primary, can rename folder if desired
   - Or keep versioned structure for history
   - Flexible migration path

### 🆚 Comparison with "New" Prefix Approach

| Aspect | V2 Folder | "New" Prefix |
|--------|-----------|--------------|
| Project names | `Api.csproj` | `HealthAggregator.NewApi.csproj` |
| Namespaces | `HealthAggregatorV2.Api` | `HealthAggregator.NewApi` |
| File paths | `HealthAggregatorV2/src/Api/` | `src/HealthAggregator.NewApi/` |
| Clarity | ⭐⭐⭐⭐⭐ Very clear | ⭐⭐⭐ Clear but verbose |
| Professional | ✅ Yes | ⚠️ Temporary naming |
| Future renames | ✅ Easy | ❌ Need to remove "New" |

---

## Development URLs

### V1 (Existing)
- Dashboard: `http://localhost:7071/dashboard/`
- API: `http://localhost:7071/api/*`

### V2 (New)
- API: `http://localhost:5000/api/*`
- SPA: `http://localhost:5173/`
- Functions: `http://localhost:7072/api/*`

---

## Database Configuration

### Connection Strings

Both V1 and V2 can share the same database or use separate databases.

**Option 1: Shared Database (Recommended for Development)**
```json
// HealthAggregatorV2/src/Api/appsettings.json
{
  "ConnectionStrings": {
    "HealthDb": "Server=...;Database=HealthAggregator;..."
  }
}
```

**Option 2: Separate Database**
```json
{
  "ConnectionStrings": {
    "HealthDb": "Server=...;Database=HealthAggregatorV2;..."
  }
}
```

**Option 3: Shared Database with V2 Table Prefix**
```csharp
// In Infrastructure/Data/Configurations
builder.ToTable("V2_Measurements");  // Instead of "Measurements"
```

---

## Migration Assembly Configuration

EF Core migrations are stored in the API project:

```csharp
// In Infrastructure
services.AddDbContext<HealthDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Migrations are in the Api project
        sqlOptions.MigrationsAssembly("HealthAggregatorV2.Api");
        // Or if using simple project name:
        sqlOptions.MigrationsAssembly("Api");
    });
});
```

---

## Running Both Versions Simultaneously

### Terminal 1: V1 (Existing)
```bash
cd d:\Work\My\HealthAggregator\HealthAggregatorApi
func start
# Runs at: http://localhost:7071
```

### Terminal 2: V2 API
```bash
cd d:\Work\My\HealthAggregator\HealthAggregatorV2\src\Api
dotnet run
# Runs at: http://localhost:5000
```

### Terminal 3: V2 SPA
```bash
cd d:\Work\My\HealthAggregator\HealthAggregatorV2\src\Spa
npm run dev
# Runs at: http://localhost:5173
```

### Terminal 4: V2 Functions
```bash
cd d:\Work\My\HealthAggregator\HealthAggregatorV2\src\Functions
func start
# Runs at: http://localhost:7072
```

---

## Git Configuration

### .gitignore Updates

Add to root `.gitignore`:

```gitignore
# V2 Build outputs
HealthAggregatorV2/src/*/bin/
HealthAggregatorV2/src/*/obj/
HealthAggregatorV2/tests/*/bin/
HealthAggregatorV2/tests/*/obj/

# V2 SPA
HealthAggregatorV2/src/Spa/node_modules/
HealthAggregatorV2/src/Spa/dist/
HealthAggregatorV2/src/Spa/.env.local

# V2 Functions
HealthAggregatorV2/src/Functions/bin/
HealthAggregatorV2/src/Functions/obj/

# V2 User settings
HealthAggregatorV2/**/*.user
HealthAggregatorV2/**/local.settings.json
```

---

## Deployment Strategy

### Azure Resources

**V1 Resources** (Keep running):
- Azure Functions: `healthaggregator-functions-v1`
- Storage: (existing)

**V2 Resources** (Create new):
- Azure App Service: `healthaggregator-api-v2` (or use deployment slots)
- Azure Static Web Apps: `healthaggregator-spa-v2`
- Azure Functions: `healthaggregator-functions-v2` (or use deployment slots)

### Deployment Slots Alternative

Instead of creating entirely new resources, use **deployment slots**:

- API: Same App Service, `staging` slot for V2
- Functions: Same Function App, `v2` slot

This saves costs and makes cutover easier.

---

## Implementation Order

Follow [IMPLEMENTATION-GUIDE.md](IMPLEMENTATION-GUIDE.md) but with V2 paths:

1. Create `HealthAggregatorV2/` folder
2. Build Domain → Infrastructure → API → SPA → Functions
3. Build tests
4. Test integration
5. Deploy V2 alongside V1
6. Gradual cutover
7. Decommission V1 when confident

---

## Quick Reference Commands

### Build All V2 Projects
```bash
cd HealthAggregatorV2
dotnet build
```

### Run All V2 Tests
```bash
cd HealthAggregatorV2
dotnet test
```

### Create Migration
```bash
cd HealthAggregatorV2/src/Api
dotnet ef migrations add MigrationName
```

### Apply Migration
```bash
cd HealthAggregatorV2/src/Api
dotnet ef database update
```

---

## Summary

**Key Points:**
- ✅ All V2 code in `HealthAggregatorV2/` folder
- ✅ Clean project names: `Api`, `Domain`, `Infrastructure`, `Functions`, `Spa`
- ✅ Namespaces: `HealthAggregatorV2.{ProjectName}`
- ✅ V1 code in `HealthAggregatorApi/` remains untouched
- ✅ Self-contained V2 solution file
- ✅ Can run V1 and V2 simultaneously
- ✅ Professional, production-ready naming

**Golden Rule:** Never modify `HealthAggregatorApi/` - it's V1 and stays as-is!
