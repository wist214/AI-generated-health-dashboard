# Health Aggregator - Complete Functionality Specification

## Overview

**Health Aggregator** is a personal health data dashboard that consolidates data from multiple wearable devices and nutrition tracking apps into a unified interface. The application is built as an Azure Functions API with a single-page web dashboard.

---

## Architecture

| Component | Technology |
|-----------|------------|
| **Backend** | .NET 8.0 Azure Functions (Isolated Worker) |
| **Frontend** | Single HTML page with vanilla JavaScript |
| **Charts** | Chart.js with date-fns adapter |
| **Storage** | Azure Blob Storage (JSON files) |
| **Data Sources** | Oura Ring API v2, Picooc Smart Scale, Cronometer |

---

## Data Sources & Integrations

### 1. Oura Ring (REST API v2)
- Sleep tracking (detailed sleep records & daily summaries)
- Readiness scores with contributors
- Daily activity tracking
- Stress levels
- Resilience metrics
- VO2 Max
- Cardiovascular age
- SpO2 (blood oxygen)
- Workouts
- Sleep time recommendations

### 2. Picooc Smart Scale
- Body composition measurements via native API client
- Weight, BMI, body fat, muscle mass, bone mass, body water, metabolic age, visceral fat, basal metabolism

### 3. Cronometer
- Daily nutrition tracking
- Individual food servings with detailed macros
- Exercise entries
- Biometric data
- Notes

---

## Pages / Tabs

The application has a **4-tab navigation** system:

| Tab | Icon | Description |
|-----|------|-------------|
| **Dashboard** | 📊 | Overview of all health metrics with trends and insights |
| **Weight** | ⚖️ | Picooc scale measurements and body composition |
| **Oura Ring** | 💍 | Sleep, readiness, activity, and advanced health metrics |
| **Food** | 🍎 | Nutrition tracking from Cronometer |

---

## Tab 1: Dashboard

### Header
- **Title**: "Health Aggregator"
- **Subtitle**: "Track your health data from multiple sources"
- **Settings button**: ⚙️ (opens settings modal)
- **Current date**: Full date display (e.g., "Friday, January 10, 2026")

### Metric Cards (4 cards in grid)

#### Weight Card (Picooc)
| Field | Format | Source |
|-------|--------|--------|
| Current Weight | `XX.X kg` | Latest measurement |
| Measurement Date | `M/D/YYYY` | Date of measurement |
| **Progress Grid**: | | |
| vs Previous | `↑/↓ X.X%` + absolute change | Comparison to previous measurement |
| vs ~1 Week | `↑/↓ X.X%` + absolute change | 5-14 days ago |
| vs ~1 Month | `↑/↓ X.X%` + absolute change | 21-45 days ago |

#### Body Fat Card (Picooc)
| Field | Format | Source |
|-------|--------|--------|
| Current Body Fat | `XX.X%` | Latest measurement |
| Measurement Date | `M/D/YYYY` | Date of measurement |
| Progress Grid | Same as Weight | |

#### Sleep Score Card (Oura)
| Field | Format | Source |
|-------|--------|--------|
| Sleep Score | `0-100` (integer) | Daily sleep summary |
| Date | `M/D/YYYY` | Day of sleep |
| Progress Grid | `↑/↓ X.X%` | vs Yesterday/Week/Month |

#### Readiness Card (Oura)
| Field | Format | Source |
|-------|--------|--------|
| Readiness Score | `0-100` (integer) | Daily readiness |
| Date | `M/D/YYYY` | Day |
| Progress Grid | `↑/↓ X.X%` | vs Yesterday/Week/Month |

### Quick Stats Row (4 stats)
| Stat | Icon | Format | Source |
|------|------|--------|--------|
| Steps Today | 🏃 | `X,XXX` | Oura activity |
| Sleep Duration | 🛏️ | `Xh Xm` | Oura sleep record (long_sleep only) |
| Avg Heart Rate | ❤️ | `XX bpm` | Oura sleep record |
| Calories | 🔥 | `XXXX / XXXX` (consumed / target) | Cronometer daily |

### Weekly Trends Chart
- **Type**: Line chart
- **Period**: Last 7 days
- **Datasets**:
  - Sleep Score (blue)
  - Readiness Score (green)  
  - Weight in kg (cyan)
- **Y-Axis**: 0-100 (Score / Weight)

### Insights Section
Dynamic insights based on data analysis:
- Weight change insights (progress messages)
- Sleep score evaluation (excellent/needs improvement)
- 7-day sleep average
- Readiness recommendations
- Step count feedback

---

## Tab 2: Weight (Picooc)

### Controls
- **Sync Button**: "🔄 Sync Data"
- **Status Message**: Connection status, record count, last sync time

### Current Stats Cards (4 cards)
| Card | Value Format | Details Format |
|------|--------------|----------------|
| Weight | `XX.X kg` | `Min: X \| Max: X \| Avg: X` |
| Body Fat | `XX.X%` | `Min: X \| Max: X \| Avg: X` |
| BMI | `XX.X` | `Min: X \| Max: X \| Avg: X` |
| Muscle Mass | `XX.X kg` | `Min: X \| Max: X \| Avg: X` |

### Weight Trends Chart
- **Type**: Multi-line chart
- **Toggle options**: Weight, Body Fat, BMI, Muscle
- **Time ranges**: 7 Days, 30 Days, 3 Months, 6 Months, 1 Year, All Time
- **Y-Axes**: 
  - Left: kg (Weight, Muscle)
  - Right: % / BMI

### Measurement History Table
| Column | Format |
|--------|--------|
| Date | `M/D/YYYY, HH:MM:SS AM/PM` |
| Weight (kg) | `XX.X` |
| Body Fat (%) | `XX.X` |
| BMI | `XX.X` |
| Muscle (kg) | `XX.X` |
| Water (%) | `XX.X` |
| Metabolic Age | Integer |

- **Pagination**: 15 records per page
- **Click action**: Opens Measurement Detail Modal

### Measurement Detail Modal
| Section | Fields |
|---------|--------|
| **Header** | Date with day of week |
| **Hero Metrics** | Weight (kg), Body Fat (%), BMI with status |
| **Body Composition Bars** | Muscle Mass, Body Fat, Body Water, Bone Mass |
| **Health Indicators** | Metabolic Age (years), Visceral Fat (with status), BMR (kcal/day) |

BMI Status Classifications:
- `< 18.5`: ⚠️ Underweight
- `18.5-25`: ✅ Normal
- `25-30`: ⚠️ Overweight
- `> 30`: 🔴 Obese

Body Fat Status Classifications:
- `< 14%`: 💪 Athletic
- `14-20%`: ✅ Fit
- `20-25%`: 👍 Average
- `> 25%`: ⚠️ Above Avg

Visceral Fat Status:
- `1-9`: ✅ Healthy
- `10-14`: ⚠️ High
- `> 14`: 🔴 Very High

---

## Tab 3: Oura Ring

### Controls
- **Sync Button**: "🔄 Sync Oura Data"
- **Status Message**: Connection status, record counts, last sync

### Primary Score Cards (4 cards)
| Card | Value | Details |
|------|-------|---------|
| Sleep Score | `0-100` | Min/Max/Avg stats |
| Readiness Score | `0-100` | Min/Max/Avg stats |
| Activity Score | `0-100` | Min/Max/Avg stats |
| Sleep Duration | `X.Xh` | Min/Max/Avg in hours |

### Advanced Health Metrics Section (Collapsible)
| Card | Value Format | Details |
|------|--------------|---------|
| 🧘 Daily Stress | `✨ Restored / 😊 Normal / 😰 Stressful` | Stress mins / Recovery mins |
| 💪 Resilience | `🌟 Exceptional / 💪 Strong / ✅ Solid / 👍 Adequate / ⚠️ Limited` | Sleep/Day recovery scores |
| 🫀 VO2 Max | `XX.X ml/kg/min` | Range (min-max) |
| ❤️ Cardio Age | `XX yrs` | Difference from actual age |

### Recovery & Vitals Section (Collapsible)
| Card | Value Format | Details |
|------|--------------|---------|
| 🩸 SpO2 | `XX.X%` | Avg / Breathing disturbance |
| 🛏️ Optimal Bedtime | `HH:MM PM - HH:MM PM` | Recommendation text |
| 🏋️ Workouts | `X this week` | Total kcal / Total km |

### Health Scores Chart
- **Type**: Multi-line chart
- **Toggle options**: Sleep Score, Readiness Score, Activity Score, Steps
- **Time ranges**: 7 Days, 30 Days, 3 Months, 6 Months, 1 Year, All Time
- **Y-Axes**:
  - Left: Score (0-100)
  - Right: Steps

### Sleep Duration Chart
- **Type**: Multi-line chart
- **Toggle options**: Total Sleep, Deep Sleep, REM Sleep, Avg HR
- **Data source**: Long sleep records only (excludes naps)
- **Y-Axes**:
  - Left: Hours
  - Right: BPM

### Sleep History Table
| Column | Format |
|--------|--------|
| Date | `YYYY-MM-DD` |
| Score | Badge with color (green/yellow/red) |
| Total Sleep | `Xh Xm` |
| Deep Sleep | `Xh Xm` |
| REM Sleep | `Xh Xm` |
| Avg HR | Integer |
| HRV | Integer |
| Efficiency | `XX%` |

- **Click action**: Opens Sleep Detail Modal

### Sleep Detail Modal
| Section | Fields |
|---------|--------|
| **Hero Metrics** | Sleep Score, Total Sleep, Restfulness |
| **Sleep Timing** | Bedtime → Wake Time, Efficiency |
| **Sleep Stages Bar** | Deep / Light / REM / Awake (% visual) |
| **Sleep Vitals** | Avg HR (+ Lowest), HRV (RMSSD), Breathing Rate, Sleep Latency |
| **Time in Bed Info** | Total time, Restless periods |
| **Contributors** | Deep Sleep, Efficiency, Latency, REM Sleep, Restfulness, Timing, Total Sleep |

### Activity History Table
| Column | Format |
|--------|--------|
| Date | `YYYY-MM-DD` |
| Score | Badge with color |
| Steps | `X,XXX` |
| Active Cal | Integer |
| Total Cal | Integer |
| Distance | `X.X km` |
| High Activity | `Xh Xm` |
| Med Activity | `Xh Xm` |

- **Click action**: Opens Activity Detail Modal

### Activity Detail Modal
| Section | Fields |
|---------|--------|
| **Hero Metrics** | Activity Score, Steps, Distance |
| **Calories** | Active Calories, Total Calories |
| **Activity Time Bar** | High / Medium / Low (% visual) |
| **Rest & Recovery** | Sedentary time, Resting time, Inactivity Alerts |

---

## Tab 4: Food (Cronometer)

### Controls
- **Sync Button**: "🔄 Sync Cronometer"
- **Status Message**: Connection status, record counts, last sync

### Today's Summary Cards (4 cards)
| Card | Value Format | Details |
|------|--------------|---------|
| Calories | `XXXX/XXXX` | X remaining/over |
| Protein | `XXXg/XXXg` | Xg remaining/over |
| Carbs | `XXXg/XXXg` | Xg remaining/over |
| Fat | `XXXg/XXXg` | Xg remaining/over |

### Calorie Trend Chart
- **Type**: Line chart with fill
- **Time ranges**: 7 Days, 30 Days, 3 Months, 6 Months, 1 Year, All Time
- **Color**: Orange

### Macro Breakdown Chart
- **Type**: Doughnut chart
- **Segments**: Protein (red), Carbs (yellow), Fat (green)
- **Legend**: Shows percentage and grams

### Key Nutrients Grid (8 progress bars)
| Nutrient | Target | Unit |
|----------|--------|------|
| Fiber | 30g | g |
| Sugar | 50g | g |
| Sodium | 2300mg | mg |
| Cholesterol | 300mg | mg |
| Vitamin D | 20µg | µg |
| Calcium | 1000mg | mg |
| Iron | 18mg | mg |
| Potassium | 3500mg | mg |

Progress bar shows:
- Current value / Target
- % fill (capped at 100%)
- ⚠️ indicator if exceeded

### Today's Food Log
- **Date Navigation**: ◀ Previous Day | Date Display | Next Day ▶
- **Date Display**: "Today", "Yesterday", or "Mon, Jan 10"

| Column | Format |
|--------|--------|
| Food | Food name |
| Category | Category badge |
| Amount | Serving description |
| Calories | Integer |
| Protein | `X.X` |
| Carbs | `X.X` |
| Fat | `X.X` |

- **Click action**: Opens Food Detail Modal

### Food Detail Modal
| Section | Fields |
|---------|--------|
| **Header** | Date |
| **Food Hero** | Food name |
| **Basic Info** | Category, Amount, Meal Group |
| **Nutrition Grid** | Calories, Protein, Carbs, Fat |
| **Additional Nutrients** | Fiber, Sugar, Sodium |

---

## Settings Modal

### Sections

#### Body Measurements
| Setting | Type | Range | Default |
|---------|------|-------|---------|
| Height | Number input | 100-250 cm | 175 |

#### Daily Nutrition Targets
| Setting | Type | Range | Default |
|---------|------|-------|---------|
| Calories | Number input | 1000-5000 kcal | 2000 |
| Protein | Number input | 30-300 g | 150 |
| Carbs | Number input | 50-500 g | 250 |
| Fat | Number input | 20-200 g | 65 |

#### Quick Presets
| Preset | Calories Formula | Protein | Fat | Carbs |
|--------|-----------------|---------|-----|-------|
| Maintenance | weight × 30 | weight × 1.8g | 25% calories | Remaining |
| Weight Loss | weight × 24 | weight × 2.2g | 25% calories | Remaining |
| Muscle Gain | weight × 35 | weight × 2.0g | 25% calories | Remaining |

---

## Data Models

### Picooc Health Measurement
```
- Date: DateTime
- Weight: double (kg)
- BMI: double
- BodyFat: double (%)
- BodyWater: double (%)
- BoneMass: double (kg)
- MetabolicAge: int (years)
- VisceralFat: int (level 1-59)
- BasalMetabolism: int (kcal/day)
- SkeletalMuscleMass: double (kg)
- Source: string
```

### Oura Sleep Record
```
- Id: string
- Day: string (YYYY-MM-DD)
- BedtimeStart: string (ISO 8601)
- BedtimeEnd: string (ISO 8601)
- AverageBreath: double (breaths/min)
- AverageHeartRate: double (bpm)
- AverageHrv: double (ms)
- AwakeTime: int (seconds)
- DeepSleepDuration: int (seconds)
- Efficiency: int (%)
- Latency: int (seconds)
- LightSleepDuration: int (seconds)
- LowestHeartRate: int (bpm)
- RemSleepDuration: int (seconds)
- RestlessPeriods: int
- TimeInBed: int (seconds)
- TotalSleepDuration: int (seconds)
- Type: string ("long_sleep" or "nap")
```

### Oura Daily Sleep Summary
```
- Id: string
- Day: string
- Score: int (0-100)
- Contributors:
  - DeepSleep: int
  - Efficiency: int
  - Latency: int
  - RemSleep: int
  - Restfulness: int
  - Timing: int
  - TotalSleep: int
```

### Oura Activity Record
```
- Id: string
- Day: string
- Score: int (0-100)
- ActiveCalories: int
- Steps: int
- TotalCalories: int
- EquivalentWalkingDistance: int (meters)
- HighActivityTime: int (seconds)
- MediumActivityTime: int (seconds)
- LowActivityTime: int (seconds)
- SedentaryTime: int (seconds)
- RestingTime: int (seconds)
- InactivityAlerts: int
```

### Oura Readiness Record
```
- Id: string
- Day: string
- Score: int (0-100)
- TemperatureDeviation: double
- TemperatureTrendDeviation: double
- Contributors:
  - ActivityBalance: int
  - BodyTemperature: int
  - HrvBalance: int
  - PreviousDayActivity: int
  - PreviousNight: int
  - RecoveryIndex: int
  - RestingHeartRate: int
  - SleepBalance: int
```

### Oura Stress Record
```
- Id: string
- Day: string
- StressHigh: int (seconds in high stress)
- RecoveryHigh: int (seconds in high recovery)
- DaySummary: string ("restored" | "normal" | "stressful")
```

### Oura Resilience Record
```
- Id: string
- Day: string
- Level: string ("limited" | "adequate" | "solid" | "strong" | "exceptional")
- Contributors:
  - SleepRecovery: int (0-100)
  - DaytimeRecovery: int (0-100)
  - Stress: int (0-100)
```

### Oura VO2 Max Record
```
- Id: string
- Day: string
- Timestamp: string
- Vo2Max: double (ml/kg/min)
```

### Oura Cardiovascular Age
```
- Day: string
- VascularAge: int (years)
```

### Oura SpO2 Record
```
- Id: string
- Day: string
- Spo2Percentage:
  - Average: double (%)
- BreathingDisturbanceIndex: double
```

### Oura Workout Record
```
- Id: string
- Day: string
- Activity: string (e.g., "running", "cycling")
- Calories: int
- Distance: int (meters)
- StartDatetime: string (ISO 8601)
- EndDatetime: string (ISO 8601)
- Intensity: string ("easy" | "moderate" | "hard")
- Label: string
- Source: string ("manual" | "autodetected")
```

### Oura Sleep Time Recommendation
```
- Id: string
- Day: string
- OptimalBedtime:
  - DayTz: int (timezone offset seconds)
  - StartOffset: int (seconds from midnight)
  - EndOffset: int (seconds from midnight)
- Recommendation: string ("improve_efficiency" | "earlier_bedtime" | etc.)
- Status: string ("not_enough_nights" | "low_sleep_scores" | "good_sleep")
```

### Cronometer Daily Nutrition
```
- Date: string (YYYY-MM-DD)
- Completed: bool
- Energy: double (kcal)
- Carbs, Fiber, Starch, Sugars, AddedSugars, NetCarbs: double (g)
- Fat, Protein: double (g)
- Cholesterol: double (mg)
- Monounsaturated, Polyunsaturated, Saturated, TransFats: double (g)
- Omega3, Omega6: double (g)
- VitaminA: double (µg)
- VitaminC: double (mg)
- VitaminD: double (IU)
- VitaminE: double (mg)
- VitaminK: double (µg)
- B1Thiamine: double (mg)
- B2Riboflavin: double (mg)
- B3Niacin: double (mg)
- B5PantothenicAcid: double (mg)
- B6Pyridoxine: double (mg)
- B12Cobalamin: double (µg)
- Folate: double (µg)
- Calcium: double (mg)
- Copper: double (mg)
- Iron: double (mg)
- Magnesium: double (mg)
- Manganese: double (mg)
- Phosphorus: double (mg)
- Potassium: double (mg)
- Selenium: double (µg)
- Sodium: double (mg)
- Zinc: double (mg)
- Alcohol: double (g)
- Caffeine: double (mg)
- Water: double (g)
```

### Cronometer Food Serving
```
- Day: string
- Group: string (meal group)
- FoodName: string
- Amount: string (serving description)
- Category: string
- Energy: double (kcal)
- Carbs: double (g)
- Fat: double (g)
- Protein: double (g)
- Fiber: double (g)
- Sugars: double (g)
- Sodium: double (mg)
```

### Cronometer Exercise Entry
```
- Day: string
- Group: string
- Exercise: string
- Minutes: double
- CaloriesBurned: double
```

### Cronometer Biometric Entry
```
- Day: string
- Group: string
- Metric: string
- Unit: string
- Amount: double
```

### Cronometer Note Entry
```
- Day: string
- Group: string
- Note: string
```

### User Settings
```
- Height: int (cm)
- Calories: int (default 2000)
- Protein: int (default 150)
- Carbs: int (default 250)
- Fat: int (default 65)
- LastUpdated: DateTime
```

---

## API Endpoints

### Picooc Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/picooc/data` | Get all measurements |
| POST | `/api/picooc/sync` | Sync from Picooc |
| GET | `/api/picooc/latest` | Get latest measurement |
| GET | `/api/picooc/status` | Get connection status |
| GET | `/api/picooc/stats` | Get statistics |

### Oura Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/oura/data` | Get all Oura data |
| POST | `/api/oura/sync` | Sync from Oura (last 30 days) |
| GET | `/api/oura/sleep` | Get sleep records |
| GET | `/api/oura/sleep/{id}` | Get sleep detail with contributors |
| GET | `/api/oura/readiness` | Get readiness records |
| GET | `/api/oura/activity` | Get activity records |
| GET | `/api/oura/latest` | Get latest records |
| GET | `/api/oura/stats` | Get statistics |
| GET | `/api/oura/status` | Get connection status |
| GET | `/api/oura/workouts` | Get workout records |
| GET | `/api/oura/stress` | Get stress records |
| GET | `/api/oura/resilience` | Get resilience records |

### Cronometer Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/cronometer/data` | Get all nutrition data |
| POST | `/api/cronometer/sync` | Sync from Cronometer (last 30 days) |
| GET | `/api/cronometer/daily/{date}` | Get daily nutrition + servings |
| GET | `/api/cronometer/latest` | Get latest nutrition |
| GET | `/api/cronometer/stats` | Get statistics |
| GET | `/api/cronometer/status` | Get connection status |

### Settings Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/settings` | Get user settings |
| POST | `/api/settings` | Save user settings |

---

## Visual Design

### Color Scheme
| Element | Color |
|---------|-------|
| Primary accent | `#00d4ff` (cyan) |
| Sleep | `#3b82f6` (blue) |
| Readiness | `#22c55e` (green) |
| Activity | `#f59e0b` (amber) |
| Body Fat | `#f472b6` (pink) |
| BMI | `#a78bfa` (purple) |
| Muscle | `#34d399` (emerald) |
| Food/Calories | `#f97316` (orange) |
| Protein | `#ef4444` (red) |
| Carbs | `#eab308` (yellow) |
| Fat | `#22c55e` (green) |
| Deep Sleep | `#1e40af` (dark blue) |
| REM Sleep | `#8b5cf6` (violet) |
| Heart Rate | `#ef4444` (red) |
| Steps | `#8b5cf6` (purple) |

### Score Badges
| Score Range | Class | Color |
|-------------|-------|-------|
| ≥ 85 | `score-good` | Green |
| 70-84 | `score-ok` | Yellow |
| < 70 | `score-low` | Red |

### Progress Indicators
- **Positive change** (good): Green with ↑/↓ based on metric
- **Negative change** (bad): Red with ↑/↓ based on metric
- **Neutral**: Gray with →

### Sleep Stages Colors
| Stage | Color |
|-------|-------|
| Deep Sleep | Dark blue |
| Light Sleep | Light blue |
| REM Sleep | Purple |
| Awake | Gray |

### Activity Time Colors
| Level | Color |
|-------|-------|
| High Activity | Red/Orange |
| Medium Activity | Yellow |
| Low Activity | Green |

---

## Chart Configurations

### Time Range Options
All charts support these time range filters:
- **7 Days** (`7d`)
- **30 Days** (`30d`)
- **3 Months** (`90d`)
- **6 Months** (`6m`)
- **1 Year** (`1y`)
- **All Time** (`all`)

### Chart Types Used
1. **Line Charts**: Weight trends, Health scores, Sleep duration, Calorie trends
2. **Doughnut Chart**: Macro breakdown
3. **Progress Bars**: Nutrient tracking, Body composition
4. **Stacked Bar (visual)**: Sleep stages, Activity time breakdown

---

## Modal Windows

### Available Modals
1. **Sleep Detail Modal** - Detailed sleep analysis with stages and vitals
2. **Measurement Detail Modal** - Body composition breakdown
3. **Activity Detail Modal** - Activity breakdown with time distribution
4. **Food Detail Modal** - Individual food item nutrition details
5. **Settings Modal** - User preferences and nutrition targets
6. **Stress Modal** - Detailed stress analysis (via external JS)
7. **Workouts Modal** - Workout history (via external JS)

### Modal Behavior
- Close on ESC key press
- Close on backdrop click
- Close button in header
- Body scroll locked when open

---

## Data Sync Behavior

### Sync Ranges
- **Oura**: Last 30 days from current date
- **Cronometer**: Last 30 days from current date
- **Picooc**: All available data

### Sync Status Display
Each data source shows:
- Connection status (configured/not configured)
- Record count
- Last sync timestamp
- Error messages if sync fails

---

## Responsive Design

### Breakpoints
- Desktop: Full grid layout
- Tablet: Reduced columns
- Mobile: Single column stack

### Mobile Considerations
- Touch-friendly buttons
- Swipeable date navigation (Food tab)
- Collapsible sections
- Scrollable tables

---

## Local Storage

### Cached Data
- User settings (fallback)
- Section collapsed states
- Time range preferences (implicit in UI)

### Storage Keys
- `healthAggregatorSettings` - User settings backup
- `section-advancedMetrics` - Collapse state
- `section-recoveryVitals` - Collapse state

---

## Features Summary

1. **Multi-source aggregation**: Combines data from Oura Ring, Picooc scales, and Cronometer
2. **Comprehensive dashboards**: 4 dedicated views for different health aspects
3. **Historical tracking**: Charts with multiple time range options (7d to All Time)
4. **Progress tracking**: Comparison vs previous, weekly, and monthly measurements
5. **Detailed modals**: Click-through for in-depth analysis of any record
6. **Personalized insights**: AI-generated health recommendations
7. **Customizable targets**: User-defined nutrition goals with presets
8. **Responsive design**: Works on desktop and mobile
9. **Real-time sync**: Manual sync from all data sources
10. **Cloud storage**: Settings and data persisted in Azure Blob Storage
11. **Offline support**: Data cached locally for offline viewing
12. **Accessibility**: Skip links, ARIA labels, keyboard navigation

---

## External Dependencies

### Frontend Libraries (CDN)
- Chart.js - Data visualization
- chartjs-adapter-date-fns - Date handling for Chart.js

### Backend NuGet Packages
- Microsoft.Azure.Functions.Worker
- Azure.Storage.Blobs
- System.Text.Json

### External APIs
- Oura API v2 (Bearer token auth)
- Picooc native API
- Cronometer API (credential-based)

---

## Configuration

### Required Settings (local.settings.json / App Settings)
```json
{
  "Oura": {
    "AccessToken": "your-oura-personal-access-token"
  },
  "Picooc": {
    "Email": "your-picooc-email",
    "Password": "your-picooc-password"
  },
  "Cronometer": {
    "Email": "your-cronometer-email",
    "Password": "your-cronometer-password"
  },
  "AzureWebJobsStorage": "your-azure-storage-connection-string"
}
```

### Blob Storage Containers
- `health-data` - Stores JSON data files for each data source
- Settings file: `settings.json`
- Oura data: `oura-data.json`
- Picooc data: `picooc-data.json`
- Cronometer data: `cronometer-data.json`
