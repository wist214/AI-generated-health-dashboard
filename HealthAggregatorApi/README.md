# Health Aggregator - Azure Functions API

This is the Azure Functions version of Health Aggregator, designed for deployment to Azure Static Web Apps with managed functions.

## Architecture

```
HealthAggregatorApi/
├── Functions/                  # Azure Functions (Timer + HTTP triggers)
│   ├── SyncTimerFunction.cs   # Hourly auto-sync (runs at :00 every hour)
│   ├── OuraFunctions.cs       # Oura Ring API endpoints
│   ├── PicoocFunctions.cs     # Picooc Scale API endpoints
│   └── DashboardFunctions.cs  # Combined dashboard endpoints
├── Core/                       # Business logic
│   ├── Interfaces/            # Service contracts
│   ├── Models/                # Domain models (Oura + Picooc)
│   └── Services/              # Business logic implementation
├── Infrastructure/             # External integrations
│   ├── ExternalApis/          # Oura API + Picooc API clients
│   └── Persistence/           # Azure Blob Storage repository
├── dashboard/                  # Static frontend files
│   ├── index.html             # Dashboard UI
│   ├── css/                   # Stylesheets
│   └── staticwebapp.config.json
├── Program.cs                  # DI configuration
├── local.settings.json         # Local development settings
└── swa-cli.config.json        # SWA CLI configuration
```

## API Endpoints

### Oura Ring
- `GET /api/oura/data` - Get all Oura data
- `GET /api/oura/stats` - Get 30-day average statistics
- `GET /api/oura/status` - Check configuration status
- `GET /api/oura/latest` - Get latest scores
- `GET /api/oura/sleep` - Get sleep records
- `GET /api/oura/sleep/{id}` - Get detailed sleep record
- `GET /api/oura/readiness` - Get readiness records
- `GET /api/oura/activity` - Get activity records
- `POST /api/oura/sync` - Trigger manual sync

### Picooc Scale
- `GET /api/picooc/data` - Get all weight measurements
- `GET /api/picooc/stats` - Get weight/body fat statistics
- `GET /api/picooc/status` - Check configuration status
- `GET /api/picooc/latest` - Get latest measurement
- `POST /api/picooc/sync` - Trigger manual sync

### Dashboard
- `GET /api/dashboard` - Combined Oura + Picooc data
- `GET /api/dashboard/metrics` - Latest metrics summary

### Timer
- `SyncTimer` - Runs hourly at minute 0 (automatic sync)

## Local Development

### Prerequisites
1. [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
2. [Azurite](https://docs.microsoft.com/azure/storage/common/storage-use-azurite) or Azure Storage Emulator
3. .NET 8.0 SDK

### Setup

1. **Configure credentials** in `local.settings.json`:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "Oura:AccessToken": "YOUR_OURA_TOKEN",
    "Picooc:Email": "YOUR_PICOOC_EMAIL",
    "Picooc:Password": "YOUR_PICOOC_PASSWORD"
  }
}
```

2. **Start Azurite** (storage emulator):
```powershell
npx azurite --silent --blobPort 10000
```

3. **Run the functions**:
```powershell
func start
```

4. **Test endpoints**:
```powershell
# Check status
Invoke-RestMethod http://localhost:7071/api/oura/status
Invoke-RestMethod http://localhost:7071/api/picooc/status

# Sync data
Invoke-RestMethod -Method POST http://localhost:7071/api/oura/sync
Invoke-RestMethod -Method POST http://localhost:7071/api/picooc/sync

# Get dashboard metrics
Invoke-RestMethod http://localhost:7071/api/dashboard/metrics
```

### Full Local Testing with Dashboard

```powershell
# Install SWA CLI globally
npm install -g @azure/static-web-apps-cli

# Start everything (functions + static files)
swa start dashboard --api-location . --api-port 7071
```

## Azure Deployment

### Option 1: Azure Portal (Simplest)

1. **Create Azure Static Web App**:
   - Go to [Azure Portal](https://portal.azure.com)
   - Create Resource → Static Web App
   - Name: `health-aggregator`
   - Region: Pick closest
   - Deployment: GitHub (connect your repo)
   - Build preset: Custom
   - App location: `/HealthAggregatorApi/dashboard`
   - API location: `/HealthAggregatorApi`
   - Output location: `/HealthAggregatorApi/dashboard`

2. **Add Application Settings** (Environment Variables):
   - Settings → Configuration → Add:
     - `Oura__AccessToken`: Your Oura token
     - `Picooc__Email`: Your Picooc email
     - `Picooc__Password`: Your Picooc password
     - `AzureWebJobsStorage`: Your storage connection string

3. **Create Storage Account** (for data persistence):
   - Create Resource → Storage Account
   - Name: `healthaggregatordata` (must be unique)
   - Performance: Standard
   - Redundancy: LRS (cheapest)
   - Copy connection string to Static Web App settings

### Option 2: Azure CLI

```bash
# Login
az login

# Create resource group
az group create --name health-aggregator-rg --location westeurope

# Create storage account
az storage account create \
  --name healthaggregatordata \
  --resource-group health-aggregator-rg \
  --sku Standard_LRS

# Get storage connection string
az storage account show-connection-string \
  --name healthaggregatordata \
  --resource-group health-aggregator-rg

# Create Static Web App (requires GitHub repo)
az staticwebapp create \
  --name health-aggregator \
  --resource-group health-aggregator-rg \
  --source https://github.com/YOUR_USERNAME/HealthAggregator \
  --branch main \
  --app-location "/HealthAggregatorApi/dashboard" \
  --api-location "/HealthAggregatorApi" \
  --output-location "/HealthAggregatorApi/dashboard"
```

## Estimated Azure Costs

| Service | Usage | Cost |
|---------|-------|------|
| Static Web Apps | Free tier | **$0.00/month** |
| Blob Storage | ~10MB data | **~$0.01/month** |
| Functions Runtime | ~744 executions/month (hourly) | **$0.00** (included) |
| **Total** | | **~$0.01/month** |

## Getting API Tokens

### Oura Access Token
1. Go to [Oura Cloud](https://cloud.ouraring.com/oauth/applications)
2. Create a Personal Access Token
3. Copy and save the token

### Picooc Credentials
Use your Picooc app login credentials (email and password).

## Troubleshooting

### Timer not running
- Check Azure Functions logs in Portal → Function App → Functions → SyncTimer → Monitor
- Timer CRON: `0 0 * * * *` = minute 0 of every hour

### Blob storage errors
- Ensure storage account exists and connection string is correct
- Container `health-data` is auto-created on first use

### API returns empty data
- Run manual sync first: `POST /api/oura/sync` and `POST /api/picooc/sync`
- Check if credentials are configured correctly

### CORS issues
- Local: Handled by `local.settings.json` Host.CORS
- Azure: SWA handles CORS automatically for managed functions
