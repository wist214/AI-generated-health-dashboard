# HealthAggregatorV2

This is the **V2 refactored system** - a modern rewrite of the Health Aggregator application.

## 📍 Location

This folder (`HealthAggregatorV2/`) contains the complete V2 implementation, separate from the V1 system in `HealthAggregatorApi/`.

## 🏗️ Structure

```
HealthAggregatorV2/
├── HealthAggregatorV2.sln          # Solution file
│
├── src/                             # Source code
│   ├── Api/                        # .NET 8 Minimal API
│   ├── Domain/                     # Entity models
│   ├── Infrastructure/             # EF Core, repositories
│   ├── Functions/                  # Azure Functions background jobs
│   └── Spa/                        # React TypeScript SPA
│
├── tests/                           # Test projects
│   ├── Api.Tests/                  # API unit tests
│   ├── Api.E2E/                    # API E2E tests
│   ├── Functions.Tests/            # Functions unit tests
│   ├── Spa.Tests/                  # React unit tests
│   └── Spa.E2E/                    # React E2E tests
│
└── README.md                        # This file
```

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- Azure SQL Database (or local SQL Server)

### Quick Start

1. **Build all projects:**
   ```bash
   dotnet build
   ```

2. **Run API:**
   ```bash
   cd src/Api
   dotnet run
   # Opens at: http://localhost:5000
   # Swagger: http://localhost:5000/swagger
   ```

3. **Run SPA:**
   ```bash
   cd src/Spa
   npm install
   npm run dev
   # Opens at: http://localhost:5173
   ```

4. **Run Functions (optional):**
   ```bash
   cd src/Functions
   func start
   # Opens at: http://localhost:7072
   ```

## 🧪 Testing

```bash
# Run all .NET tests
dotnet test

# Run React tests
cd src/Spa
npm test
```

## 📦 Projects

### Api (.NET 8 Minimal API)
- **Tech**: .NET 8, EF Core 8, Minimal APIs
- **Purpose**: REST API for health data
- **Port**: 5000
- **Endpoints**: `/api/metrics`, `/api/dashboard`, `/api/sources`

### Domain (Class Library)
- **Tech**: .NET 8
- **Purpose**: Shared entity models
- **Contents**: Source, Measurement, MetricType, DailySummary entities

### Infrastructure (Class Library)
- **Tech**: .NET 8, EF Core 8, Azure SQL
- **Purpose**: Data access layer
- **Contents**: HealthDbContext, repositories, EF configurations

### Functions (Azure Functions)
- **Tech**: .NET 8 Isolated Worker
- **Purpose**: Background data synchronization
- **Trigger**: Timer (every 30 minutes)
- **Sources**: Oura, Picooc, Cronometer

### Spa (React TypeScript)
- **Tech**: React 18, TypeScript, Vite, Fluent UI v9
- **Purpose**: Web dashboard UI
- **Port**: 5173
- **Design**: Dark theme, pixel-perfect match with V1

## 🗄️ Database

### Configuration

Update connection string in `src/Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "HealthDb": "Server=...;Database=HealthAggregator;..."
  }
}
```

### Migrations

```bash
# Create migration
cd src/Api
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update
```

## 🔧 Configuration

### API (src/Api/appsettings.json)
```json
{
  "ConnectionStrings": {
    "HealthDb": "..."
  },
  "AllowedOrigins": {
    "SPA": "http://localhost:5173"
  }
}
```

### Functions (src/Functions/local.settings.json)
```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings:HealthDb": "..."
  }
}
```

### SPA (src/Spa/.env)
```env
VITE_API_URL=http://localhost:5000/api
```

## 🌐 URLs

### Development
- **API**: http://localhost:5000
- **Swagger**: http://localhost:5000/swagger
- **SPA**: http://localhost:5173
- **Functions**: http://localhost:7072

### V1 (for comparison)
- **V1 Dashboard**: http://localhost:7071/dashboard/
- **V1 API**: http://localhost:7071/api/

## 📝 Documentation

For detailed implementation guidance, see:

- **[../docs/V2-STRUCTURE.md](../docs/V2-STRUCTURE.md)** - Complete structure details
- **[../docs/IMPLEMENTATION-GUIDE.md](../docs/IMPLEMENTATION-GUIDE.md)** - Step-by-step guide
- **[../docs/QUICK-START.md](../docs/QUICK-START.md)** - Quick reference

## 🔗 Key Technologies

- **.NET 8**: Latest .NET with Minimal APIs
- **EF Core 8**: Database access and migrations
- **React 18**: UI library with hooks
- **TypeScript**: Type-safe JavaScript
- **Vite**: Fast build tool and dev server
- **Fluent UI React v9**: Microsoft's design system
- **TanStack Query**: Server state management
- **Chart.js**: Data visualization
- **Playwright**: E2E testing
- **xUnit**: .NET unit testing
- **Vitest**: React unit testing

## 🎯 Design Goals

- **SOLID Principles**: Clean architecture
- **DRY**: No code duplication
- **KISS**: Simple, straightforward
- **Testability**: E2E and unit tests
- **Performance**: Fast, optimized
- **Maintainability**: Easy to understand and modify
- **Pixel-Perfect UI**: Exact match with V1 design

## 🚀 Deployment

### Azure Resources (Separate from V1)

- **API**: Azure App Service (new or deployment slot)
- **SPA**: Azure Static Web Apps
- **Functions**: Azure Functions (new or deployment slot)
- **Database**: Azure SQL (shared or separate)

### Build for Production

```bash
# API
cd src/Api
dotnet publish -c Release -o ./publish

# SPA
cd src/Spa
npm run build

# Functions
cd src/Functions
dotnet publish -c Release -o ./publish
```

## 🐛 Troubleshooting

### Build Issues
```bash
dotnet clean
dotnet restore
dotnet build
```

### Port Conflicts
- Change API port in `src/Api/Properties/launchSettings.json`
- Change SPA port in `src/Spa/vite.config.ts`
- Change Functions port in `src/Functions/host.json`

### Database Issues
- Verify connection string
- Check Azure SQL firewall rules
- Ensure migrations applied: `dotnet ef database update`

## 📊 Status

🚧 **In Development**

This is the V2 refactored system currently being built. The V1 system (`../HealthAggregatorApi/`) remains operational and should not be modified.

## 📞 Support

For issues or questions:
1. Check [../docs/](../docs/) folder for detailed guides
2. Review specific plan files for technical details
3. Compare with V1 implementation for reference

---

**Version**: 2.0.0-dev
**Status**: In Development
**V1 Location**: `../HealthAggregatorApi/`
