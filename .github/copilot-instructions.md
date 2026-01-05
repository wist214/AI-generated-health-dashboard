# Health Aggregator - Copilot Instructions

## Related Instructions

**Reference these files for detailed guidance:**
- **AI Persona**: See `.github/prompt-snippets/copilot-personality.md` for AI assistant behavior
- **Coding Standards**: See `.github/prompt-snippets/coding-standards.md` for C# and JSON rules
- **Software Engineering**: See `.github/prompts/agent.software-engineer.prompt.md` for implementation patterns
- **Data Engineering**: See `.github/prompts/agent.data-engineer.prompt.md` for API integration patterns
- **Task Management**: See `.github/prompts/task-manager.prompt.md` for development workflows
- **Testing**: See `.github/prompts/unit-tester.prompt.md` for test strategies

## Project Overview

**Health Aggregator** is a personal health data dashboard that consolidates data from multiple wearable devices and smart scales into a unified interface. The application aggregates health metrics from Oura Ring (sleep, readiness, activity) and Picooc smart scales (weight, body composition) to provide comprehensive health insights.

### Key Features
- **Oura Ring Integration**: Sleep tracking, readiness scores, activity metrics, heart rate, HRV
- **Picooc Smart Scale Integration**: Weight, body fat, muscle mass, BMI via SmartScaleConnect
- **Unified Dashboard**: Single-page web interface with charts, trends, and historical data
- **Data Persistence**: File-based JSON storage for offline access and historical tracking

## Technology Stack

### Backend (.NET)
- **.NET 8.0 + C# 12**: ASP.NET Core Minimal API
- **System.Text.Json**: JSON serialization with snake_case/camelCase handling
- **File-based Storage**: JSON files for data persistence (no database)
- **HTTP Clients**: HttpClient for Oura API, Docker exec for SmartScaleConnect

### Frontend
- **Vanilla JavaScript**: No framework, single HTML file
- **Chart.js**: Data visualization for trends and history
- **CSS**: Custom styling with responsive design

### External Integrations
- **Oura API v2**: REST API with Bearer token authentication
- **SmartScaleConnect**: Go-based CLI tool running in Docker for Picooc scale data

## Project Structure

```text
HealthAggregatorDotNet/
├── Api/
│   ├── DTOs/                    # API response DTOs
│   │   ├── OuraDtos.cs         # Oura API response wrappers
│   │   └── PicoocDtos.cs       # Picooc sync response DTOs
│   └── Endpoints/
│       └── EndpointExtensions.cs  # Minimal API endpoint definitions
├── Core/
│   ├── DTOs/
│   │   └── DashboardDtos.cs    # Dashboard aggregation DTOs
│   ├── Interfaces/              # Service and repository interfaces
│   │   ├── IDashboardService.cs
│   │   ├── IDataRepository.cs
│   │   ├── IOuraApiClient.cs
│   │   ├── IOuraDataService.cs
│   │   ├── IPicoocDataService.cs
│   │   └── IPicoocSyncClient.cs
│   ├── Models/
│   │   ├── Oura/               # Oura domain models
│   │   │   ├── OuraData.cs
│   │   │   ├── SleepModels.cs
│   │   │   ├── ReadinessModels.cs
│   │   │   └── ActivityModels.cs
│   │   └── Picooc/
│   │       └── HealthMeasurement.cs
│   └── Services/                # Business logic
│       ├── DashboardService.cs
│       ├── OuraDataService.cs
│       └── PicoocDataService.cs
├── Infrastructure/
│   ├── ExternalApis/
│   │   ├── OuraApiClient.cs    # Oura REST API client
│   │   └── PicoocSyncClient.cs # SmartScaleConnect Docker integration
│   └── Persistence/
│       └── FileDataRepository.cs  # Generic JSON file storage
├── wwwroot/
│   └── index.html              # Single-page dashboard UI
├── Program.cs                   # Application entry point, DI setup
└── appsettings.json            # Configuration (API keys, paths)
```

## Architecture Patterns

### Clean Architecture (Simplified)
- **Api/**: HTTP endpoints, request/response DTOs
- **Core/**: Business logic, domain models, service interfaces
- **Infrastructure/**: External API clients, file persistence

### Dependency Flow
`Api → Core ← Infrastructure`

### JSON Serialization Strategy
- **Oura API Client**: Uses `SnakeCaseLower` naming policy for deserializing Oura API responses
- **File Repository**: Uses `CamelCase` naming policy for storage and frontend consumption
- **Important**: Do NOT use `[JsonPropertyName]` attributes on models - let naming policies handle conversion

## Key Implementation Details

### Oura API Integration
```csharp
// OuraApiClient uses SnakeCaseLower for Oura API v2
_jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true
};
```

### File Repository
```csharp
// FileDataRepository uses CamelCase for frontend compatibility
_jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};
```

### Frontend Data Access
JavaScript expects camelCase properties:
```javascript
record.totalSleepDuration  // NOT total_sleep_duration
record.averageHeartRate    // NOT average_heart_rate
record.deepSleepDuration   // NOT deep_sleep_duration
```

## Developer Commands

```powershell
# Build and run
cd HealthAggregatorDotNet
dotnet build
dotnet run

# Application runs on http://localhost:3001

# Test API endpoints
Invoke-RestMethod -Uri "http://localhost:3001/api/oura/sync" -Method POST
Invoke-RestMethod -Uri "http://localhost:3001/api/oura/data" -Method GET
Invoke-RestMethod -Uri "http://localhost:3001/api/picooc/sync" -Method POST
```

## Configuration

### appsettings.json
```json
{
  "Oura": {
    "AccessToken": "your-oura-token"
  },
  "Picooc": {
    "Email": "your-email",
    "Password": "your-password"
  }
}
```

## Quality Standards

- **Zero Warnings**: Enable nullable reference types, fix all warnings
- **Consistent JSON**: Always use naming policies, never mix snake_case and camelCase
- **Error Handling**: Graceful fallbacks when external APIs are unavailable
- **Logging**: Use ILogger<T> for diagnostics

## Common Issues & Solutions

### JSON Property Names
- **Problem**: Data shows in API but not in frontend
- **Solution**: Ensure models don't have `[JsonPropertyName]` attributes; let `CamelCase` policy in FileDataRepository handle serialization

### Oura API Deserialization
- **Problem**: Oura API returns snake_case, C# uses PascalCase
- **Solution**: OuraApiClient uses `SnakeCaseLower` policy to automatically convert

### Docker/SmartScaleConnect
- **Problem**: Picooc sync fails
- **Solution**: Ensure Docker is running and SmartScaleConnect container is available
