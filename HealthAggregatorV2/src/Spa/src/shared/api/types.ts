/**
 * Shared API types
 * Common types used across API responses
 */

/**
 * Generic API response wrapper
 */
export interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
}

/**
 * Paginated response for list endpoints
 */
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Standard API error response
 */
export interface ApiError {
  message: string;
  code?: string;
  details?: Record<string, string[]>;
  statusCode?: number;
}

/**
 * Metric categories enum (matches backend)
 */
export type MetricCategory = 
  | 'Sleep'
  | 'Activity'
  | 'Body'
  | 'Heart'
  | 'Nutrition';

/**
 * Source names enum
 */
export type SourceName = 
  | 'Oura'
  | 'Picooc'
  | 'Cronometer'
  | 'Manual';

/**
 * Latest metric response from API
 */
export interface LatestMetricResponse {
  metricType: string;
  value: number | null;
  unit: string;
  category: MetricCategory;
  sourceName: SourceName;
  recordedAt: string; // ISO date string
  daysAgo: number;
}

/**
 * Dashboard summary response - matches actual API response
 */
export interface DashboardSummaryResponse {
  oura: {
    dailySleep: Array<{
      id: string;
      day: string;
      score: number | null;
      totalSleepDuration: number | null;
      deepSleepDuration: number | null;
      remSleepDuration: number | null;
      lightSleepDuration: number | null;
      awakeTime: number | null;
      efficiency: number | null;
      averageHeartRate: number | null;
      lowestHeartRate: number | null;
      averageHrv: number | null;
      bedtimeStart: string | null;
      bedtimeEnd: string | null;
      latencyDuration: number | null;
      restfulness: string | null;
    }>;
    readiness: Array<{
      id: string;
      day: string;
      score: number | null;
      temperatureDeviation: number | null;
      activityBalance: number | null;
      bodyTemperature: string | null;
      hrvBalance: number | null;
      previousDayActivity: number | null;
      previousNight: number | null;
      recoveryIndex: number | null;
      restingHeartRate: number | null;
      sleepBalance: number | null;
    }>;
    activity: Array<{
      id: string;
      day: string;
      score: number | null;
      activeCalories: number | null;
      totalCalories: number | null;
      steps: number | null;
      targetCalories: number | null;
      averageMetMinutes: number | null;
      highActivityMetMinutes: number | null;
      lowActivityMetMinutes: number | null;
      mediumActivityMetMinutes: number | null;
      nonWearMinutes: number | null;
      restingMetMinutes: number | null;
      sedentaryMetMinutes: number | null;
      totalSteps: number | null;
      inactivityAlerts: number | null;
    }>;
    lastSync: string | null;
  };
  picooc: {
    measurements: Array<{
      date: string;
      weight: number | null;
      bmi: number | null;
      bodyFat: number | null;
      bodyWater: number | null;
      boneMass: number | null;
      metabolicAge: number | null;
      visceralFat: number | null;
      basalMetabolism: number | null;
      skeletalMuscleMass: number | null;
      source: string | null;
      hasValidWeight: boolean;
      hasBodyComposition: boolean;
    }>;
    lastSync: string | null;
  };
  latest: {
    weight: number | null;
    bodyFat: number | null;
    weightDate: string | null;
    sleepScore: number | null;
    sleepDate: string | null;
    readinessScore: number | null;
    readinessDate: string | null;
    activityScore: number | null;
    steps: number | null;
    activityDate: string | null;
  };
}

/**
 * Daily summary for history endpoints
 */
export interface DailySummaryResponse {
  date: string;
  sleepScore: number | null;
  readinessScore: number | null;
  activityScore: number | null;
  steps: number | null;
  caloriesBurned: number | null;
  weight: number | null;
  bodyFatPercentage: number | null;
  proteinGrams: number | null;
  carbsGrams: number | null;
  fatGrams: number | null;
}

/**
 * Source status response
 */
export interface SourceStatusResponse {
  id: number;
  name: string;
  displayName: string;
  isActive: boolean;
  lastSyncAt: string | null;
  metricCount: number;
}

/**
 * Metric range query parameters
 */
export interface MetricRangeParams {
  startDate: string;
  endDate: string;
  metricTypes?: string[];
}

/**
 * Dashboard history query parameters
 */
export interface DashboardHistoryParams {
  startDate?: string;
  endDate?: string;
  days?: number;
}
