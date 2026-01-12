import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { get, API_ENDPOINTS } from '@shared/api';
import { syncPicooc } from '@shared/api/syncClient';
import type { WeightMetric } from '../types';
import type { DashboardSummaryResponse } from '@shared/api/types';

/**
 * Query key factory for weight data
 */
export const weightKeys = {
  all: ['weight'] as const,
  history: (range: string) => [...weightKeys.all, 'history', range] as const,
  latest: () => [...weightKeys.all, 'latest'] as const,
  dashboard: () => [...weightKeys.all, 'dashboard'] as const,
};

/**
 * Transform Picooc measurements to WeightMetric format
 */
const transformToWeightMetrics = (measurements: DashboardSummaryResponse['picooc']['measurements']): WeightMetric[] => {
  return measurements
    .filter(m => m.weight !== null)
    .map(m => ({
      date: m.date,
      weight: m.weight,
      bodyFat: m.bodyFat,
      bmi: m.bmi,
      muscleMass: m.skeletalMuscleMass,
      bodyWater: m.bodyWater,
      metabolicAge: m.metabolicAge,
      visceralFat: m.visceralFat,
      boneMass: m.boneMass,
      bmr: m.basalMetabolism,
    }));
};

/**
 * Fetch dashboard data which contains both Picooc and Oura data
 */
const fetchDashboardData = async (): Promise<DashboardSummaryResponse> => {
  return get<DashboardSummaryResponse>(API_ENDPOINTS.DASHBOARD);
};

/**
 * Trigger Picooc sync via Azure Functions
 */
const syncPicoocData = async () => {
  return syncPicooc();
};

/**
 * Hook for fetching weight history (uses Picooc measurements from dashboard)
 */
export const useWeightHistory = (_startDate: string, _endDate: string) => {
  return useQuery({
    queryKey: weightKeys.history(`all`),
    queryFn: fetchDashboardData,
    staleTime: 5 * 60 * 1000, // 5 minutes
    select: (data) => transformToWeightMetrics(data.picooc?.measurements ?? []),
  });
};

/**
 * Hook for fetching latest weight metrics (uses dashboard summary)
 */
export const useLatestWeight = () => {
  return useQuery({
    queryKey: weightKeys.dashboard(),
    queryFn: fetchDashboardData,
    staleTime: 5 * 60 * 1000, // 5 minutes
    select: (data) => {
      const latestMeasurement = data.picooc?.measurements?.[0];
      return {
        weight: data.latest?.weight ?? null,
        bodyFat: data.latest?.bodyFat ?? null,
        bmi: latestMeasurement?.bmi ?? null,
        // Show sync time to indicate when data was last fetched from Picooc
        lastUpdated: data.picooc?.lastSync ?? data.latest?.weightDate ?? null,
      };
    },
  });
};

/**
 * Hook for Picooc data sync
 */
export const usePicoocSync = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: syncPicoocData,
    onSuccess: async () => {
      // Invalidate all weight-related queries
      await queryClient.invalidateQueries({ queryKey: weightKeys.all });
      // Invalidate dashboard to update Insights section
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      // Force refetch to ensure UI updates with fresh data
      await queryClient.refetchQueries({ queryKey: weightKeys.all, type: 'active' });
      await queryClient.refetchQueries({ queryKey: ['dashboard'], type: 'active' });
    },
  });
};
