/**
 * Food feature types
 * Types for Cronometer nutrition tracking
 */

/**
 * Daily nutrition summary
 */
export interface DailyNutrition {
  date: string;
  calories: number | null;
  protein: number | null;
  carbs: number | null;
  fat: number | null;
  fiber: number | null;
  sugar: number | null;
  netCarbs: number | null;
  sodium: number | null;
  cholesterol: number | null;
}

/**
 * Macro breakdown
 */
export interface MacroBreakdown {
  protein: number;
  carbs: number;
  fat: number;
}

/**
 * Nutrient with goal
 */
export interface NutrientProgress {
  name: string;
  value: number | null;
  goal: number;
  unit: string;
  variant: 'fiber' | 'sugar' | 'sodium' | 'cholesterol' | 'vitamin' | 'mineral';
}

/**
 * Food log entry
 */
export interface FoodLogEntry {
  id: string;
  name: string;
  mealType: 'breakfast' | 'lunch' | 'dinner' | 'snack';
  calories: number;
  protein: number;
  carbs: number;
  fat: number;
  time: string;
}

/**
 * Time range for charts
 */
export type TimeRange = '7d' | '30d' | '90d' | '6m' | '1y' | 'all';
