/**
 * API endpoint constants
 * Centralized API routes for the Health Aggregator Azure Functions API
 */
export const API_ENDPOINTS = {
  // Dashboard
  DASHBOARD: '/api/dashboard',
  DASHBOARD_METRICS: '/api/dashboard/metrics',
  DASHBOARD_SUMMARY: '/api/dashboard',        // Alias for compatibility
  DASHBOARD_HISTORY: '/api/dashboard/metrics', // Alias for compatibility

  // Oura Ring
  OURA: {
    DATA: '/api/oura/data',
    SYNC: '/api/oura/sync',
    STATUS: '/api/oura/status',
    STATS: '/api/oura/stats',
    SLEEP: '/api/oura/sleep',
    SLEEP_DETAIL: (id: string) => `/api/oura/sleep/${id}`,
    READINESS: '/api/oura/readiness',
    ACTIVITY: '/api/oura/activity',
    STRESS: '/api/oura/stress',
    WORKOUTS: '/api/oura/workouts',
    RESILIENCE: '/api/oura/resilience',
    LATEST: '/api/oura/latest',
  },

  // Picooc Scale
  PICOOC: {
    DATA: '/api/picooc/data',
    SYNC: '/api/picooc/sync',
    STATUS: '/api/picooc/status',
    STATS: '/api/picooc/stats',
    LATEST: '/api/picooc/latest',
  },

  // Cronometer Food
  CRONOMETER: {
    DATA: '/api/cronometer/data',
    SYNC: '/api/cronometer/sync',
    STATUS: '/api/cronometer/status',
    STATS: '/api/cronometer/stats',
    LATEST: '/api/cronometer/latest',
    DAILY: (date: string) => `/api/cronometer/daily/${date}`,
  },

  // Settings
  SETTINGS: '/api/settings',
} as const;
