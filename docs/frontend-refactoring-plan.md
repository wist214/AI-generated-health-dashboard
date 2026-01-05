# Frontend Refactoring Plan: Health Aggregator Dashboard

**Based on:** [Frontend Code Review](./frontend-code-review.md)  
**Target File:** `HealthAggregatorDotNet/wwwroot/index.html`  
**Total Estimated Time:** 15-20 hours  
**Priority:** High → Medium → Low

---

## Phase 1: File Structure Separation (3-4 hours)

### Step 1.1: Create CSS File
**Time:** 1 hour  
**Priority:** 🔴 High

1. Create new file: `wwwroot/css/styles.css`
2. Extract all content from `<style>` tag (lines 9-990)
3. Organize CSS into sections:
   ```
   /* ===========================================
      1. CSS Variables (NEW)
      =========================================== */
   
   /* ===========================================
      2. Reset & Base Styles
      =========================================== */
   
   /* ===========================================
      3. Layout (Container, Grid)
      =========================================== */
   
   /* ===========================================
      4. Components (Cards, Buttons, Tables)
      =========================================== */
   
   /* ===========================================
      5. Tab Navigation
      =========================================== */
   
   /* ===========================================
      6. Dashboard Specific
      =========================================== */
   
   /* ===========================================
      7. Charts
      =========================================== */
   
   /* ===========================================
      8. Modals
      =========================================== */
   
   /* ===========================================
      9. Utilities & Helpers
      =========================================== */
   
   /* ===========================================
      10. Media Queries
      =========================================== */
   ```
4. Replace `<style>` tag with: `<link rel="stylesheet" href="/css/styles.css">`
5. Test all pages/tabs render correctly

### Step 1.2: Create JavaScript Files
**Time:** 2 hours  
**Priority:** 🔴 High

1. Create folder structure:
   ```
   wwwroot/
   ├── css/
   │   └── styles.css
   └── js/
       ├── app.js           # Main initialization
       ├── state.js         # Global state management
       ├── api.js           # API calls
       ├── dashboard.js     # Dashboard tab logic
       ├── picooc.js        # Picooc tab logic
       ├── oura.js          # Oura tab logic
       ├── charts.js        # Chart configuration helpers
       ├── modals.js        # Modal logic
       └── utils.js         # Utility functions
   ```

2. **state.js** - Extract global state:
   ```javascript
   // State management
   export const state = {
       picoocData: [],
       ouraData: { sleepRecords: [], dailySleep: [], readiness: [], activity: [] },
       charts: {
           picooc: null,
           oura: null,
           sleep: null,
           dashboard: null
       },
       timeRanges: {
           picooc: 'all',
           oura: '1y'
       }
   };
   
   export function updateState(key, value) {
       state[key] = value;
   }
   ```

3. **api.js** - Extract API functions:
   ```javascript
   export async function fetchData(endpoint) { ... }
   export async function syncPicooc() { ... }
   export async function syncOura() { ... }
   export async function fetchSleepDetail(id) { ... }
   ```

4. **utils.js** - Extract utility functions:
   ```javascript
   export function formatDuration(seconds) { ... }
   export function getScoreClass(score) { ... }
   export function filterDataByTimeRange(data, range, getDateFn) { ... }
   ```

5. **charts.js** - Extract chart helpers:
   ```javascript
   export function createBaseChartConfig(options) { ... }
   export function destroyChart(chartRef) { ... }
   ```

6. Update `index.html`:
   ```html
   <script type="module" src="/js/app.js"></script>
   ```

7. Test all functionality works

### Step 1.3: Add Resource Hints
**Time:** 15 minutes  
**Priority:** 🟡 Medium

Add to `<head>`:
```html
<link rel="preconnect" href="https://cdn.jsdelivr.net">
<link rel="dns-prefetch" href="https://cdn.jsdelivr.net">
```

### Step 1.4: Add Script Defer/Module
**Time:** 15 minutes  
**Priority:** 🔴 High

Change:
```html
<!-- Before -->
<script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
<script src="https://cdn.jsdelivr.net/npm/chartjs-adapter-date-fns"></script>

<!-- After -->
<script defer src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>
<script defer src="https://cdn.jsdelivr.net/npm/chartjs-adapter-date-fns@3.0.0/dist/chartjs-adapter-date-fns.bundle.min.js"></script>
```

---

## Phase 2: CSS Improvements (2-3 hours)

### Step 2.1: Add CSS Custom Properties
**Time:** 1.5 hours  
**Priority:** 🟡 Medium

1. Add at top of `styles.css`:
   ```css
   :root {
       /* Colors - Background */
       --color-bg-primary: #1a1a2e;
       --color-bg-secondary: #16213e;
       --color-bg-card: rgba(255, 255, 255, 0.05);
       --color-bg-hover: rgba(255, 255, 255, 0.1);
       
       /* Colors - Text */
       --color-text-primary: #e0e0e0;
       --color-text-secondary: #aaa;
       --color-text-muted: #888;
       --color-text-dim: #666;
       
       /* Colors - Accent */
       --color-accent-blue: #00d4ff;
       --color-accent-purple: #7c3aed;
       --color-accent-pink: #f472b6;
       --color-accent-green: #22c55e;
       --color-accent-yellow: #f59e0b;
       --color-accent-red: #ef4444;
       
       /* Colors - Oura Theme */
       --color-oura-primary: #00a99d;
       --color-oura-secondary: #00d4aa;
       
       /* Colors - Sleep Theme */
       --color-sleep-primary: #3b82f6;
       --color-sleep-deep: #1e40af;
       --color-sleep-light: #60a5fa;
       --color-sleep-rem: #8b5cf6;
       --color-sleep-awake: #f97316;
       
       /* Borders */
       --border-color: rgba(255, 255, 255, 0.1);
       --border-color-light: rgba(255, 255, 255, 0.05);
       
       /* Border Radius */
       --radius-sm: 8px;
       --radius-md: 12px;
       --radius-lg: 15px;
       --radius-xl: 20px;
       
       /* Spacing */
       --spacing-xs: 5px;
       --spacing-sm: 10px;
       --spacing-md: 15px;
       --spacing-lg: 20px;
       --spacing-xl: 30px;
       
       /* Transitions */
       --transition-fast: 0.2s ease;
       --transition-normal: 0.3s ease;
       
       /* Shadows */
       --shadow-card: 0 10px 30px rgba(0, 0, 0, 0.2);
       --shadow-hover: 0 10px 30px rgba(124, 58, 237, 0.3);
       
       /* Z-index layers */
       --z-modal: 1000;
       --z-tooltip: 900;
       --z-header: 100;
   }
   ```

2. Replace all hardcoded values with variables throughout CSS
3. Test dark theme consistency

### Step 2.2: Add Focus Styles
**Time:** 30 minutes  
**Priority:** 🔴 High (Accessibility)

Add to CSS:
```css
/* Focus styles for accessibility */
:focus {
    outline: none;
}

:focus-visible {
    outline: 2px solid var(--color-accent-blue);
    outline-offset: 2px;
}

.btn:focus-visible {
    box-shadow: 0 0 0 3px rgba(0, 212, 255, 0.4);
}

.tab-btn:focus-visible {
    box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.4);
}

/* Skip link for keyboard navigation */
.skip-link {
    position: absolute;
    top: -40px;
    left: 0;
    background: var(--color-accent-blue);
    color: white;
    padding: 8px 16px;
    z-index: 1001;
    transition: top var(--transition-fast);
}

.skip-link:focus {
    top: 0;
}
```

### Step 2.3: Remove !important
**Time:** 15 minutes  
**Priority:** 🟢 Low

Find and fix:
```css
/* Before */
.chart-wrapper canvas {
    max-height: 400px !important;
}

/* After - increase specificity instead */
.chart-container .chart-wrapper canvas {
    max-height: 400px;
}
```

---

## Phase 3: HTML Improvements (1-2 hours)

### Step 3.1: Add Semantic Structure
**Time:** 30 minutes  
**Priority:** 🔴 High

1. Add skip link at start of body:
   ```html
   <body>
       <a href="#main-content" class="skip-link">Skip to main content</a>
       ...
   ```

2. Wrap content in `<main>`:
   ```html
   <div class="container">
       <header>...</header>
       <main id="main-content">
           <nav class="tab-nav" aria-label="Main navigation">
               ...
           </nav>
           <!-- Tab contents -->
       </main>
   </div>
   ```

### Step 3.2: Add Button Types
**Time:** 20 minutes  
**Priority:** 🔴 High

Add `type="button"` to all buttons:
```html
<button type="button" class="tab-btn dashboard-btn active" data-tab="dashboard">
<button type="button" class="btn btn-primary" id="syncBtn">
<button type="button" class="time-btn" data-range="7d">
<button type="button" class="modal-close" aria-label="Close modal">
```

### Step 3.3: Add Table Accessibility
**Time:** 20 minutes  
**Priority:** 🔴 High

Add `scope` attributes:
```html
<thead>
    <tr>
        <th scope="col">Date</th>
        <th scope="col">Weight (kg)</th>
        <th scope="col">Body Fat (%)</th>
        ...
    </tr>
</thead>
```

### Step 3.4: Add ARIA Attributes
**Time:** 30 minutes  
**Priority:** 🔴 High

1. Status messages:
   ```html
   <div id="statusMessage" class="status" role="status" aria-live="polite"></div>
   <div id="ouraStatusMessage" class="status" role="status" aria-live="polite"></div>
   ```

2. Tab navigation:
   ```html
   <nav class="tab-nav" role="tablist" aria-label="Data sections">
       <button type="button" class="tab-btn" role="tab" 
               aria-selected="true" aria-controls="dashboard-tab" id="tab-dashboard">
           <span class="tab-icon" aria-hidden="true">📊</span>
           <span class="tab-text">Dashboard</span>
       </button>
       ...
   </nav>
   
   <div id="dashboard-tab" class="tab-content active" role="tabpanel" 
        aria-labelledby="tab-dashboard" tabindex="0">
   ```

3. Tab buttons with hidden text (mobile):
   ```html
   <button type="button" class="tab-btn" aria-label="Dashboard">
       <span class="tab-icon" aria-hidden="true">📊</span>
       <span class="tab-text">Dashboard</span>
   </button>
   ```

4. Modals:
   ```html
   <div class="modal-overlay" id="sleepModal" role="dialog" aria-modal="true" 
        aria-labelledby="sleepModalTitle" aria-hidden="true">
       <div class="modal sleep-modal">
           <div class="modal-header">
               <h2 id="sleepModalTitle">Sleep Analysis</h2>
               <button type="button" class="modal-close" aria-label="Close modal">×</button>
           </div>
           ...
       </div>
   </div>
   ```

---

## Phase 4: JavaScript Improvements (5-7 hours)

### Step 4.1: Remove Inline Event Handlers
**Time:** 1.5 hours  
**Priority:** 🟡 Medium

1. Add data attributes to HTML:
   ```html
   <button type="button" class="tab-btn" data-tab="dashboard">
   <button type="button" class="time-btn" data-range="7d">
   <tr data-index="0" class="clickable-row">
   ```

2. Add event listeners in JS:
   ```javascript
   // Tab navigation
   document.querySelectorAll('[data-tab]').forEach(btn => {
       btn.addEventListener('click', (e) => {
           const tabName = e.currentTarget.dataset.tab;
           switchTab(tabName, e.currentTarget);
       });
   });
   
   // Time range buttons
   document.querySelectorAll('[data-range]').forEach(btn => {
       btn.addEventListener('click', (e) => {
           const range = e.currentTarget.dataset.range;
           const isPicooc = e.currentTarget.closest('#picooc-time-selector');
           if (isPicooc) {
               setPicoocTimeRange(range, e.currentTarget);
           } else {
               setOuraTimeRange(range, e.currentTarget);
           }
       });
   });
   
   // Sync buttons
   document.getElementById('syncBtn').addEventListener('click', syncPicoocData);
   document.getElementById('ouraSyncBtn').addEventListener('click', syncOuraData);
   
   // Table row clicks (event delegation)
   document.getElementById('picoocTableBody').addEventListener('click', (e) => {
       const row = e.target.closest('tr[data-index]');
       if (row) {
           showMeasurementDetail(parseInt(row.dataset.index));
       }
   });
   
   // Modal close
   document.querySelectorAll('.modal-close').forEach(btn => {
       btn.addEventListener('click', (e) => {
           const modal = e.target.closest('.modal-overlay');
           closeModal(modal.id);
       });
   });
   
   // Modal overlay click
   document.querySelectorAll('.modal-overlay').forEach(modal => {
       modal.addEventListener('click', (e) => {
           if (e.target === modal) {
               closeModal(modal.id);
           }
       });
   });
   
   // Checkbox changes
   document.querySelectorAll('#showWeight, #showBodyFat, #showBMI, #showMuscle')
       .forEach(cb => cb.addEventListener('change', updatePicoocChart));
   
   document.querySelectorAll('#showSleepScore, #showReadinessScore, #showActivityScore, #showSteps')
       .forEach(cb => cb.addEventListener('change', updateOuraChart));
   
   document.querySelectorAll('#showTotalSleep, #showDeepSleep, #showRemSleep, #showAvgHR')
       .forEach(cb => cb.addEventListener('change', updateSleepChart));
   ```

### Step 4.2: Cache DOM References
**Time:** 1 hour  
**Priority:** 🟡 Medium

Create `elements.js`:
```javascript
// Cache all frequently accessed DOM elements
export const elements = {
    // Dashboard
    dashboardDate: document.getElementById('dashboardDate'),
    dashWeight: document.getElementById('dashWeight'),
    dashWeightDate: document.getElementById('dashWeightDate'),
    dashBodyFat: document.getElementById('dashBodyFat'),
    dashBodyFatDate: document.getElementById('dashBodyFatDate'),
    dashSleepScore: document.getElementById('dashSleepScore'),
    dashSleepDate: document.getElementById('dashSleepDate'),
    dashReadiness: document.getElementById('dashReadiness'),
    dashReadinessDate: document.getElementById('dashReadinessDate'),
    dashSteps: document.getElementById('dashSteps'),
    dashSleepDuration: document.getElementById('dashSleepDuration'),
    dashAvgHR: document.getElementById('dashAvgHR'),
    dashBodyWater: document.getElementById('dashBodyWater'),
    insightsList: document.getElementById('insightsList'),
    
    // Progress elements
    weightVsYesterday: document.getElementById('weightVsYesterday'),
    weightVsWeek: document.getElementById('weightVsWeek'),
    weightVsMonth: document.getElementById('weightVsMonth'),
    // ... etc
    
    // Picooc
    syncBtn: document.getElementById('syncBtn'),
    statusMessage: document.getElementById('statusMessage'),
    picoocTableBody: document.getElementById('picoocTableBody'),
    // ... etc
    
    // Oura
    ouraSyncBtn: document.getElementById('ouraSyncBtn'),
    ouraStatusMessage: document.getElementById('ouraStatusMessage'),
    ouraSleepTableBody: document.getElementById('ouraSleepTableBody'),
    ouraActivityTableBody: document.getElementById('ouraActivityTableBody'),
    // ... etc
    
    // Charts
    dashboardChart: document.getElementById('dashboardChart'),
    picoocChart: document.getElementById('picoocChart'),
    ouraChart: document.getElementById('ouraChart'),
    sleepChart: document.getElementById('sleepChart'),
    
    // Modals
    sleepModal: document.getElementById('sleepModal'),
    measurementModal: document.getElementById('measurementModal'),
    // ... modal inner elements
};

// Initialize elements after DOM ready
export function initElements() {
    // Re-query any elements that might be dynamically created
}
```

### Step 4.3: Add Modal Focus Management
**Time:** 1 hour  
**Priority:** 🔴 High (Accessibility)

Create `modals.js`:
```javascript
let lastFocusedElement = null;
let focusTrap = null;

export function openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;
    
    // Store last focused element
    lastFocusedElement = document.activeElement;
    
    // Show modal
    modal.classList.add('active');
    modal.setAttribute('aria-hidden', 'false');
    document.body.style.overflow = 'hidden';
    
    // Focus first focusable element
    const focusableElements = modal.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    const firstFocusable = focusableElements[0];
    const lastFocusable = focusableElements[focusableElements.length - 1];
    
    if (firstFocusable) {
        firstFocusable.focus();
    }
    
    // Set up focus trap
    focusTrap = (e) => {
        if (e.key === 'Tab') {
            if (e.shiftKey) {
                if (document.activeElement === firstFocusable) {
                    e.preventDefault();
                    lastFocusable.focus();
                }
            } else {
                if (document.activeElement === lastFocusable) {
                    e.preventDefault();
                    firstFocusable.focus();
                }
            }
        }
        
        if (e.key === 'Escape') {
            closeModal(modalId);
        }
    };
    
    modal.addEventListener('keydown', focusTrap);
}

export function closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;
    
    // Remove focus trap
    if (focusTrap) {
        modal.removeEventListener('keydown', focusTrap);
        focusTrap = null;
    }
    
    // Hide modal
    modal.classList.remove('active');
    modal.setAttribute('aria-hidden', 'true');
    document.body.style.overflow = '';
    
    // Restore focus
    if (lastFocusedElement) {
        lastFocusedElement.focus();
        lastFocusedElement = null;
    }
}
```

### Step 4.4: Add Constants for Magic Numbers
**Time:** 30 minutes  
**Priority:** 🟡 Medium

Create `constants.js`:
```javascript
export const SCORE_THRESHOLDS = {
    EXCELLENT: 85,
    GOOD: 70,
    POOR: 50
};

export const WEIGHT_CHANGE = {
    SIGNIFICANT: 0.3,  // kg
    WEEK_TOLERANCE: 0.6,
    MONTH_TOLERANCE: 0.3
};

export const BMI_RANGES = {
    UNDERWEIGHT: 18.5,
    NORMAL: 25,
    OVERWEIGHT: 30
};

export const BODY_FAT_RANGES = {
    ATHLETIC: 14,
    FIT: 20,
    AVERAGE: 25
};

export const VISCERAL_FAT = {
    HEALTHY: 9,
    HIGH: 14
};

export const TABLE_LIMITS = {
    DEFAULT: 50,
    EXTENDED: 100
};

export const TIME_RANGES = {
    '7d': 7 * 24 * 60 * 60 * 1000,
    '30d': 30 * 24 * 60 * 60 * 1000,
    '90d': 90 * 24 * 60 * 60 * 1000
};

export const CHART_COLORS = {
    weight: '#00d4ff',
    bodyFat: '#f472b6',
    bmi: '#a78bfa',
    muscle: '#34d399',
    sleep: '#3b82f6',
    readiness: '#22c55e',
    activity: '#f59e0b',
    steps: '#8b5cf6',
    heart: '#ef4444'
};
```

### Step 4.5: Refactor Large Functions
**Time:** 2-3 hours  
**Priority:** 🟡 Medium

Split `updateDashboardMetrics()`:
```javascript
function updateDashboardMetrics() {
    updateDashboardDate();
    updateWeightMetrics();
    updateBodyFatMetrics();
    updateSleepMetrics();
    updateReadinessMetrics();
    updateActivityMetrics();
}

function updateWeightMetrics() {
    if (state.picoocData.length === 0) return;
    
    const sorted = getSortedPicoocData();
    const latest = sorted[0];
    const weight = getValue(latest, 'weight', 'Weight');
    
    if (weight) {
        elements.dashWeight.innerHTML = `${weight}<span class="metric-unit"> kg</span>`;
        elements.dashWeightDate.textContent = formatDate(getDate(latest));
        calculateProgress('weight', sorted, getWeight, getDate, true);
    }
}

// Similar functions for other metrics...
```

Split `updateInsights()`:
```javascript
function updateInsights() {
    const insights = [
        ...generateWeightInsights(),
        ...generateSleepInsights(),
        ...generateReadinessInsights(),
        ...generateActivityInsights()
    ];
    
    renderInsights(insights);
}

function generateWeightInsights() {
    const insights = [];
    // Weight-specific insight logic
    return insights;
}

// Similar functions for other insight types...
```

### Step 4.6: Add Error Handling
**Time:** 45 minutes  
**Priority:** 🟡 Medium

Create `api.js` with proper error handling:
```javascript
class ApiError extends Error {
    constructor(message, status, endpoint) {
        super(message);
        this.status = status;
        this.endpoint = endpoint;
    }
}

export async function fetchApi(endpoint, options = {}) {
    try {
        const response = await fetch(endpoint, options);
        
        if (!response.ok) {
            throw new ApiError(
                `HTTP ${response.status}: ${response.statusText}`,
                response.status,
                endpoint
            );
        }
        
        return await response.json();
    } catch (error) {
        if (error instanceof ApiError) {
            throw error;
        }
        throw new ApiError(
            `Network error: ${error.message}`,
            0,
            endpoint
        );
    }
}

export function showError(message, element) {
    if (element) {
        element.className = 'status error';
        element.textContent = `❌ ${message}`;
    }
    console.error(message);
}

export function showSuccess(message, element) {
    if (element) {
        element.className = 'status success';
        element.textContent = `✅ ${message}`;
    }
}
```

### Step 4.7: Add JSDoc Comments
**Time:** 1 hour  
**Priority:** 🟢 Low

Add documentation:
```javascript
/**
 * Synchronizes Picooc scale data from the cloud API
 * Updates UI with loading state, fetches data, and handles errors
 * @async
 * @returns {Promise<void>}
 * @throws {ApiError} When API request fails
 */
export async function syncPicoocData() {
    // ...
}

/**
 * Formats duration in seconds to human-readable string
 * @param {number} seconds - Duration in seconds
 * @returns {string} Formatted string like "7h 30m" or "--" if invalid
 * @example
 * formatDuration(27000) // Returns "7h 30m"
 * formatDuration(null)  // Returns "--"
 */
export function formatDuration(seconds) {
    // ...
}

/**
 * Calculates and displays progress metrics comparing current to historical values
 * @param {string} metricType - Type of metric (weight, bodyFat, sleep, readiness)
 * @param {Array} data - Sorted array of data points (newest first)
 * @param {Function} getValue - Function to extract value from data point
 * @param {Function} getDate - Function to extract date from data point
 * @param {boolean} lowerIsBetter - True if lower values indicate improvement
 */
function calculateProgress(metricType, data, getValue, getDate, lowerIsBetter) {
    // ...
}
```

---

## Phase 5: Security Improvements (30 minutes)

### Step 5.1: Add Subresource Integrity (SRI)
**Time:** 15 minutes  
**Priority:** 🟡 Medium

Update script tags:
```html
<script defer 
        src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"
        integrity="sha384-..." 
        crossorigin="anonymous"></script>
```

Generate SRI hashes:
```bash
# Use https://www.srihash.org/ or
curl -s https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js | openssl dgst -sha384 -binary | openssl base64 -A
```

### Step 5.2: Add CSP Meta Tag (Optional)
**Time:** 15 minutes  
**Priority:** 🟢 Low

Add to `<head>`:
```html
<meta http-equiv="Content-Security-Policy" 
      content="default-src 'self'; 
               script-src 'self' https://cdn.jsdelivr.net; 
               style-src 'self' 'unsafe-inline'; 
               img-src 'self' data:;">
```

Or better, configure in ASP.NET Core middleware.

---

## Phase 6: Testing & Validation (1-2 hours)

### Step 6.1: Functional Testing
**Time:** 30 minutes

- [ ] All tabs switch correctly
- [ ] Picooc sync works
- [ ] Oura sync works
- [ ] Charts render correctly
- [ ] Time range filters work
- [ ] Tables populate correctly
- [ ] Modals open and close
- [ ] All data displays correctly

### Step 6.2: Accessibility Testing
**Time:** 30 minutes

- [ ] Test with keyboard only (Tab, Enter, Escape)
- [ ] Test with screen reader (NVDA or VoiceOver)
- [ ] Run Lighthouse accessibility audit (target: 90+)
- [ ] Check color contrast with browser devtools
- [ ] Verify focus indicators are visible

### Step 6.3: Performance Testing
**Time:** 30 minutes

- [ ] Run Lighthouse performance audit (target: 90+)
- [ ] Check Network tab for file sizes
- [ ] Verify CSS/JS are cached correctly
- [ ] Test on slow 3G network throttling
- [ ] Check for memory leaks (long running)

### Step 6.4: Cross-Browser Testing
**Time:** 30 minutes

- [ ] Chrome
- [ ] Firefox
- [ ] Safari (if available)
- [ ] Edge
- [ ] Mobile Chrome/Safari

---

## Final File Structure

```
wwwroot/
├── index.html              # ~200 lines (HTML only)
├── css/
│   └── styles.css          # ~1000 lines (all CSS)
└── js/
    ├── app.js              # ~50 lines (initialization)
    ├── state.js            # ~30 lines (state management)
    ├── constants.js        # ~60 lines (constants)
    ├── elements.js         # ~80 lines (DOM cache)
    ├── api.js              # ~100 lines (API calls)
    ├── utils.js            # ~80 lines (utilities)
    ├── charts.js           # ~150 lines (chart helpers)
    ├── modals.js           # ~100 lines (modal logic)
    ├── dashboard.js        # ~200 lines (dashboard tab)
    ├── picooc.js           # ~150 lines (picooc tab)
    └── oura.js             # ~200 lines (oura tab)
```

---

## Implementation Order

| Phase | Task | Time | Priority | Dependencies |
|-------|------|------|----------|--------------|
| 1.1 | Extract CSS | 1h | 🔴 | None |
| 1.2 | Extract JS | 2h | 🔴 | 1.1 |
| 1.3 | Resource hints | 15m | 🟡 | None |
| 1.4 | Script defer | 15m | 🔴 | None |
| 2.1 | CSS variables | 1.5h | 🟡 | 1.1 |
| 2.2 | Focus styles | 30m | 🔴 | 1.1 |
| 2.3 | Remove !important | 15m | 🟢 | 1.1 |
| 3.1 | Semantic HTML | 30m | 🔴 | None |
| 3.2 | Button types | 20m | 🔴 | None |
| 3.3 | Table accessibility | 20m | 🔴 | None |
| 3.4 | ARIA attributes | 30m | 🔴 | None |
| 4.1 | Remove onclick | 1.5h | 🟡 | 1.2 |
| 4.2 | Cache DOM | 1h | 🟡 | 1.2 |
| 4.3 | Modal focus | 1h | 🔴 | 1.2 |
| 4.4 | Constants | 30m | 🟡 | 1.2 |
| 4.5 | Refactor functions | 2-3h | 🟡 | 1.2, 4.2 |
| 4.6 | Error handling | 45m | 🟡 | 1.2 |
| 4.7 | JSDoc | 1h | 🟢 | 4.5 |
| 5.1 | SRI | 15m | 🟡 | None |
| 5.2 | CSP | 15m | 🟢 | None |
| 6.x | Testing | 1-2h | 🔴 | All above |

---

## Quick Wins (Do First - Under 1 Hour Total)

1. ✅ Add `defer` to script tags (5 min)
2. ✅ Add resource hints (5 min)
3. ✅ Add `type="button"` to buttons (10 min)
4. ✅ Add `scope` to table headers (10 min)
5. ✅ Add `role="status"` and `aria-live` to status elements (5 min)
6. ✅ Add skip link (10 min)
7. ✅ Add `aria-label` to modal close buttons (5 min)

**Total Quick Wins Time:** ~50 minutes for significant accessibility improvement

---

## Notes

- Test after each phase before moving to next
- Commit changes after each completed step
- Keep backup of original file before starting
- If time-constrained, prioritize 🔴 High items only (~6 hours)
