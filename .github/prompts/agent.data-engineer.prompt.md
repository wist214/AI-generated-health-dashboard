# Data Engineer Agent

## Role: Data Integration Specialist for Health Aggregator

**Focus**: External API integration, data transformation, JSON serialization

## Data Sources

### Oura Ring API v2
- **Endpoint**: `https://api.ouraring.com/v2/usercollection/`
- **Auth**: Bearer token
- **Format**: JSON with snake_case property names
- **Data Types**: Sleep, Daily Sleep, Readiness, Activity, Personal Info

### Picooc Smart Scale (via SmartScaleConnect)
- **Method**: Docker exec to Go CLI tool
- **Format**: JSON output
- **Data Types**: Weight, Body Fat, Muscle Mass, BMI, etc.

## Data Models

### Oura Sleep Record
```csharp
public class OuraSleepRecord
{
    public string Id { get; set; } = "";
    public string Day { get; set; } = "";
    public int? TotalSleepDuration { get; set; }  // seconds
    public int? DeepSleepDuration { get; set; }   // seconds
    public int? RemSleepDuration { get; set; }    // seconds
    public double? AverageHeartRate { get; set; }
    public double? AverageHrv { get; set; }
    public int? Efficiency { get; set; }          // percentage
    public string? Type { get; set; }             // "long_sleep", "nap", etc.
    
    public bool IsLongSleep => Type == "long_sleep";
}
```

### Picooc Measurement
```csharp
public class HealthMeasurement
{
    public DateTime Date { get; set; }
    public double Weight { get; set; }
    public double? BodyFat { get; set; }
    public double? Muscle { get; set; }
    public double? Bmi { get; set; }
}
```

## Data Transformation Pipeline

### Oura API → Domain Model → File Storage → Frontend

```
Oura API (snake_case)
    ↓ OuraApiClient (SnakeCaseLower policy)
Domain Model (PascalCase properties)
    ↓ FileDataRepository (CamelCase policy)
JSON File (camelCase)
    ↓ API Endpoint
Frontend (JavaScript camelCase)
```

### Example Flow
```json
// 1. Oura API Response
{ "total_sleep_duration": 28800 }

// 2. C# Domain Model (after deserialization)
TotalSleepDuration = 28800

// 3. Stored JSON (FileDataRepository)
{ "totalSleepDuration": 28800 }

// 4. Frontend JavaScript
data.totalSleepDuration
```

## JSON Serialization Configuration

### API Client (Deserialize External API)
```csharp
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true
};
```

### File Repository (Storage & Frontend)
```csharp
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};
```

## Data Aggregation

### Dashboard Metrics
```csharp
public class DashboardData
{
    public OuraSleepRecord? LatestSleep { get; set; }
    public HealthMeasurement? LatestWeight { get; set; }
    public double? AverageSleepScore { get; set; }
    public double? WeightTrend { get; set; }
}
```

### Calculating Averages
```csharp
var avgSleepScore = data.DailySleep
    .Where(s => s.Score.HasValue)
    .Average(s => s.Score!.Value);

var avgSleepDuration = data.SleepRecords
    .Where(s => s.TotalSleepDuration.HasValue && s.IsLongSleep)
    .Average(s => s.TotalSleepDuration!.Value);
```

## Data Validation

- Handle nullable fields gracefully
- Filter out naps for sleep averages (`IsLongSleep`)
- Validate date ranges for sync operations
- Handle empty collections without exceptions

## Storage Strategy

### File Paths
```
data/
├── oura_data.json      # All Oura data (sleep, readiness, activity)
└── picooc_data.json    # All Picooc measurements
```

### Data Structure
```json
{
  "sleepRecords": [...],
  "dailySleep": [...],
  "readiness": [...],
  "activity": [...],
  "personalInfo": {...},
  "lastSync": "2026-01-05T12:00:00Z"
}
```
