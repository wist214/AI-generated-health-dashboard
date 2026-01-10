import { get } from '@shared/api/apiClient';
import { API_ENDPOINTS } from '@shared/api/endpoints';
import type { DashboardSummaryResponse, DailySummaryResponse } from '@shared/api/types';

/**
 * Dashboard API service
 */
export const dashboardService = {
  /**
   * Get the dashboard summary for today
   */
  async getSummary(): Promise<DashboardSummaryResponse> {
    return get<DashboardSummaryResponse>(API_ENDPOINTS.DASHBOARD_SUMMARY);
  },

  /**
   * Get daily history for charts
   * @param days Number of days to fetch (default 30)
   */
  async getHistory(days = 30): Promise<DailySummaryResponse[]> {
    return get<DailySummaryResponse[]>(API_ENDPOINTS.DASHBOARD_HISTORY, { days });
  },
};
