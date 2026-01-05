# Health Aggregator

A personal health dashboard that consolidates data from Oura Ring and Picooc smart scales into a unified interface.

🌐 **Live Dashboard**: [https://ambitious-flower-061bc3203.2.azurestaticapps.net](https://ambitious-flower-061bc3203.2.azurestaticapps.net)

## Features

- 📊 **Unified Dashboard** - View all health metrics at a glance
- 💍 **Oura Ring Integration** - Sleep scores, readiness, activity, HRV, heart rate
- ⚖️ **Weight Tracking** - Weight, body fat, muscle mass, BMI from Picooc scales
- 📈 **Trends & Charts** - Interactive charts with customizable time ranges
- 💡 **Smart Insights** - Personalized health recommendations
- 🔄 **Auto-Sync** - Hourly automatic data synchronization
- 🌙 **Dark Theme** - Modern, responsive design

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Azure Static Web Apps                     │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐         ┌─────────────────────────┐   │
│  │  Static Files   │         │   Azure Functions API    │   │
│  │  (Dashboard UI) │ ──────► │  (.NET 8 Isolated)      │   │
│  └─────────────────┘         └──────────┬──────────────┘   │
│                                         │                   │
│                              ┌──────────┴──────────┐       │
│                              ▼                     ▼       │
│                    ┌─────────────────┐   ┌──────────────┐  │
│                    │    Oura API     │   │  Picooc API  │  │
│                    └─────────────────┘   └──────────────┘  │
│                                                             │
│                         ┌─────────────────┐                │
│                         │  Azure Blob     │                │
│                         │  Storage        │                │
│                         └─────────────────┘                │
└─────────────────────────────────────────────────────────────┘
```

## Tech Stack

- **Frontend**: Vanilla JavaScript, Chart.js, CSS3
- **Backend**: .NET 8 Azure Functions (isolated worker)
- **Storage**: Azure Blob Storage
- **Hosting**: Azure Static Web Apps
- **CI/CD**: GitHub Actions

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/oura/data` | GET | Get all Oura data |
| `/api/oura/sync` | POST | Trigger Oura sync |
| `/api/oura/stats` | GET | Get sleep/readiness/activity statistics |
| `/api/oura/sleep/{id}` | GET | Get detailed sleep record |
| `/api/picooc/data` | GET | Get all weight measurements |
| `/api/picooc/sync` | POST | Trigger weight sync |
| `/api/dashboard` | GET | Get combined dashboard data |

## Local Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Node.js](https://nodejs.org/) (for serving static files locally)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/wist214/AI-generated-health-dashboard.git
   cd AI-generated-health-dashboard
   ```

2. **Configure credentials** in `HealthAggregatorApi/local.settings.json`:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "Oura:AccessToken": "YOUR_OURA_TOKEN",
       "Picooc:Email": "YOUR_EMAIL",
       "Picooc:Password": "YOUR_PASSWORD"
     }
   }
   ```

3. **Start Azure Functions**
   ```powershell
   cd HealthAggregatorApi
   func start
   ```

4. **Serve the dashboard** (in another terminal)
   ```powershell
   cd HealthAggregatorApi
   npx serve dashboard -l 4280 --cors
   ```

5. Open http://localhost:4280

## Deployment

The project auto-deploys to Azure Static Web Apps via GitHub Actions on every push to `main`.

### Manual Deployment Setup

1. Create an Azure Static Web App in the Azure Portal
2. Get the deployment token from Azure Portal → Static Web App → Manage deployment token
3. Add the token as a GitHub secret named `AZURE_STATIC_WEB_APPS_API_TOKEN`
4. Configure application settings in Azure:
   - `Oura__AccessToken`
   - `Picooc__Email`
   - `Picooc__Password`

## Getting API Credentials

### Oura Access Token
1. Go to [Oura Cloud](https://cloud.ouraring.com/oauth/applications)
2. Create a Personal Access Token
3. Copy the token

### Picooc Credentials
Use your Picooc mobile app login credentials (email + password).

## Cost

Running on Azure Free Tier:
- **Static Web Apps**: Free
- **Functions**: Free (up to 1M executions/month)
- **Blob Storage**: ~$0.01/month

**Total: ~$0.01/month**

## License

MIT
