/**
 * Oura feature types
 * Types for Oura Ring data tracking
 */

/**
 * Oura daily scores
 */
export interface OuraScores {
  date: string;
  sleepScore: number | null;
  readinessScore: number | null;
  activityScore: number | null;
}

/**
 * Sleep data from Oura
 */
export interface OuraSleepData {
  date: string;
  score: number | null;
  totalSleep: number | null; // seconds
  deepSleep: number | null; // seconds
  remSleep: number | null; // seconds
  lightSleep: number | null; // seconds
  awakeTime: number | null; // seconds
  sleepEfficiency: number | null; // percentage
  sleepLatency: number | null; // seconds
  avgHeartRate: number | null;
  avgHrv: number | null;
  avgBreath: number | null;
}

/**
 * Activity data from Oura
 */
export interface OuraActivityData {
  date: string;
  score: number | null;
  steps: number | null;
  activeCalories: number | null;
  totalCalories: number | null;
  distance: number | null; // meters
  highActivity: number | null; // seconds
  mediumActivity: number | null; // seconds
  lowActivity: number | null; // seconds
  sedentaryTime: number | null; // seconds
}

/**
 * Readiness data from Oura
 */
export interface OuraReadinessData {
  date: string;
  score: number | null;
  temperatureDeviation: number | null;
  hrvBalance: number | null;
  restingHeartRate: number | null;
  recoveryIndex: number | null;
  previousNightScore: number | null;
  sleepBalance: number | null;
  activityBalance: number | null;
}

/**
 * Advanced health metrics from Oura
 */
export interface OuraAdvancedMetrics {
  stressStatus: string | null;
  stressHigh: number | null;
  stressMedium: number | null;
  stressLow: number | null;
  resilienceLevel: string | null;
  vo2Max: number | null;
  cardioAge: number | null;
  spo2: number | null;
  optimalBedtimeStart: string | null;
  optimalBedtimeEnd: string | null;
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

/**
 * Collapsible section state
 */
export interface SectionState {
  advancedMetrics: boolean;
  recoveryVitals: boolean;
}
