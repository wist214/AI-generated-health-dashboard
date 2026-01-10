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
 * Dashboard summary response
 */
export interface DashboardSummaryResponse {
  date: string;
  sleepScore: number | null;
  readinessScore: number | null;
  activityScore: number | null;
  steps: number | null;
  caloriesBurned: number | null;
  weight: number | null;
  bodyFatPercentage: number | null;
  lastUpdated: string;
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
