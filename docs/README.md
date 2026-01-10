# Health Aggregator - Documentation Index

## 🎯 Start Here

If you're beginning implementation, read these documents in order:

1. **[V2-STRUCTURE.md](V2-STRUCTURE.md)** ⭐ **READ THIS FIRST**
   - Complete folder structure overview
   - V1 vs V2 comparison
   - Clean naming conventions
   - Project setup instructions

2. **[QUICK-START.md](QUICK-START.md)** ⭐ Quick reference
   - Implementation phases summary
   - Quick commands
   - Verification checklists
   - Common issues & solutions

3. **[IMPLEMENTATION-GUIDE.md](IMPLEMENTATION-GUIDE.md)** ⭐ Detailed guide
   - Step-by-step phase-by-phase instructions
   - Complete code examples
   - Build verification steps
   - Troubleshooting

4. **[plans/00-PROJECT-LOCATIONS.md](plans/00-PROJECT-LOCATIONS.md)** Reference
   - Detailed location strategy
   - Database strategies
   - Migration workflow
   - Deployment guidance

---

## 📋 Detailed Implementation Plans

These provide comprehensive technical details for each component:

| Plan | Component | Document | When to Read |
|------|-----------|----------|--------------|
| **Plan 01** | Backend API | [01-backend-api-implementation.md](plans/01-backend-api-implementation.md) | Phase 3 - When building API |
| **Plan 02** | Database/EF Core | [02-database-ef-core-implementation.md](plans/02-database-ef-core-implementation.md) | Phase 1 & 2 - Foundation |
| **Plan 03** | React SPA | [03-react-spa-implementation.md](plans/03-react-spa-implementation.md) | Phase 4 - When building UI |
| **Plan 04** | Azure Functions | [04-azure-functions-sync-implementation.md](plans/04-azure-functions-sync-implementation.md) | Phase 5 - Background jobs |
| **Plan 05** | Integration | *(SKIPPED)* | Future - Plugin architecture |
| **Plan 06** | Testing | [06-testing-strategy.md](plans/06-testing-strategy.md) | Phase 6 - Testing setup |

---

## 🏗️ Project Structure Overview

```
HealthAggregator/                      # Git repository root
│
├── HealthAggregatorApi/               # ❌ V1 - DO NOT MODIFY
│   └── (existing working system)
│
├── HealthAggregatorV2/                # ✅ V2 - Your implementation
│   ├── HealthAggregatorV2.sln
│   ├── src/
│   │   ├── Api/                      # .NET 8 Minimal API
│   │   ├── Domain/                   # Entity models
│   │   ├── Infrastructure/           # EF Core, repositories
│   │   ├── Functions/                # Azure Functions sync
│   │   └── Spa/                      # React SPA
│   └── tests/
│       ├── Api.Tests/
│       ├── Functions.Tests/
│       └── Spa.Tests/
│
└── docs/                              # ℹ️ This folder
    ├── README.md                      # This file
    ├── V2-STRUCTURE.md                # V2 structure details
    ├── QUICK-START.md                 # Quick reference
    ├── IMPLEMENTATION-GUIDE.md        # Detailed guide
    ├── MIGRATION_PLAN.md              # Original migration plan
    └── plans/                         # Detailed component plans
```

---

## 🚀 Quick Start Commands

### Create V2 Structure
```bash
mkdir HealthAggregatorV2
cd HealthAggregatorV2
mkdir src tests
dotnet new sln -n HealthAggregatorV2
```

### Run V2 System
```bash
# Terminal 1: API
cd HealthAggregatorV2/src/Api && dotnet run

# Terminal 2: SPA
cd HealthAggregatorV2/src/Spa && npm run dev

# Terminal 3: Functions
cd HealthAggregatorV2/src/Functions && func start
```

---

## 📊 Implementation Phases

| Phase | Duration | Component | Key Output |
|-------|----------|-----------|------------|
| **Phase 0** | 5 min | Preparation | Folder structure created |
| **Phase 1** | 1-2 hrs | Domain Layer | Entity models compile |
| **Phase 2** | 2-3 hrs | Infrastructure | EF Core + repositories compile |
| **Phase 3** | 3-4 hrs | API | API runs with Swagger |
| **Phase 4** | 4-6 hrs | React SPA | SPA displays dashboard |
| **Phase 5** | 3-4 hrs | Functions | Background sync works |
| **Phase 6** | 2-3 hrs | Testing | Tests pass |
| **Phase 7** | 2-3 hrs | Integration | Full system works |
| **TOTAL** | **18-28 hrs** | | Production-ready V2 |

---

## 🎯 Key Decisions Made

### Architecture
- ✅ .NET 8 Minimal API (not Controllers)
- ✅ Entity Framework Core 8
- ✅ React 18 with TypeScript
- ✅ Azure Functions .NET 8 Isolated Worker
- ✅ Playwright for E2E testing

### Data
- ✅ `double` type for metrics (not `decimal`)
- ✅ Hard delete (not soft delete)
- ✅ No authentication (public API)
- ✅ No API versioning initially (KISS)

### UI
- ✅ Dark mode theme
- ✅ Pixel-perfect design match with existing dashboard
- ✅ Chart.js (not Recharts)
- ✅ Fluent UI React v9 (heavily customized)

### Project Structure
- ✅ **HealthAggregatorV2 folder** - Clean V2 isolation
- ✅ **Clean project names** - Api, Domain, Infrastructure, Functions, Spa
- ✅ **No "New" prefix** - Version separation via folder
- ❌ **Skip Plan 05** - Integration architecture (future)

---

## 🔗 External Resources

### Microsoft Documentation
- [.NET 8 Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [EF Core 8](https://learn.microsoft.com/en-us/ef/core/)
- [Azure Functions .NET Isolated](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
- [React with TypeScript](https://react.dev/learn/typescript)

### Libraries & Frameworks
- [Fluent UI React v9](https://react.fluentui.dev/)
- [TanStack Query (React Query)](https://tanstack.com/query/latest)
- [Chart.js](https://www.chartjs.org/)
- [Playwright](https://playwright.dev/)
- [Vite](https://vitejs.dev/)

---

## 🆘 Getting Help

### During Implementation

1. **Build fails?** Check [IMPLEMENTATION-GUIDE.md](IMPLEMENTATION-GUIDE.md) Troubleshooting section
2. **Folder confusion?** Read [V2-STRUCTURE.md](V2-STRUCTURE.md)
3. **Technical details?** Refer to specific plan files
4. **Quick commands?** See [QUICK-START.md](QUICK-START.md)

### Common Issues

- **Port conflicts**: Change ports in launchSettings.json or vite.config.ts
- **CORS errors**: Update CORS policy in API Program.cs
- **Database connection**: Verify connection string in appsettings.json
- **Package restore**: Run `dotnet restore` and `npm install`

---

## ✅ Success Criteria

You're done when:
- ✅ V2 system runs at localhost:5173 (SPA) + localhost:5000 (API)
- ✅ V1 system still works at localhost:7071
- ✅ UI design is pixel-perfect match
- ✅ All tests pass
- ✅ Database migrations applied
- ✅ Both systems can run simultaneously

---

## 📝 Version History

- **V1**: Current Azure Functions + vanilla JS dashboard (`HealthAggregatorApi/`)
- **V2**: Refactored .NET 8 + React TypeScript (`HealthAggregatorV2/`) - In development

---

## 🎉 Ready to Start?

1. Read **[V2-STRUCTURE.md](V2-STRUCTURE.md)** to understand the folder structure
2. Open **[IMPLEMENTATION-GUIDE.md](IMPLEMENTATION-GUIDE.md)** and start with Phase 0
3. Follow the guide phase-by-phase
4. Verify at each step before proceeding
5. Keep V1 running as your safety net

**Good luck with the implementation! 🚀**
