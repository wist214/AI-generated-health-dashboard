/**
 * Weight feature types
 * Types for Picooc weight tracking
 */

/**
 * Weight metric data
 */
export interface WeightMetric {
  date: string;
  weight: number | null;
  bodyFat: number | null;
  bmi: number | null;
  muscleMass: number | null;
  bodyWater: number | null;
  metabolicAge: number | null;
  visceralFat: number | null;
  boneMass: number | null;
  bmr: number | null;
}

/**
 * Weight summary for stats cards
 */
export interface WeightSummary {
  currentWeight: number | null;
  currentBodyFat: number | null;
  currentBMI: number | null;
  currentMuscle: number | null;
  lastUpdated: string | null;
  weightChange7d: number | null;
  weightChange30d: number | null;
  bodyFatChange7d: number | null;
  bodyFatChange30d: number | null;
}

/**
 * Time range options for charts
 */
export type TimeRange = '7d' | '30d' | '90d' | '6m' | '1y' | 'all';

/**
 * Chart series configuration
 */
export interface ChartSeries {
  id: string;
  label: string;
  color: string;
  enabled: boolean;
}
