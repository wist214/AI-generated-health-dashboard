/**
 * API endpoint constants
 * Centralized API routes for the Health Aggregator V2 API
 */
export const API_ENDPOINTS = {
  // Metrics
  METRICS_LATEST: '/api/metrics/latest',
  METRICS_RANGE: '/api/metrics/range',
  METRIC_BY_TYPE: (type: string) => `/api/metrics/latest/${type}`,
  METRICS_BY_CATEGORY: (category: string) => `/api/metrics/category/${category}`,

  // Dashboard
  DASHBOARD_SUMMARY: '/api/dashboard/summary',
  DASHBOARD_HISTORY: '/api/dashboard/history',

  // Sources
  SOURCES: '/api/sources',
  SOURCE_BY_NAME: (name: string) => `/api/sources/${name}`,

  // Health
  HEALTH: '/health',
} as const;
