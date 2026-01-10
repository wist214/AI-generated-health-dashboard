import { useQuery } from '@tanstack/react-query';
import { dashboardService } from '../services/dashboardService';
import type { DashboardSummaryResponse, DailySummaryResponse } from '@shared/api/types';

/**
 * Hook to fetch dashboard summary data
 */
export function useDashboardSummary() {
  return useQuery<DashboardSummaryResponse, Error>({
    queryKey: ['dashboard', 'summary'],
    queryFn: dashboardService.getSummary,
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    refetchOnWindowFocus: true,
    retry: 2,
  });
}

/**
 * Hook to fetch dashboard history for charts
 * @param days Number of days to fetch
 */
export function useDashboardHistory(days = 30) {
  return useQuery<DailySummaryResponse[], Error>({
    queryKey: ['dashboard', 'history', days],
    queryFn: () => dashboardService.getHistory(days),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    retry: 2,
  });
}
