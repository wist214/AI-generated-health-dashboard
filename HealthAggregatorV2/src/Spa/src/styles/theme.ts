import {
  createDarkTheme,
  BrandVariants,
  Theme,
} from '@fluentui/react-components';

/**
 * Health Aggregator Brand Colors
 * Custom brand variants matching the current dark theme design
 */
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

/**
 * Health Aggregator Dark Theme
 * Customized Fluent UI theme matching the V1 dashboard design
 */
export const healthDarkTheme: Theme = {
  ...createDarkTheme(healthBrand),
  // Primary brand colors
  colorBrandForeground1: '#00d4ff',              // Primary cyan
  colorBrandForeground2: '#7c3aed',              // Secondary purple
  colorBrandBackground: '#00d4ff',
  colorBrandBackgroundHover: '#00a9cc',
  colorBrandBackgroundPressed: '#0091b3',

  // Neutral backgrounds matching design tokens
  colorNeutralBackground1: '#1a1a2e',            // Main background
  colorNeutralBackground2: 'rgba(255,255,255,0.05)', // Card background
  colorNeutralBackground3: 'rgba(255,255,255,0.08)', // Card hover
  colorNeutralBackground4: '#16213e',            // Darker background
  colorNeutralBackgroundInverted: '#e0e0e0',

  // Text colors
  colorNeutralForeground1: '#e0e0e0',            // Primary text
  colorNeutralForeground2: '#888888',            // Secondary text
  colorNeutralForeground3: '#666666',            // Muted text
  colorNeutralForeground4: '#555555',

  // Status colors
  colorPaletteGreenForeground1: '#22c55e',       // Success
  colorPaletteYellowForeground1: '#f59e0b',      // Warning
  colorPaletteRedForeground1: '#ef4444',         // Error
  colorPaletteBlueForeground2: '#3b82f6',        // Info

  // Stroke/border colors
  colorNeutralStroke1: 'rgba(255, 255, 255, 0.1)',
  colorNeutralStroke2: 'rgba(255, 255, 255, 0.05)',
  colorNeutralStrokeAccessible: '#888888',

  // Shadow tokens
  shadow4: '0 4px 20px rgba(0, 0, 0, 0.3)',
  shadow8: '0 8px 30px rgba(0, 0, 0, 0.4)',
  shadow16: '0 16px 50px rgba(0, 0, 0, 0.5)',

  // Border radius tokens
  borderRadiusMedium: '12px',
  borderRadiusLarge: '15px',
  borderRadiusXLarge: '20px',

  // Font family
  fontFamilyBase: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen, Ubuntu, sans-serif',
};

/**
 * Chart color constants matching V1 dashboard
 */
export const CHART_COLORS = {
  sleep: '#3b82f6',
  readiness: '#22c55e',
  activity: '#f59e0b',
  weight: '#00d4ff',
  bodyFat: '#f472b6',
  muscle: '#34d399',
  bmi: '#a855f7',
  calories: '#ef4444',
  protein: '#3b82f6',
  carbs: '#f59e0b',
  fat: '#ef4444',
  fiber: '#22c55e',
  sugar: '#f472b6',
  sodium: '#3b82f6',
  cholesterol: '#f59e0b',
} as const;

/**
 * Gradient definitions for text and backgrounds
 */
export const GRADIENTS = {
  primary: 'linear-gradient(90deg, #00d4ff, #7c3aed)',
  oura: 'linear-gradient(90deg, #00a99d, #00d4aa)',
  food: 'linear-gradient(90deg, #f97316, #fb923c)',
  sleep: 'linear-gradient(90deg, #3b82f6, #1d4ed8)',
  readiness: 'linear-gradient(90deg, #22c55e, #16a34a)',
  weight: 'linear-gradient(90deg, #00d4ff, #7c3aed)',
  dashboard: 'linear-gradient(135deg, #f59e0b, #ef4444)',
  background: 'linear-gradient(135deg, #1a1a2e 0%, #16213e 100%)',
} as const;

/**
 * Metric card border colors by variant
 */
export const CARD_BORDER_COLORS = {
  weight: GRADIENTS.primary,
  sleep: GRADIENTS.sleep,
  readiness: GRADIENTS.readiness,
  bodyScore: GRADIENTS.primary,
  calories: 'linear-gradient(90deg, #ef4444, #f97316)',
  protein: 'linear-gradient(90deg, #3b82f6, #0ea5e9)',
  carbs: 'linear-gradient(90deg, #f59e0b, #fbbf24)',
  fat: 'linear-gradient(90deg, #ef4444, #f472b6)',
} as const;
