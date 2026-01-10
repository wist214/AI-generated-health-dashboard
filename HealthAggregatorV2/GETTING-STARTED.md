# Getting Started with HealthAggregatorV2

## 🚀 Quick Setup (5 Minutes)

### Step 1: Create Projects

```bash
# You're already in HealthAggregatorV2 folder
pwd
# Should show: d:\Work\My\HealthAggregator\HealthAggregatorV2

# Create solution
dotnet new sln -n HealthAggregatorV2

# Create Domain project
cd src
dotnet new classlib -n Domain -f net8.0

# Create Infrastructure project
dotnet new classlib -n Infrastructure -f net8.0

# Create API project
dotnet new web -n Api -f net8.0

# Back to root
cd ..

# Add projects to solution
dotnet sln add src/Domain/Domain.csproj
dotnet sln add src/Infrastructure/Infrastructure.csproj
dotnet sln add src/Api/Api.csproj
```

### Step 2: Add Project References

```bash
# Infrastructure → Domain
cd src/Infrastructure
dotnet add reference ../Domain/Domain.csproj

# Api → Domain + Infrastructure
cd ../Api
dotnet add reference ../Domain/Domain.csproj
dotnet add reference ../Infrastructure/Infrastructure.csproj

cd ../..
```

### Step 3: Verify Build

```bash
dotnet build
```

✅ Should see: "Build succeeded"

---

## 📚 Documentation

| Document | Purpose | When to Read |
|----------|---------|--------------|
| [README.md](README.md) | Project overview | Start here |
| [../docs/V2-STRUCTURE.md](../docs/V2-STRUCTURE.md) | Complete structure guide | Before implementation |
| [../docs/QUICK-START.md](../docs/QUICK-START.md) | Quick reference | During implementation |
| [../docs/IMPLEMENTATION-GUIDE.md](../docs/IMPLEMENTATION-GUIDE.md) | Detailed step-by-step | Follow for implementation |

---

## 📁 Your Structure

```
HealthAggregatorV2/
├── HealthAggregatorV2.sln        # Solution file
├── GETTING-STARTED.md             # This file
├── README.md                      # Project overview
│
├── src/
│   ├── Domain/                    # Create first (Phase 1)
│   ├── Infrastructure/            # Create second (Phase 1)
│   ├── Api/                       # Create third (Phase 2)
│   ├── Functions/                 # Create later (Phase 4)
│   └── Spa/                       # Create later (Phase 3)
│
└── tests/
    └── (create test projects in Phase 5)
```

---

## ⚡ Quick Commands

### Build All
```bash
dotnet build
```

### Run API (once created)
```bash
cd src/Api
dotnet run
```

### Run Tests (once created)
```bash
dotnet test
```

### Create Migration (once API + Infrastructure ready)
```bash
cd src/Api
dotnet ef migrations add InitialCreate
```

---

## 🎯 Implementation Order

1. ✅ **Phase 0**: Folder structure (you're here!)
2. ⏭️ **Phase 1**: Domain + Infrastructure (2-3 hours)
3. ⏭️ **Phase 2**: API (3-4 hours)
4. ⏭️ **Phase 3**: SPA (4-6 hours)
5. ⏭️ **Phase 4**: Functions (3-4 hours)
6. ⏭️ **Phase 5**: Testing (2-3 hours)

**Follow**: [../docs/IMPLEMENTATION-GUIDE.md](../docs/IMPLEMENTATION-GUIDE.md) for detailed instructions

---

## 🆚 V1 vs V2

| Aspect | V1 (Old) | V2 (New - You) |
|--------|----------|----------------|
| Location | `../HealthAggregatorApi/` | `./` (this folder) |
| Framework | Azure Functions | .NET 8 Minimal API + Functions |
| UI | Vanilla JS | React + TypeScript |
| Status | ❌ Do not touch | ✅ Your implementation |
| URL | http://localhost:7071 | http://localhost:5000 |

---

## 🐛 Common Issues

### "dotnet command not found"
```bash
# Check .NET SDK installed
dotnet --version
# Should show: 8.0.x
```

### "Project not found"
```bash
# Ensure you're in HealthAggregatorV2 root
pwd
# Should end with: HealthAggregatorV2
```

### "Build failed"
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

---

## ✅ Checklist

After setup, verify:
- [ ] `HealthAggregatorV2.sln` exists
- [ ] `src/Domain/` folder exists with `Domain.csproj`
- [ ] `src/Infrastructure/` folder exists with `Infrastructure.csproj`
- [ ] `src/Api/` folder exists with `Api.csproj`
- [ ] `dotnet build` succeeds
- [ ] Project references work (Infrastructure → Domain, Api → Both)

---

## 📞 Next Steps

1. ✅ You've created the basic structure
2. ⏭️ Open [../docs/IMPLEMENTATION-GUIDE.md](../docs/IMPLEMENTATION-GUIDE.md)
3. ⏭️ Start with **Phase 1: Domain Layer**
4. ⏭️ Follow the guide phase by phase
5. ⏭️ Build and verify after each phase

**Happy coding! 🎉**
