# React TypeScript SPA Implementation Plan

## 1. Overview

This document outlines the implementation plan for the Health Aggregator React Single-Page Application (SPA) using **React 18**, **TypeScript**, **Vite**, and **Fluent UI React v9**, following **SOLID**, **DRY**, and **KISS** principles with Microsoft best practices for 2024-2026.

**CRITICAL REQUIREMENT**: The React SPA MUST maintain the **EXACT SAME UI DESIGN** as the current vanilla JavaScript dashboard. This is a pixel-perfect migration where the only changes are the underlying framework and architecture - the visual appearance, colors, gradients, animations, and overall user experience must remain identical.

### 1.1 Technology Stack

- **Framework**: React 18.2+ with TypeScript 5.0+
- **Build Tool**: Vite 5.0+ (ultra-fast HMR, optimized builds)
- **UI Library**: Fluent UI React v9 (Microsoft's design system) - **HEAVILY CUSTOMIZED**
- **Charting Library**: Chart.js with react-chartjs-2 (SAME as current implementation)
- **State Management**: TanStack Query (React Query) for server state
- **Routing**: React Router v6
- **HTTP Client**: Axios with TypeScript types
- **Styling**: CSS Modules + Custom CSS matching current design tokens
- **Testing**: Vitest for unit tests, Playwright for E2E
- **Hosting**: Azure Static Web Apps
- **Code Quality**: ESLint, Prettier, TypeScript strict mode

### 1.2 Architecture Goals

- **Type Safety**: Full TypeScript coverage with strict mode
- **Performance**: Code splitting, lazy loading, optimized bundle size
- **Maintainability**: Feature-based folder structure, clear separation of concerns
- **Reusability**: Shared components, custom hooks, utility functions
- **Testability**: Isolated components, mocked API calls
- **Accessibility**: WCAG 2.1 Level AA compliance via Fluent UI
- **Design Preservation**: 100% visual parity with current dashboard (pixel-perfect)

---

## 2. Project Location and Structure

### 2.1 Important: React SPA Location

**CRITICAL:** This new React application should be created in a **separate folder** to avoid modifying the existing dashboard.

**Existing V1 dashboard:** `HealthAggregatorApi/dashboard/` (current static HTML/JS/CSS dashboard)

**New V2 SPA location:** `HealthAggregatorV2/src/Spa/` (new React TypeScript application)

**Rationale:**
- Keep existing V1 dashboard operational during migration
- Enable side-by-side comparison and testing
- Allow users to switch between V1 and V2 UI
- Preserve rollback capability
- Both can initially call the same or different backend APIs

### 2.2 React SPA Project Structure (Feature-Based)

```
HealthAggregatorV2/src/Spa/
├── public/
│   ├── favicon.ico
│   └── manifest.json
│
├── src/
│   ├── main.tsx                        # Application entry point
│   ├── App.tsx                         # Root component with routing
│   ├── vite-env.d.ts                   # Vite TypeScript definitions
│   │
│   ├── features/                       # Feature modules (vertical slices)
│   │   ├── dashboard/
│   │   │   ├── components/
│   │   │   │   ├── DashboardCard.tsx
│   │   │   │   ├── MetricChart.tsx
│   │   │   │   └── QuickStats.tsx
│   │   │   ├── hooks/
│   │   │   │   └── useDashboardData.ts
│   │   │   ├── services/
│   │   │   │   └── dashboardService.ts
│   │   │   ├── types/
│   │   │   │   └── dashboard.types.ts
│   │   │   └── DashboardPage.tsx       # Page component
│   │   │
│   │   ├── metrics/
│   │   │   ├── components/
│   │   │   │   ├── MetricsList.tsx
│   │   │   │   ├── MetricCard.tsx
│   │   │   │   └── MetricHistoryChart.tsx
│   │   │   ├── hooks/
│   │   │   │   ├── useMetrics.ts
│   │   │   │   └── useMetricHistory.ts
│   │   │   ├── services/
│   │   │   │   └── metricsService.ts
│   │   │   ├── types/
│   │   │   │   └── metrics.types.ts
│   │   │   └── MetricsPage.tsx
│   │   │
│   │   ├── sources/
│   │   │   ├── components/
│   │   │   │   ├── SourceCard.tsx
│   │   │   │   └── SourceStatusBadge.tsx
│   │   │   ├── hooks/
│   │   │   │   └── useSourceStatus.ts
│   │   │   ├── services/
│   │   │   │   └── sourcesService.ts
│   │   │   ├── types/
│   │   │   │   └── sources.types.ts
│   │   │   └── SourcesPage.tsx
│   │   │
│   │   └── settings/
│   │       ├── components/
│   │       │   └── SettingsForm.tsx
│   │       └── SettingsPage.tsx
│   │
│   ├── shared/                         # Shared/common code
│   │   ├── components/                 # Reusable components
│   │   │   ├── Layout/
│   │   │   │   ├── AppHeader.tsx
│   │   │   │   ├── AppSidebar.tsx
│   │   │   │   └── AppLayout.tsx
│   │   │   ├── ErrorBoundary.tsx
│   │   │   ├── LoadingSpinner.tsx
│   │   │   └── ErrorMessage.tsx
│   │   │
│   │   ├── hooks/                      # Custom React hooks
│   │   │   ├── useApi.ts
│   │   │   ├── useDebounce.ts
│   │   │   ├── useLocalStorage.ts
│   │   │   └── useTheme.ts
│   │   │
│   │   ├── utils/                      # Utility functions
│   │   │   ├── formatters.ts
│   │   │   ├── validators.ts
│   │   │   └── dateUtils.ts
│   │   │
│   │   ├── api/                        # API client
│   │   │   ├── apiClient.ts            # Axios instance
│   │   │   ├── endpoints.ts            # API endpoint constants
│   │   │   └── types.ts                # Shared API types
│   │   │
│   │   └── constants/                  # Application constants
│   │       ├── routes.ts
│   │       └── config.ts
│   │
│   ├── styles/                         # Global styles
│   │   ├── global.css
│   │   └── theme.ts                    # Fluent UI theme customization
│   │
│   ├── contexts/                       # React contexts
│   │   └── ThemeContext.tsx            # Theme provider for dark mode
│   │
│   └── tests/                          # Test utilities
│       ├── setup.ts
│       ├── mocks/
│       │   └── apiMocks.ts
│       └── helpers/
│           └── renderWithProviders.tsx
│
├── e2e/                                # Playwright E2E tests
│   ├── dashboard.spec.ts
│   ├── metrics.spec.ts
│   └── sources.spec.ts
│
├── .env.development                    # Development environment variables
├── .env.production                     # Production environment variables
├── .eslintrc.cjs                       # ESLint configuration
├── .prettierrc                         # Prettier configuration
├── tsconfig.json                       # TypeScript configuration
├── vite.config.ts                      # Vite configuration
├── vitest.config.ts                    # Vitest configuration
├── playwright.config.ts                # Playwright configuration
├── package.json
└── README.md
```

---

## 3. Design System Preservation

### 3.1 Current Design Analysis

The existing dashboard features a **dark cyberpunk aesthetic** with the following key characteristics:

#### Color Palette (CSS Custom Properties to Replicate)

```css
:root {
  /* Primary Colors */
  --color-primary: #00d4ff;           /* Cyan - main accent */
  --color-primary-dark: #00a9cc;
  --color-secondary: #7c3aed;         /* Purple - secondary accent */
  --color-accent: #f472b6;            /* Pink accent */

  /* Oura Theme */
  --color-oura: #00a99d;              /* Teal */
  --color-oura-light: #00d4aa;

  /* Food Theme */
  --color-food: #f97316;              /* Orange */
  --color-food-light: #fb923c;

  /* Backgrounds */
  --bg-dark: #1a1a2e;
  --bg-darker: #16213e;
  --bg-card: rgba(255, 255, 255, 0.05);      /* Glass morphism */
  --bg-card-hover: rgba(255, 255, 255, 0.08);

  /* Text Colors */
  --text-primary: #e0e0e0;
  --text-secondary: #888;
  --text-muted: #666;

  /* Status Colors */
  --color-success: #22c55e;
  --color-warning: #f59e0b;
  --color-error: #ef4444;
  --color-info: #3b82f6;

  /* Chart Colors */
  --chart-sleep: #3b82f6;
  --chart-readiness: #22c55e;
  --chart-activity: #f59e0b;
  --chart-weight: #00d4ff;
  --chart-body-fat: #f472b6;
  --chart-muscle: #34d399;

  /* Spacing */
  --spacing-xs: 5px;
  --spacing-sm: 10px;
  --spacing-md: 15px;
  --spacing-lg: 20px;
  --spacing-xl: 30px;
  --spacing-2xl: 40px;

  /* Border Radius */
  --radius-sm: 8px;
  --radius-md: 12px;
  --radius-lg: 15px;
  --radius-xl: 20px;

  /* Transitions */
  --transition-fast: 0.2s ease;
  --transition-normal: 0.3s ease;

  /* Shadows */
  --shadow-card: 0 4px 20px rgba(0, 0, 0, 0.3);
  --shadow-hover: 0 8px 30px rgba(0, 0, 0, 0.4);

  /* Font */
  --font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
}
```

#### Key Visual Features

1. **Gradient Background**: `linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)`
2. **Glass Morphism Cards**: `background: rgba(255, 255, 255, 0.05)` with backdrop blur
3. **Gradient Text Effects**: Using `-webkit-background-clip: text` for metric values
4. **Colored Top Borders**: 4px solid borders on specific cards (weight, sleep, readiness, etc.)
5. **Hover Animations**: `transform: translateY(-2px)` with shadow transitions
6. **Tab Navigation**: Active tabs with gradient backgrounds matching theme
7. **Modal Overlays**: Dark overlays (`rgba(0, 0, 0, 0.8)`) with gradient headers

### 3.2 Fluent UI Theme Override Configuration

**CRITICAL**: Fluent UI React v9 must be configured to match the current design system, not replace it. Use custom themes that override Fluent UI defaults:

```typescript
// src/styles/healthAggregatorTheme.ts
import {
  createDarkTheme,
  createLightTheme,
  BrandVariants,
  Theme
} from '@fluentui/react-components';

// Health Aggregator Brand Colors (matching current design)
const healthBrand: BrandVariants = {
  10: '#020305',
  20: '#0F1B2E',
  30: '#1A2F52',
  40: '#16213e',     // --bg-darker
  50: '#1a1a2e',     // --bg-dark
  60: '#00a9cc',     // --color-primary-dark
  70: '#00d4ff',     // --color-primary (MAIN)
  80: '#33ddff',
  90: '#66e6ff',
  100: '#99eeff',
  110: '#ccf6ff',
  120: '#e6faff',
  130: '#f0fcff',
  140: '#f8feff',
  150: '#fcfeff',
  160: '#FFFFFF',
};

export const healthDarkTheme: Theme = {
  ...createDarkTheme(healthBrand),
  // Override specific tokens
  colorBrandForeground1: '#00d4ff',              // Primary cyan
  colorBrandForeground2: '#7c3aed',              // Secondary purple
  colorNeutralBackground1: '#1a1a2e',            // Main background
  colorNeutralBackground2: 'rgba(255,255,255,0.05)', // Card background
  colorNeutralForeground1: '#e0e0e0',            // Primary text
  colorNeutralForeground2: '#888',               // Secondary text
  colorNeutralForeground3: '#666',               // Muted text

  // Status colors matching current design
  colorPaletteGreenForeground1: '#22c55e',       // Success
  colorPaletteYellowForeground1: '#f59e0b',      // Warning
  colorPaletteRedForeground1: '#ef4444',         // Error

  // Border radius tokens
  borderRadiusMedium: '12px',
  borderRadiusLarge: '15px',
  borderRadiusXLarge: '20px',

  // Font family
  fontFamilyBase: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen, Ubuntu, sans-serif',
};

// Light theme (if needed, but current design is dark-only)
export const healthLightTheme = createLightTheme(healthBrand);
```

### 3.3 CSS Modules for Custom Styling

Since Fluent UI cannot replicate ALL design features (gradients, glass morphism, animations), use CSS Modules for component-specific styles:

```typescript
// Example: src/features/dashboard/components/MetricCard.module.css
.metricCard {
  background: var(--bg-card);
  border-radius: var(--radius-xl);
  padding: var(--spacing-lg);
  border: 1px solid rgba(255, 255, 255, 0.05);
  position: relative;
  overflow: hidden;
  transition: var(--transition-normal);
}

.metricCard:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-hover);
  background: var(--bg-card-hover);
}

/* Colored top border variants */
.metricCard.weight::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 4px;
  background: linear-gradient(90deg, var(--color-primary), var(--color-secondary));
}

.metricCard.sleep::before {
  background: linear-gradient(90deg, #3b82f6, #1d4ed8);
}

.metricCard.readiness::before {
  background: linear-gradient(90deg, var(--color-success), #16a34a);
}

/* Gradient text for metric values */
.metricValue {
  font-size: 3rem;
  font-weight: 700;
  margin-bottom: var(--spacing-xs);
  background: linear-gradient(90deg, var(--color-primary), var(--color-secondary));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.metricValue.oura {
  background: linear-gradient(90deg, var(--color-oura), var(--color-oura-light));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}

.metricValue.food {
  background: linear-gradient(90deg, #f97316, #fb923c);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
}
```

### 3.4 Global CSS Setup

```typescript
// src/styles/global.css
/* Import CSS custom properties */
@import './design-tokens.css';

/* Global resets */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: var(--font-family);
  background: linear-gradient(135deg, var(--bg-dark) 0%, var(--bg-darker) 100%);
  min-height: 100vh;
  color: var(--text-primary);
}

/* Glass morphism utility class */
.glass-card {
  background: rgba(255, 255, 255, 0.05);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.1);
}

/* Gradient text utility */
.gradient-text-primary {
  background: linear-gradient(90deg, var(--color-primary), var(--color-secondary));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.gradient-text-oura {
  background: linear-gradient(90deg, var(--color-oura), var(--color-oura-light));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.gradient-text-food {
  background: linear-gradient(90deg, #f97316, #fb923c);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}
```

### 3.5 Chart.js Configuration Matching Current Implementation

```typescript
// src/shared/utils/chartConfig.ts
import { ChartOptions } from 'chart.js';

export const createChartConfig = (isDark = true): Partial<ChartOptions> => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      display: true,
      position: 'bottom',
      labels: {
        color: isDark ? '#e0e0e0' : '#333',
        padding: 15,
        font: {
          family: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
          size: 12,
        },
        usePointStyle: true,
      },
    },
    tooltip: {
      backgroundColor: 'rgba(0, 0, 0, 0.8)',
      titleColor: '#00d4ff',
      bodyColor: '#e0e0e0',
      borderColor: 'rgba(0, 212, 255, 0.3)',
      borderWidth: 1,
      padding: 12,
      cornerRadius: 8,
      displayColors: true,
      callbacks: {
        // Custom tooltip formatting matching current design
      },
    },
  },
  scales: {
    x: {
      grid: {
        color: 'rgba(255, 255, 255, 0.05)',
        drawBorder: false,
      },
      ticks: {
        color: '#888',
        font: { size: 11 },
      },
    },
    y: {
      grid: {
        color: 'rgba(255, 255, 255, 0.05)',
        drawBorder: false,
      },
      ticks: {
        color: '#888',
        font: { size: 11 },
      },
    },
  },
});

// Chart color schemes matching current implementation
export const CHART_COLORS = {
  sleep: '#3b82f6',
  readiness: '#22c55e',
  activity: '#f59e0b',
  weight: '#00d4ff',
  bodyFat: '#f472b6',
  muscle: '#34d399',
  // ... more color definitions
};
```

---

## 4. UI Component Mapping (Current → React)

### 4.1 Component Mapping Table

| Current HTML Component | React Component | Styling Approach | Key Features |
|------------------------|-----------------|------------------|--------------|
| Tab navigation (`div.tab-nav`) | `<TabNavigation />` | CSS Module + Fluent Tabs (customized) | Gradient active state, icons |
| Metric cards (`div.metric-card`) | `<MetricCard />` | CSS Module | Colored top border, gradient text, hover animation |
| Stats grid (`div.stats-grid`) | `<StatsGrid />` | CSS Module | Responsive grid, auto-fit columns |
| Chart containers | `<ChartContainer />` | CSS Module + Chart.js | Toggle controls, time range selector |
| Data tables | `<DataTable />` | Fluent DataGrid (customized) | Pagination, clickable rows, hover effects |
| Modals (sleep, activity, food) | `<Modal />` | CSS Module + Fluent Dialog | Dark overlay, gradient headers, glass card body |
| Settings modal | `<SettingsModal />` | CSS Module + Fluent Dialog | Form inputs, preset buttons |
| Status badges | `<StatusBadge />` | CSS Module | Color-coded (success/warning/error) |
| Progress bars | `<ProgressBar />` | CSS Module | Gradient fills, smooth animations |
| Collapsible sections | `<CollapsibleSection />` | CSS Module | Arrow rotation, max-height transition |

### 4.2 Detailed Component Examples

#### TabNavigation Component

```typescript
// src/shared/components/Navigation/TabNavigation.tsx
import React from 'react';
import { Tab, TabList } from '@fluentui/react-components';
import styles from './TabNavigation.module.css';

interface TabConfig {
  id: string;
  label: string;
  icon: string;
  themeClass?: 'oura' | 'food' | 'dashboard';
}

interface TabNavigationProps {
  tabs: TabConfig[];
  activeTab: string;
  onTabChange: (tabId: string) => void;
}

export const TabNavigation: React.FC<TabNavigationProps> = ({
  tabs,
  activeTab,
  onTabChange,
}) => {
  return (
    <div className={styles.tabNav}>
      <TabList
        selectedValue={activeTab}
        onTabSelect={(_, data) => onTabChange(data.value as string)}
        className={styles.tabList}
      >
        {tabs.map((tab) => (
          <Tab
            key={tab.id}
            value={tab.id}
            className={`${styles.tabBtn} ${tab.themeClass ? styles[tab.themeClass] : ''}`}
            icon={<span className={styles.tabIcon}>{tab.icon}</span>}
          >
            <span className={styles.tabText}>{tab.label}</span>
          </Tab>
        ))}
      </TabList>
    </div>
  );
};
```

```css
/* src/shared/components/Navigation/TabNavigation.module.css */
.tabNav {
  display: flex;
  gap: var(--spacing-sm);
  margin-bottom: var(--spacing-xl);
  background: var(--bg-card);
  border-radius: var(--radius-lg);
  padding: var(--spacing-sm);
}

.tabList {
  display: flex;
  width: 100%;
  gap: var(--spacing-sm);
}

.tabBtn {
  flex: 1;
  padding: var(--spacing-md) var(--spacing-xl);
  border: none;
  border-radius: var(--radius-md);
  cursor: pointer;
  font-size: 1rem;
  font-weight: 600;
  background: transparent;
  color: var(--text-secondary);
  transition: var(--transition-normal);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-sm);
}

.tabBtn:hover {
  background: var(--bg-card);
  color: var(--text-primary);
}

.tabBtn[aria-selected="true"] {
  background: linear-gradient(135deg, var(--color-secondary), var(--color-primary));
  color: white;
}

.tabBtn.oura[aria-selected="true"] {
  background: linear-gradient(135deg, var(--color-oura), var(--color-oura-light));
}

.tabBtn.food[aria-selected="true"] {
  background: linear-gradient(135deg, #f97316, #fb923c);
}

.tabBtn.dashboard[aria-selected="true"] {
  background: linear-gradient(135deg, #f59e0b, #ef4444);
}

.tabIcon {
  font-size: 1.3rem;
}

@media (max-width: 768px) {
  .tabText {
    display: none;
  }

  .tabBtn {
    padding: var(--spacing-sm) var(--spacing-md);
    font-size: 0.9rem;
  }
}
```

#### MetricCard Component

```typescript
// src/shared/components/Cards/MetricCard.tsx
import React from 'react';
import { Card } from '@fluentui/react-components';
import styles from './MetricCard.module.css';

interface MetricCardProps {
  title: string;
  value: number | string | null;
  unit?: string;
  icon: string;
  source?: string;
  date?: string;
  variant?: 'weight' | 'sleep' | 'readiness' | 'bodyScore' | 'calories' | 'protein' | 'carbs' | 'fat';
  progress?: {
    label: string;
    value: string;
    absValue?: string;
    type: 'positive' | 'negative' | 'neutral';
  }[];
  onClick?: () => void;
  className?: string;
}

export const MetricCard: React.FC<MetricCardProps> = ({
  title,
  value,
  unit,
  icon,
  source,
  date,
  variant = 'weight',
  progress,
  onClick,
  className,
}) => {
  const cardClasses = [
    styles.metricCard,
    styles[variant],
    onClick && styles.clickable,
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <Card className={cardClasses} onClick={onClick}>
      <div className={styles.metricHeader}>
        <div className={styles.metricTitle}>
          <span className={styles.icon}>{icon}</span>
          <h3>{title}</h3>
        </div>
        {source && <span className={styles.metricSource}>{source}</span>}
      </div>

      <div className={`${styles.metricValue} ${styles[variant]}`}>
        {value !== null ? value : '--'}
        {unit && <span className={styles.metricUnit}> {unit}</span>}
      </div>

      {date && <div className={styles.metricDate}>{date}</div>}

      {progress && progress.length > 0 && (
        <div className={styles.progressSection}>
          <h4>Progress</h4>
          <div className={styles.progressGrid}>
            {progress.map((item, index) => (
              <div key={index} className={styles.progressItem}>
                <div className={styles.label}>{item.label}</div>
                <div className={`${styles.change} ${styles[item.type]}`}>
                  {item.value}
                </div>
                {item.absValue && (
                  <div className={styles.absValue}>{item.absValue}</div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </Card>
  );
};
```

#### Modal Component with Gradient Header

```typescript
// src/shared/components/Modal/Modal.tsx
import React from 'react';
import { Dialog, DialogSurface, DialogBody } from '@fluentui/react-components';
import styles from './Modal.module.css';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  variant?: 'primary' | 'oura' | 'food';
  maxWidth?: string;
}

export const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  subtitle,
  children,
  variant = 'primary',
  maxWidth = '800px',
}) => {
  return (
    <Dialog open={isOpen} onOpenChange={(_, data) => !data.open && onClose()}>
      <div className={styles.modalOverlay}>
        <DialogSurface
          className={styles.modal}
          style={{ maxWidth }}
        >
          <div className={`${styles.modalHeader} ${styles[variant]}`}>
            <div>
              <h2 className={`${styles.modalTitle} ${styles[variant]}`}>{title}</h2>
              {subtitle && <p className={styles.modalSubtitle}>{subtitle}</p>}
            </div>
            <button
              type="button"
              className={styles.modalClose}
              onClick={onClose}
              aria-label="Close modal"
            >
              ✕
            </button>
          </div>
          <DialogBody className={styles.modalBody}>
            {children}
          </DialogBody>
        </DialogSurface>
      </div>
    </Dialog>
  );
};
```

```css
/* src/shared/components/Modal/Modal.module.css */
.modalOverlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.8);
  z-index: 1000;
  display: flex;
  justify-content: center;
  align-items: center;
  padding: var(--spacing-lg);
}

.modal {
  background: linear-gradient(135deg, var(--bg-dark) 0%, var(--bg-darker) 100%);
  border-radius: var(--radius-xl);
  max-height: 90vh;
  overflow-y: auto;
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 25px 50px rgba(0, 0, 0, 0.5);
}

.modalHeader {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg) var(--spacing-xl);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: var(--radius-xl) var(--radius-xl) 0 0;
}

.modalHeader.primary {
  background: linear-gradient(135deg, rgba(124, 58, 237, 0.2) 0%, rgba(0, 212, 255, 0.1) 100%);
}

.modalHeader.oura {
  background: linear-gradient(135deg, rgba(0, 169, 157, 0.2) 0%, rgba(0, 212, 170, 0.1) 100%);
}

.modalHeader.food {
  background: linear-gradient(135deg, rgba(249, 115, 22, 0.2) 0%, rgba(251, 146, 60, 0.1) 100%);
}

.modalTitle {
  margin: 0;
  font-size: 1.5rem;
  margin-bottom: var(--spacing-xs);
}

.modalTitle.primary {
  background: linear-gradient(90deg, var(--color-primary), var(--color-secondary));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.modalTitle.oura {
  background: linear-gradient(90deg, var(--color-oura), var(--color-oura-light));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.modalTitle.food {
  background: linear-gradient(90deg, #f97316, #fb923c);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.modalClose {
  background: rgba(255, 255, 255, 0.1);
  border: none;
  color: var(--text-primary);
  font-size: 1.8rem;
  cursor: pointer;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: var(--transition-normal);
  line-height: 1;
}

.modalClose:hover {
  background: rgba(255, 255, 255, 0.2);
  transform: rotate(90deg);
}

.modalBody {
  padding: var(--spacing-xl);
}
```

---

## 5. TypeScript Configuration

### 3.1 tsconfig.json (Strict Mode)

```json
{
  "compilerOptions": {
    // Type Checking
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "noImplicitOverride": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,

    // Module Resolution
    "target": "ES2020",
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "moduleResolution": "bundler",
    "resolveJsonModule": true,
    "allowImportingTsExtensions": true,

    // Interop Constraints
    "allowSyntheticDefaultImports": true,
    "esModuleInterop": true,
    "forceConsistentCasingInFileNames": true,
    "isolatedModules": true,

    // Emit
    "declaration": true,
    "declarationMap": true,
    "sourceMap": true,
    "noEmit": true,

    // JSX
    "jsx": "react-jsx",
    "jsxImportSource": "react",

    // Path Mapping
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"],
      "@features/*": ["./src/features/*"],
      "@shared/*": ["./src/shared/*"]
    }
  },
  "include": ["src"],
  "exclude": ["node_modules", "dist", "build"]
}
```

---

## 4. API Client Layer with TypeScript

### 4.1 Axios Client Configuration

```typescript
// src/shared/api/apiClient.ts
import axios, { AxiosError, AxiosRequestConfig, AxiosResponse } from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor
apiClient.interceptors.request.use(
  (config) => {
    // Add auth token if available
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

// Response interceptor
apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Handle unauthorized
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// Generic API request helper with type safety
export async function apiRequest<T>(config: AxiosRequestConfig): Promise<T> {
  try {
    const response = await apiClient.request<T>(config);
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error)) {
      throw new Error(error.response?.data?.message || error.message);
    }
    throw error;
  }
}
```

### 4.2 API Endpoints

```typescript
// src/shared/api/endpoints.ts
export const API_ENDPOINTS = {
  // Metrics
  METRICS_LATEST: '/metrics/latest',
  METRICS_RANGE: '/metrics/range',
  METRIC_HISTORY: (type: string) => `/metrics/${type}/history`,

  // Sources
  SOURCES: '/sources',
  SOURCE_STATUS: (id: number) => `/sources/${id}/status`,

  // Dashboard
  DASHBOARD_SUMMARY: '/dashboard/summary',
} as const;
```

### 4.3 Shared API Types

```typescript
// src/shared/api/types.ts
export interface ApiResponse<T> {
  data: T;
  message?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ApiError {
  message: string;
  code?: string;
  details?: Record<string, string[]>;
}
```

---

## 5. Feature Implementation Example: Dashboard

### 5.1 TypeScript Types

```typescript
// src/features/dashboard/types/dashboard.types.ts
export interface DashboardSummary {
  sleepScore: number | null;
  readinessScore: number | null;
  activityScore: number | null;
  steps: number | null;
  weight: number | null;
  caloriesBurned: number | null;
  lastUpdated: string;
}

export interface MetricTile {
  title: string;
  value: number | string | null;
  unit: string;
  icon: string;
  trend?: 'up' | 'down' | 'neutral';
  trendValue?: number;
}
```

### 5.2 Service Layer

```typescript
// src/features/dashboard/services/dashboardService.ts
import { apiClient } from '@shared/api/apiClient';
import { API_ENDPOINTS } from '@shared/api/endpoints';
import type { DashboardSummary } from '../types/dashboard.types';

export const dashboardService = {
  async getSummary(): Promise<DashboardSummary> {
    const response = await apiClient.get<DashboardSummary>(
      API_ENDPOINTS.DASHBOARD_SUMMARY
    );
    return response.data;
  },
};
```

### 5.3 Custom Hook with TanStack Query

```typescript
// src/features/dashboard/hooks/useDashboardData.ts
import { useQuery, UseQueryResult } from '@tanstack/react-query';
import { dashboardService } from '../services/dashboardService';
import type { DashboardSummary } from '../types/dashboard.types';

export const useDashboardData = (): UseQueryResult<DashboardSummary, Error> => {
  return useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: dashboardService.getSummary,
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes (formerly cacheTime)
    refetchOnWindowFocus: true,
    retry: 3,
  });
};
```

### 5.4 Dashboard Component

```typescript
// src/features/dashboard/DashboardPage.tsx
import React from 'react';
import {
  makeStyles,
  shorthands,
  Spinner,
  Text,
  Card,
  CardHeader,
} from '@fluentui/react-components';
import { useDashboardData } from './hooks/useDashboardData';
import { DashboardCard } from './components/DashboardCard';
import { ErrorMessage } from '@shared/components/ErrorMessage';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap('24px'),
    ...shorthands.padding('24px'),
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
    ...shorthands.gap('16px'),
  },
  header: {
    marginBottom: '16px',
  },
});

export const DashboardPage: React.FC = () => {
  const styles = useStyles();
  const { data, isLoading, error } = useDashboardData();

  if (isLoading) {
    return (
      <div className={styles.container}>
        <Spinner label="Loading dashboard..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <ErrorMessage error={error.message} />
      </div>
    );
  }

  if (!data) {
    return null;
  }

  return (
    <div className={styles.container}>
      <Text as="h1" size={900} weight="semibold" className={styles.header}>
        Health Dashboard
      </Text>

      <div className={styles.grid}>
        <DashboardCard
          title="Sleep Score"
          value={data.sleepScore}
          unit="score"
          icon="bed"
        />
        <DashboardCard
          title="Readiness"
          value={data.readinessScore}
          unit="score"
          icon="checkmark"
        />
        <DashboardCard
          title="Activity Score"
          value={data.activityScore}
          unit="score"
          icon="running"
        />
        <DashboardCard
          title="Steps"
          value={data.steps}
          unit="steps"
          icon="walk"
        />
        <DashboardCard
          title="Weight"
          value={data.weight}
          unit="kg"
          icon="scale"
        />
        <DashboardCard
          title="Calories Burned"
          value={data.caloriesBurned}
          unit="kcal"
          icon="flame"
        />
      </div>

      <Text size={200} className={styles.header}>
        Last updated: {new Date(data.lastUpdated).toLocaleString()}
      </Text>
    </div>
  );
};
```

### 5.5 Dashboard Card Component

```typescript
// src/features/dashboard/components/DashboardCard.tsx
import React from 'react';
import {
  Card,
  CardHeader,
  Text,
  makeStyles,
  shorthands,
  tokens,
} from '@fluentui/react-components';

const useStyles = makeStyles({
  card: {
    ...shorthands.padding('16px'),
    minHeight: '140px',
  },
  value: {
    fontSize: tokens.fontSizeHero900,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorBrandForeground1,
  },
  unit: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground3,
    marginLeft: '8px',
  },
});

interface DashboardCardProps {
  title: string;
  value: number | null;
  unit: string;
  icon?: string;
}

export const DashboardCard: React.FC<DashboardCardProps> = ({
  title,
  value,
  unit,
  icon,
}) => {
  const styles = useStyles();

  return (
    <Card className={styles.card}>
      <CardHeader header={<Text weight="semibold">{title}</Text>} />
      <div>
        <Text className={styles.value}>
          {value !== null ? value.toLocaleString() : '--'}
        </Text>
        <Text className={styles.unit}>{unit}</Text>
      </div>
    </Card>
  );
};
```

---

## 6. State Management Strategy

### 6.1 TanStack Query Setup with Theme Support

```typescript
// src/main.tsx
import React from 'react';
import ReactDOM from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { ThemeProvider } from './contexts/ThemeContext';
import App from './App';
import './styles/global.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 10 * 60 * 1000, // 10 minutes
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <App />
        <ReactQueryDevtools initialIsOpen={false} />
      </ThemeProvider>
    </QueryClientProvider>
  </React.StrictMode>
);
```

### 6.2 Local State with React Hooks

```typescript
// src/features/metrics/hooks/useMetricFilter.ts
import { useState, useMemo } from 'react';

interface MetricFilter {
  category: string | null;
  dateRange: { from: Date; to: Date } | null;
}

export const useMetricFilter = () => {
  const [filter, setFilter] = useState<MetricFilter>({
    category: null,
    dateRange: null,
  });

  const setCategory = (category: string | null) => {
    setFilter((prev) => ({ ...prev, category }));
  };

  const setDateRange = (from: Date, to: Date) => {
    setFilter((prev) => ({ ...prev, dateRange: { from, to } }));
  };

  const clearFilters = () => {
    setFilter({ category: null, dateRange: null });
  };

  return {
    filter,
    setCategory,
    setDateRange,
    clearFilters,
  };
};
```

---

## 7. Routing with React Router

### 7.1 Routes Configuration

```typescript
// src/App.tsx
import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from '@shared/components/Layout/AppLayout';
import { DashboardPage } from '@features/dashboard/DashboardPage';
import { MetricsPage } from '@features/metrics/MetricsPage';
import { SourcesPage } from '@features/sources/SourcesPage';
import { SettingsPage } from '@features/settings/SettingsPage';
import { ErrorBoundary } from '@shared/components/ErrorBoundary';

const App: React.FC = () => {
  return (
    <ErrorBoundary>
      <BrowserRouter>
        <AppLayout>
          <Routes>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/metrics" element={<MetricsPage />} />
            <Route path="/sources" element={<SourcesPage />} />
            <Route path="/settings" element={<SettingsPage />} />
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </AppLayout>
      </BrowserRouter>
    </ErrorBoundary>
  );
};

export default App;
```

### 7.2 Route Constants

```typescript
// src/shared/constants/routes.ts
export const ROUTES = {
  DASHBOARD: '/dashboard',
  METRICS: '/metrics',
  SOURCES: '/sources',
  SETTINGS: '/settings',
} as const;

export type RouteKey = keyof typeof ROUTES;
```

---

## 8. Fluent UI React v9 Components

### 8.1 Path-Based Imports (Bundle Optimization)

```typescript
// GOOD: Path-based imports (tree-shakeable)
import { Button } from '@fluentui/react-components/unstable';
import { makeStyles } from '@fluentui/react-components';

// BAD: Barrel imports (larger bundle)
import { Button, makeStyles } from '@fluentui/react-components';
```

### 8.2 Theme Customization

```typescript
// src/styles/theme.ts
import { createLightTheme, createDarkTheme, BrandVariants } from '@fluentui/react-components';

const healthBrand: BrandVariants = {
  10: '#020305',
  20: '#0F1B2E',
  30: '#1A2F52',
  40: '#234276',
  50: '#2C559C',
  60: '#3369C2',
  70: '#5285D0',
  80: '#74A1DE',
  90: '#96BDEB',
  100: '#B8D9F8',
  110: '#D0E7FF',
  120: '#E8F3FF',
  130: '#F5F9FF',
  140: '#FAFCFF',
  150: '#FDFDFE',
  160: '#FFFFFF',
};

export const lightTheme = createLightTheme(healthBrand);
export const darkTheme = createDarkTheme(healthBrand);
```

### 8.3 Theme Context for Dark Mode

```typescript
// src/contexts/ThemeContext.tsx
import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { FluentProvider, Theme } from '@fluentui/react-components';
import { lightTheme, darkTheme } from '@/styles/theme';

interface ThemeContextType {
  isDarkMode: boolean;
  toggleTheme: () => void;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return context;
};

interface ThemeProviderProps {
  children: ReactNode;
}

export const ThemeProvider: React.FC<ThemeProviderProps> = ({ children }) => {
  // Check system preference and local storage
  const getInitialTheme = (): boolean => {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme) {
      return savedTheme === 'dark';
    }
    // Default to system preference
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  };

  const [isDarkMode, setIsDarkMode] = useState(getInitialTheme);

  useEffect(() => {
    // Save preference to localStorage
    localStorage.setItem('theme', isDarkMode ? 'dark' : 'light');
  }, [isDarkMode]);

  const toggleTheme = () => {
    setIsDarkMode((prev) => !prev);
  };

  const currentTheme: Theme = isDarkMode ? darkTheme : lightTheme;

  return (
    <ThemeContext.Provider value={{ isDarkMode, toggleTheme }}>
      <FluentProvider theme={currentTheme}>
        {children}
      </FluentProvider>
    </ThemeContext.Provider>
  );
};
```

### 8.4 Theme Toggle Component

```typescript
// src/shared/components/Layout/ThemeToggle.tsx
import React from 'react';
import { Button, makeStyles, tokens } from '@fluentui/react-components';
import { WeatherMoon24Regular, WeatherSunny24Regular } from '@fluentui/react-icons';
import { useTheme } from '@/contexts/ThemeContext';

const useStyles = makeStyles({
  button: {
    minWidth: '40px',
  },
});

export const ThemeToggle: React.FC = () => {
  const styles = useStyles();
  const { isDarkMode, toggleTheme } = useTheme();

  return (
    <Button
      appearance="subtle"
      className={styles.button}
      icon={isDarkMode ? <WeatherSunny24Regular /> : <WeatherMoon24Regular />}
      onClick={toggleTheme}
      aria-label={isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'}
      title={isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'}
    />
  );
};
```

### 8.5 Layout Component with Fluent UI and Theme Toggle

```typescript
// src/shared/components/Layout/AppLayout.tsx
import React from 'react';
import { makeStyles, shorthands } from '@fluentui/react-components';
import { AppHeader } from './AppHeader';
import { AppSidebar } from './AppSidebar';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
  },
  main: {
    display: 'flex',
    flex: 1,
    overflow: 'hidden',
  },
  content: {
    flex: 1,
    overflowY: 'auto',
    ...shorthands.padding('24px'),
  },
});

interface AppLayoutProps {
  children: React.ReactNode;
}

export const AppLayout: React.FC<AppLayoutProps> = ({ children }) => {
  const styles = useStyles();

  return (
    <div className={styles.root}>
      <AppHeader />
      <div className={styles.main}>
        <AppSidebar />
        <main className={styles.content}>{children}</main>
      </div>
    </div>
  );
};
```

---

## 9. Performance Optimization

### 9.1 Code Splitting with React.lazy

```typescript
// src/App.tsx
import React, { Suspense, lazy } from 'react';
import { Spinner } from '@fluentui/react-components';

// Lazy load feature pages
const DashboardPage = lazy(() => import('@features/dashboard/DashboardPage'));
const MetricsPage = lazy(() => import('@features/metrics/MetricsPage'));
const SourcesPage = lazy(() => import('@features/sources/SourcesPage'));

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <AppLayout>
        <Suspense fallback={<Spinner label="Loading..." />}>
          <Routes>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/metrics" element={<MetricsPage />} />
            <Route path="/sources" element={<SourcesPage />} />
          </Routes>
        </Suspense>
      </AppLayout>
    </BrowserRouter>
  );
};
```

### 9.2 Vite Configuration for Optimization

```typescript
// vite.config.ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@features': path.resolve(__dirname, './src/features'),
      '@shared': path.resolve(__dirname, './src/shared'),
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'vendor-react': ['react', 'react-dom', 'react-router-dom'],
          'vendor-query': ['@tanstack/react-query'],
          'vendor-fluent': ['@fluentui/react-components'],
          'vendor-http': ['axios'],
        },
      },
    },
    chunkSizeWarningLimit: 1000,
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
});
```

### 9.3 React.memo for Expensive Components

```typescript
// src/features/metrics/components/MetricHistoryChart.tsx
import React, { memo } from 'react';

interface MetricHistoryChartProps {
  data: Array<{ timestamp: string; value: number }>;
  metricType: string;
}

export const MetricHistoryChart = memo<MetricHistoryChartProps>(
  ({ data, metricType }) => {
    // Expensive chart rendering logic
    return <div>{/* Chart implementation */}</div>;
  },
  (prevProps, nextProps) => {
    // Custom comparison function
    return (
      prevProps.data === nextProps.data &&
      prevProps.metricType === nextProps.metricType
    );
  }
);

MetricHistoryChart.displayName = 'MetricHistoryChart';
```

---

## 10. Testing Strategy

### 10.1 Vitest Configuration

```typescript
// vitest.config.ts
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/tests/setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: ['node_modules/', 'src/tests/'],
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@features': path.resolve(__dirname, './src/features'),
      '@shared': path.resolve(__dirname, './src/shared'),
    },
  },
});
```

### 10.2 Test Setup

```typescript
// src/tests/setup.ts
import '@testing-library/jest-dom';
import { cleanup } from '@testing-library/react';
import { afterEach, vi } from 'vitest';

// Cleanup after each test
afterEach(() => {
  cleanup();
});

// Mock window.matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});
```

### 10.3 Component Unit Test Example

```typescript
// src/features/dashboard/components/DashboardCard.test.tsx
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { DashboardCard } from './DashboardCard';

const renderWithProviders = (ui: React.ReactElement) => {
  return render(<FluentProvider theme={webLightTheme}>{ui}</FluentProvider>);
};

describe('DashboardCard', () => {
  it('renders title and value correctly', () => {
    renderWithProviders(
      <DashboardCard title="Sleep Score" value={85} unit="score" />
    );

    expect(screen.getByText('Sleep Score')).toBeInTheDocument();
    expect(screen.getByText('85')).toBeInTheDocument();
    expect(screen.getByText('score')).toBeInTheDocument();
  });

  it('displays placeholder when value is null', () => {
    renderWithProviders(
      <DashboardCard title="Sleep Score" value={null} unit="score" />
    );

    expect(screen.getByText('--')).toBeInTheDocument();
  });
});
```

### 10.4 Playwright E2E Test Example

```typescript
// e2e/dashboard.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Dashboard Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:3000/dashboard');
  });

  test('displays dashboard cards', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Health Dashboard' })).toBeVisible();

    // Check for dashboard cards
    await expect(page.getByText('Sleep Score')).toBeVisible();
    await expect(page.getByText('Readiness')).toBeVisible();
    await expect(page.getByText('Activity Score')).toBeVisible();
  });

  test('loads data from API', async ({ page }) => {
    // Wait for API call to complete
    await page.waitForResponse((response) =>
      response.url().includes('/api/dashboard/summary')
    );

    // Verify data is displayed
    const sleepScore = page.locator('[data-testid="sleep-score-value"]');
    await expect(sleepScore).not.toHaveText('--');
  });
});
```

---

## 11. Azure Static Web Apps Configuration

### 11.1 staticwebapp.config.json

```json
{
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["anonymous"]
    },
    {
      "route": "/*",
      "serve": "/index.html",
      "statusCode": 200
    }
  ],
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/images/*.{png,jpg,gif}", "/css/*"]
  },
  "responseOverrides": {
    "404": {
      "rewrite": "/index.html",
      "statusCode": 200
    }
  },
  "globalHeaders": {
    "content-security-policy": "default-src 'self' https://*.azure.com; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';"
  }
}
```

### 11.2 Environment Variables

```bash
# .env.development
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_INSIGHTS_KEY=

# .env.production
VITE_API_BASE_URL=https://healthaggregator-api.azurewebsites.net/api
VITE_APP_INSIGHTS_KEY=your-app-insights-key
```

---

## 12. Shared Utilities

### 12.1 Date Formatting

```typescript
// src/shared/utils/dateUtils.ts
import { format, formatDistanceToNow } from 'date-fns';

export const formatDate = (date: string | Date): string => {
  return format(new Date(date), 'MMM d, yyyy');
};

export const formatDateTime = (date: string | Date): string => {
  return format(new Date(date), 'MMM d, yyyy HH:mm');
};

export const formatRelativeTime = (date: string | Date): string => {
  return formatDistanceToNow(new Date(date), { addSuffix: true });
};
```

### 12.2 Number Formatting

```typescript
// src/shared/utils/formatters.ts
export const formatNumber = (value: number | null, decimals = 0): string => {
  if (value === null) return '--';
  return value.toLocaleString(undefined, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  });
};

export const formatPercentage = (value: number | null): string => {
  if (value === null) return '--';
  return `${value.toFixed(1)}%`;
};

export const formatWeight = (kg: number | null): string => {
  if (kg === null) return '--';
  return `${kg.toFixed(1)} kg`;
};
```

### 12.3 Custom Hooks

```typescript
// src/shared/hooks/useDebounce.ts
import { useState, useEffect } from 'react';

export function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}

// Usage
const [searchTerm, setSearchTerm] = useState('');
const debouncedSearchTerm = useDebounce(searchTerm, 500);
```

---

## 13. Component Structure Summary

The React application will have the following component hierarchy:

```
App
├── ThemeProvider (Fluent UI + Custom Theme)
├── QueryClientProvider (TanStack Query)
├── BrowserRouter
│   └── AppLayout
│       ├── AppHeader (with Settings button)
│       ├── TabNavigation (Dashboard, Weight, Oura, Food)
│       └── Routes
│           ├── DashboardPage
│           │   ├── MetricCard (x4: Weight, Body Fat, Sleep, Readiness)
│           │   ├── QuickStats (x4: Steps, Sleep Duration, HR, Calories)
│           │   ├── WeeklyTrendsChart (Chart.js)
│           │   └── InsightsSection
│           │
│           ├── WeightPage
│           │   ├── StatsGrid (x4: Weight, Body Fat, BMI, Muscle)
│           │   ├── ChartContainer (with toggles + time selector)
│           │   ├── DataTable (with pagination)
│           │   └── MeasurementModal (detail view)
│           │
│           ├── OuraPage
│           │   ├── StatsGrid (x4: Sleep, Readiness, Activity, Duration)
│           │   ├── CollapsibleSection (Advanced Metrics)
│           │   │   └── StatsGrid (x4: Stress, Resilience, VO2, Cardio Age)
│           │   ├── CollapsibleSection (Recovery & Vitals)
│           │   │   └── StatsGrid (x3: SpO2, Bedtime, Workouts)
│           │   ├── ChartContainer (Health Scores)
│           │   ├── ChartContainer (Sleep Duration)
│           │   ├── DataTable (Sleep History)
│           │   ├── SleepModal (detail view)
│           │   ├── ActivityModal (detail view)
│           │   ├── StressModal (detail view with chart)
│           │   └── WorkoutsModal (list view)
│           │
│           └── FoodPage
│               ├── DateSelector
│               ├── StatsGrid (x4: Calories, Protein, Carbs, Fat)
│               ├── MacroChart (Chart.js pie chart)
│               ├── NutrientsGrid (x8+: Fiber, Sugar, Sodium, etc.)
│               ├── DataTable (Food Log)
│               ├── FoodDetailModal (nutrition breakdown)
│               └── SettingsModal (nutritional targets)
```

---

## 14. UI Design Parity Checklist

### 14.1 Visual Elements

- [ ] **CSS Custom Properties**: All design tokens replicated in CSS
- [ ] **Gradient Background**: Body gradient `linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)`
- [ ] **Glass Morphism**: Card backgrounds with `rgba(255, 255, 255, 0.05)`
- [ ] **Font Family**: System font stack matches exactly
- [ ] **Spacing System**: All spacing variables (xs to 2xl) implemented
- [ ] **Border Radius**: All radius values (sm to xl) implemented
- [ ] **Transition Timing**: Fast (0.2s) and normal (0.3s) transitions
- [ ] **Shadows**: Card and hover shadows match

### 14.2 Color System

- [ ] **Primary Colors**: Cyan (#00d4ff) and purple (#7c3aed)
- [ ] **Oura Theme**: Teal (#00a99d) gradient
- [ ] **Food Theme**: Orange (#f97316) gradient
- [ ] **Status Colors**: Success, warning, error match
- [ ] **Chart Colors**: All 6+ chart colors defined
- [ ] **Text Colors**: Primary, secondary, muted text

### 14.3 Typography & Text Effects

- [ ] **Gradient Text**: `-webkit-background-clip: text` working
- [ ] **Metric Values**: 3rem size with gradient
- [ ] **Header Text**: 2.5rem with gradient
- [ ] **Uppercase Labels**: 0.8-0.9rem with letter-spacing
- [ ] **Unit Text**: Smaller, secondary color

### 14.4 Component Parity

#### Tab Navigation
- [ ] Glass morphism background
- [ ] Gradient active state (different per tab)
- [ ] Icon + text layout
- [ ] Hover state transitions
- [ ] Mobile responsive (hide text on small screens)

#### Metric Cards (Dashboard)
- [ ] 4px colored top border (variant-specific)
- [ ] Glass morphism background
- [ ] Hover animation (`translateY(-2px)`)
- [ ] Gradient metric values (variant-specific)
- [ ] Progress grid (3 columns)
- [ ] Positive/negative/neutral change colors

#### Modal Overlays
- [ ] Dark overlay (`rgba(0, 0, 0, 0.8)`)
- [ ] Gradient header (variant-specific)
- [ ] Gradient title text
- [ ] Close button hover animation (rotate 90deg)
- [ ] Max width 800px (or component-specific)
- [ ] Scroll behavior

#### Charts
- [ ] Chart.js with date-fns adapter
- [ ] Dark theme colors
- [ ] Grid lines (`rgba(255, 255, 255, 0.05)`)
- [ ] Tooltip styling (cyan title, dark background)
- [ ] Legend at bottom
- [ ] Toggle controls for series
- [ ] Time range selector buttons

#### Tables
- [ ] Dark table header background
- [ ] Row hover effect
- [ ] Clickable rows (where applicable)
- [ ] Pagination controls
- [ ] Empty state styling

#### Status Badges
- [ ] Color-coded backgrounds (success/warning/error)
- [ ] Rounded corners
- [ ] Proper padding

#### Progress Bars
- [ ] Gradient fills (nutrient-specific)
- [ ] Smooth width transitions
- [ ] 8px height
- [ ] Rounded corners

#### Collapsible Sections
- [ ] Arrow rotation animation
- [ ] Max-height transition
- [ ] Opacity fade
- [ ] Header hover state

### 14.5 Specific Page Checks

#### Dashboard Page
- [ ] 4 metric cards with correct gradients
- [ ] Quick stats row (4 items)
- [ ] Weekly trends chart
- [ ] Insights section

#### Weight/Picooc Page
- [ ] 4 stat cards
- [ ] Chart with 4 toggles
- [ ] Time range selector
- [ ] Data table with pagination
- [ ] Clickable rows opening modal

#### Oura Page
- [ ] Primary scores (4 cards)
- [ ] Collapsible "Advanced Health Metrics" section
- [ ] Collapsible "Recovery & Vitals" section
- [ ] Clickable stress card (opens modal)
- [ ] Clickable workout card (opens modal)
- [ ] Health scores chart
- [ ] Sleep duration chart
- [ ] Sleep history table

#### Food Page
- [ ] Date selector
- [ ] 4 macro cards (calories, protein, carbs, fat)
- [ ] Macro pie chart
- [ ] Nutrients grid (4 columns)
- [ ] Progress bars with exceeded indicators
- [ ] Food log table
- [ ] Clickable rows opening food detail modal

#### Settings Modal
- [ ] Preset buttons
- [ ] Form inputs with focus states
- [ ] Save/cancel buttons

### 14.6 Animations & Interactions

- [ ] Tab switching (instant)
- [ ] Card hover lift effect
- [ ] Button hover effects
- [ ] Modal open/close transitions
- [ ] Collapsible section expand/collapse
- [ ] Progress bar width animations
- [ ] Close button rotation on hover
- [ ] Settings button rotation on hover

### 14.7 Responsive Behavior

- [ ] Tab text hidden on mobile (<768px)
- [ ] Metric cards stack properly
- [ ] Charts responsive height
- [ ] Tables horizontal scroll
- [ ] Modal padding adjusts
- [ ] Grid columns adjust on small screens

## 15. Implementation Checklist

### 15.1 Project Setup

- [ ] Initialize Vite project with React TypeScript template
- [ ] Configure TypeScript with strict mode
- [ ] Install Fluent UI React v9 dependencies
- [ ] Install Chart.js and react-chartjs-2
- [ ] Install date-fns and chartjs-adapter-date-fns
- [ ] Set up CSS Modules configuration
- [ ] Create design-tokens.css with all variables

### 15.2 Core Architecture

- [ ] Set up TanStack Query for server state management
- [ ] Configure Axios API client with interceptors
- [ ] Define TypeScript types for all API responses
- [ ] Create feature-based folder structure
- [ ] Configure React Router with route constants
- [ ] Add error boundary for global error handling
- [ ] Implement loading states with custom spinner

### 15.3 Styling Infrastructure

- [ ] Create custom Fluent UI theme (healthDarkTheme)
- [ ] Set up global CSS with design tokens
- [ ] Create utility CSS classes (gradient-text, glass-card)
- [ ] Set up CSS Modules for components
- [ ] Configure Chart.js default theme

### 15.4 Component Development

- [ ] Build TabNavigation component
- [ ] Build MetricCard component (all variants)
- [ ] Build Modal component (all variants)
- [ ] Build ChartContainer component
- [ ] Build DataTable component
- [ ] Build StatusBadge component
- [ ] Build ProgressBar component
- [ ] Build CollapsibleSection component
- [ ] Build TimeRangeSelector component

### 15.5 Page Implementation

- [ ] Implement AppLayout with header
- [ ] Build Dashboard page with metric cards
- [ ] Build Weight/Picooc page
- [ ] Build Oura page with collapsible sections
- [ ] Build Food page with nutrition tracking
- [ ] Build Settings modal

### 15.6 Quality & Testing

- [ ] Set up Vitest for unit testing
- [ ] Write unit tests for components and hooks
- [ ] Set up Playwright for E2E testing
- [ ] Write E2E tests for critical user flows
- [ ] Perform visual regression testing (compare to current dashboard)
- [ ] Test all animations and transitions
- [ ] Test responsive behavior

### 15.7 Optimization & Deployment

- [ ] Optimize bundle size with code splitting
- [ ] Lazy load routes
- [ ] Configure Azure Static Web Apps
- [ ] Set up CI/CD pipeline for deployment
- [ ] Add Application Insights for telemetry
- [ ] Performance audit (Lighthouse)

---

## 16. Gray Areas / Questions

### 16.1 Charting Library

**Decision Made**: Use **Chart.js with react-chartjs-2** (SAME as current implementation) to ensure visual parity. This is the ONLY acceptable option for maintaining pixel-perfect design.

**Rationale:**
- Current dashboard uses Chart.js with specific theming
- Recharts would require extensive customization and may not match
- D3.js is overkill and would require complete rewrite of chart logic

### 16.2 Dark Mode Toggle

**Question:** Should the app support light mode, or remain dark-only like the current dashboard?

**Options:**
- **Option 1:** Dark mode only (matches current design)
- **Option 2:** Add light mode toggle (requires redesigning all gradients and colors)

**Recommendation:** Option 1 (dark-only) to maintain design consistency and reduce scope.

### 16.3 Offline Support

**Question:** Should the SPA work offline with cached data?

**Options:**
- **Option 1:** No offline support (same as current)
- **Option 2:** Service Worker with cache-first strategy for API responses
- **Option 3:** Full PWA with IndexedDB

**Recommendation:** Option 2 for basic offline viewing of cached data (future enhancement).

### 16.4 Real-Time Updates

**Question:** Should metrics update in real-time?

**Options:**
- **Option 1:** Manual refresh only (same as current)
- **Option 2:** Polling with TanStack Query refetch (every 5 minutes)
- **Option 3:** SignalR for real-time push notifications

**Recommendation:** Option 2 (polling every 5 minutes) for automatic updates without complexity.

---

## 17. Critical Success Factors

### 17.1 Non-Negotiable Requirements

1. **Visual Parity**: The React app MUST look identical to the current dashboard
   - All colors, gradients, animations must match exactly
   - Use Chart.js (NOT Recharts or D3.js)
   - Replicate all CSS custom properties

2. **Component Approach**: Fluent UI v9 + Custom CSS Modules
   - Fluent UI for base components (Dialog, Card, DataGrid)
   - Custom CSS Modules for ALL visual styling
   - Fluent UI theme heavily customized to match current design

3. **No Feature Changes**: This is a framework migration ONLY
   - All current features must be preserved
   - All current interactions must work the same way
   - No new features, no removed features

4. **Testing Strategy**: Visual regression testing is CRITICAL
   - Compare screenshots of React app vs. current dashboard
   - Test all animations and transitions
   - Verify responsive behavior matches

### 17.2 Developer Guidelines

**DO:**
- ✅ Copy CSS from current dashboard and adapt to CSS Modules
- ✅ Use exact same color values (#00d4ff, #7c3aed, etc.)
- ✅ Test components side-by-side with current dashboard
- ✅ Use Chart.js with same configuration as current implementation
- ✅ Preserve all current icons (emojis)
- ✅ Match font sizes, spacing, and border radius exactly

**DON'T:**
- ❌ Use default Fluent UI styling without customization
- ❌ Change any colors or gradients
- ❌ Use a different charting library
- ❌ Add new features or change existing ones
- ❌ Skip visual comparison testing
- ❌ Remove any animations or hover effects

### 17.3 Validation Process

Before considering the migration complete, validate ALL of the following:

1. **Visual Inspection**: Open React app and current dashboard side-by-side
   - All colors match (use color picker tool)
   - All gradients are identical
   - All spacing and sizing matches
   - All animations work the same

2. **Functional Testing**: Every feature works identically
   - Tab switching
   - Chart interactions (toggles, time range selectors)
   - Modal opening/closing
   - Table pagination
   - Collapsible sections

3. **Performance**: React app should be FASTER than current
   - Bundle size < 500KB gzipped
   - Time to Interactive < 2 seconds
   - Lighthouse score > 90

4. **Accessibility**: Maintain or improve accessibility
   - All ARIA labels preserved
   - Keyboard navigation works
   - Screen reader compatible

---

## 18. References

### Microsoft Documentation

- [React TypeScript Best Practices](https://react.dev/learn/typescript)
- [Fluent UI React v9](https://react.fluentui.dev/)
- [Azure Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/)
- [Vite Build Tool](https://vitejs.dev/guide/)
- [TanStack Query](https://tanstack.com/query/latest/docs/framework/react/overview)

### Charting & Styling

- [Chart.js Documentation](https://www.chartjs.org/docs/latest/)
- [react-chartjs-2](https://react-chartjs-2.js.org/)
- [CSS Modules](https://github.com/css-modules/css-modules)
- [CSS Custom Properties (Variables)](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties)

### Best Practices

- Use TypeScript strict mode for type safety
- Implement feature-based folder structure for scalability
- Use path-based imports for tree-shaking
- Lazy load routes with React.lazy and Suspense
- Use TanStack Query for server state management
- Implement error boundaries for graceful error handling
- Write E2E tests with Playwright for critical flows
- Optimize bundle size with code splitting and lazy loading
- Use React.memo for expensive component re-renders
- Follow WCAG 2.1 accessibility guidelines
- **ALWAYS validate visual parity against current dashboard**
