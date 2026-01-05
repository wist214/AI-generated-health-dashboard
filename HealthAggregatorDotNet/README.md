# Health Aggregator (.NET)

A .NET 8 implementation of the Health Aggregator web app for syncing and visualizing Picooc smart scale data.

## Prerequisites

- .NET 8 SDK
- Docker Desktop (for SmartScaleConnect)

## Configuration

Edit `appsettings.json` to configure your Picooc credentials:

```json
{
  "Picooc": {
    "Username": "your_email@example.com",
    "Password": "your_password",
    "User": ""
  }
}
```

## Running the App

```bash
cd HealthAggregatorDotNet
dotnet run
```

The app will start at **http://localhost:3001**

## Features

- 📊 Interactive charts with Chart.js
- 🔄 One-click sync with Picooc via Docker
- 💾 Local data caching
- 📱 Modern dark-themed responsive UI
- 📈 Statistics cards showing current, min, max, and average values

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/data` | GET | Get all cached health data |
| `/api/sync` | POST | Sync data from Picooc |
| `/api/latest` | GET | Get latest measurement |
| `/api/stats` | GET | Get statistics |
| `/api/status` | GET | Check configuration status |

## Project Structure

```
HealthAggregatorDotNet/
├── Program.cs              # Main entry point with API endpoints
├── Models/
│   └── HealthMeasurement.cs # Data models
├── Services/
│   └── PicoocSyncService.cs # Picooc sync service
├── wwwroot/
│   └── index.html          # Frontend UI
├── data/                   # Cached data (auto-created)
├── appsettings.json        # Configuration
└── HealthAggregator.csproj # Project file
```
