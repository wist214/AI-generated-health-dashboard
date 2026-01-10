# Browser Automation Testing Instructions

When asked to "test", "check", "verify", or "investigate" the application in a browser, follow this workflow.

## Prerequisites

### 1. Ensure Server is Running
```powershell
# Check if server is running
netstat -an | Select-String ":7071"

# If not running, start it
Start-Process powershell -ArgumentList "-NoProfile", "-Command", "cd d:\Work\My\HealthAggregator\HealthAggregatorApi; func start" -WindowStyle Normal

# Wait for server to be ready
Start-Sleep 10; netstat -an | Select-String ":7071"
```

### 2. Ensure Azurite (Storage Emulator) is Running
```powershell
# Check if Azurite is running on port 10000
netstat -an | Select-String ":10000"

# If not running, start it
azurite --silent --location d:\Work\My\HealthAggregator\azurite-data --debug d:\Work\My\HealthAggregator\azurite-debug.log
```

## Browser Automation Tools

### Activate Required Tool Groups
Before using browser tools, activate them:

1. **Navigation & Interaction**: `mcp_microsoft_pla_browser_navigate`, `mcp_microsoft_pla_browser_click`, `mcp_microsoft_pla_browser_type`
2. **Screenshots & Snapshots**: `mcp_microsoft_pla_browser_snapshot`, `mcp_microsoft_pla_browser_take_screenshot`
3. **Console & Debugging**: `mcp_microsoft_pla_browser_console_messages`

### Core Browser Commands

#### Navigate to a Page
```
mcp_microsoft_pla_browser_navigate
- url: "http://localhost:7071/api/ui"
```

#### Get Page State (Accessibility Snapshot)
```
mcp_microsoft_pla_browser_snapshot
```
This returns a YAML tree of all interactive elements with `ref` IDs for clicking.

#### Click an Element
```
mcp_microsoft_pla_browser_click
- element: "Human-readable description"
- ref: "e123"  # From snapshot
```

#### Type Text
```
mcp_microsoft_pla_browser_type
- element: "Description of input field"
- ref: "e456"
- text: "Text to type"
- submit: true/false  # Press Enter after?
```

#### Check Console Messages
```
mcp_microsoft_pla_browser_console_messages
- level: "error"  # Options: error, warning, info, debug
```

#### Take Screenshot
```
mcp_microsoft_pla_browser_take_screenshot
- filename: "test-screenshot.png"
```

#### Press Keyboard Key
```
mcp_microsoft_pla_browser_press_key
- key: "F5"  # Refresh page
```

## Standard Testing Workflow

### 1. Start Fresh
```
1. Navigate to http://localhost:7071/api/ui
2. Take a snapshot to see current state
3. Check console for any errors
```

### 2. Test Navigation
```
1. Click on each tab (Dashboard, Weight, Oura Ring, Food)
2. After each click, check console for errors
3. Verify expected content appears in snapshot
```

### 3. Test Interactions
```
1. Click buttons (Sync, navigation arrows, etc.)
2. Check console messages after each action
3. Verify UI updates correctly
```

### 4. Test Data Display
```
1. Verify data loads from API
2. Check that values display correctly
3. Navigate between dates/time periods
4. Verify charts render (no errors)
```

## Health Aggregator Specific Tests

### Dashboard Tab
- Verify Sleep Score, Readiness, Weight, Body Fat cards display
- Check Weekly Trends chart renders
- Verify Insights section populates

### Weight Tab
- Click Sync Picooc button
- Check weight chart displays
- Test time range buttons (7d, 30d, 3m, etc.)

### Oura Ring Tab
- Click Sync Oura button
- Verify sleep/readiness data displays
- Test time range selectors
- Click on sleep records to open detail modal

### Food Tab
- Click Sync Cronometer button
- Verify calorie/macro summary displays
- Navigate dates using ◀ ▶ buttons
- Verify food log table populates
- Click on food items to open detail modal

## Debugging Common Issues

### Page Won't Load
1. Check server is running: `netstat -an | Select-String ":7071"`
2. Check Azurite is running: `netstat -an | Select-String ":10000"`
3. Restart server if needed

### JavaScript Errors
1. Use `mcp_microsoft_pla_browser_console_messages` with level "error"
2. Note the error message and line number
3. Read the source file at that line to investigate

### Data Not Displaying
1. Check API directly: `Invoke-RestMethod -Uri "http://localhost:7071/api/[endpoint]"`
2. Verify data structure matches what JS expects (camelCase properties)
3. Check console for fetch errors

### Element Not Found
1. Refresh page and get new snapshot
2. Element refs change after page updates
3. Use snapshot to get current ref IDs

## Example Full Test Session

```
# 1. Navigate to app
mcp_microsoft_pla_browser_navigate(url="http://localhost:7071/api/ui")

# 2. Check for initial errors
mcp_microsoft_pla_browser_console_messages(level="error")

# 3. Get page state
mcp_microsoft_pla_browser_snapshot()

# 4. Click Food tab (find ref from snapshot)
mcp_microsoft_pla_browser_click(element="Food tab", ref="e20")

# 5. Check for errors after click
mcp_microsoft_pla_browser_console_messages(level="error")

# 6. Get updated state
mcp_microsoft_pla_browser_snapshot()

# 7. Click previous day button
mcp_microsoft_pla_browser_click(element="Previous day button", ref="e235")

# 8. Check for errors
mcp_microsoft_pla_browser_console_messages(level="error")

# 9. Verify data loaded in snapshot
mcp_microsoft_pla_browser_snapshot()
```

## Reporting Results

After testing, report:
1. ✅ What worked correctly
2. ❌ What failed (with error messages)
3. 🔧 Fixes applied
4. 📝 Any remaining issues

## Tips

- Always get a fresh snapshot after navigation/clicks - refs change
- Check console after EVERY interaction for early error detection
- Use `info` level console messages to see debug logs
- If something seems cached, restart the server
- Screenshots are useful for visual issues the snapshot doesn't capture
