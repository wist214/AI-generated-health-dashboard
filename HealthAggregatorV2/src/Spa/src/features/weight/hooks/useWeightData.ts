import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { get, API_ENDPOINTS } from '@shared/api';
import { syncPicooc } from '@shared/api/syncClient';
import type { WeightMetric } from '../types';

/**
 * API response types matching backend DTOs
 */
interface DashboardSummaryDto {
  sleepScore: number | null;
  readinessScore: number | null;
  totalSleepHours: number | null;
  deepSleepHours: number | null;
  remSleepHours: number | null;
  sleepEfficiency: number | null;
  activityScore: number | null;
  steps: number | null;
  activeCalories: number | null;
  weight: number | null;
  bodyFat: number | null;
  bmi: number | null;
  heartRateAvg: number | null;
  heartRateMin: number | null;
  hrvAverage: number | null;
  caloriesConsumed: number | null;
  protein: number | null;
  carbs: number | null;
  fat: number | null;
  lastUpdated: string;
  sourceSyncInfo: Array<{ sourceName: string; lastSyncedAt: string | null }>;
}

interface DailySummaryDto {
  date: string;
  sleepScore: number | null;
  readinessScore: number | null;
  totalSleepHours: number | null;
  deepSleepHours: number | null;
  remSleepHours: number | null;
  sleepEfficiency: number | null;
  hrvAverage: number | null;
  restingHeartRate: number | null;
  activityScore: number | null;
  steps: number | null;
  activeCalories: number | null;
  weight: number | null;
  bodyFat: number | null;
  caloriesConsumed: number | null;
  protein: number | null;
  carbs: number | null;
  fat: number | null;
}

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
 * Transform daily summaries to WeightMetric format
 */
const transformToWeightMetrics = (summaries: DailySummaryDto[]): WeightMetric[] => {
  return summaries
    .filter(s => s.weight !== null || s.bodyFat !== null)
    .map(s => ({
      date: s.date,
      weight: s.weight,
      bodyFat: s.bodyFat,
      bmi: null, // Not in daily summaries
      muscleMass: null,
      bodyWater: null,
      metabolicAge: null,
      visceralFat: null,
      boneMass: null,
    }));
};

/**
 * Fetch weight metrics for a date range using dashboard history endpoint
 */
const fetchWeightHistory = async (startDate: string, endDate: string): Promise<WeightMetric[]> => {
  const params = new URLSearchParams({
    from: startDate,
    to: endDate,
  });
  
  const response = await get<DailySummaryDto[]>(`${API_ENDPOINTS.DASHBOARD_HISTORY}?${params}`);
  return transformToWeightMetrics(response);
};

/**
 * Fetch dashboard summary for latest weight data
 */
const fetchDashboardSummary = async (): Promise<DashboardSummaryDto> => {
  return get<DashboardSummaryDto>(API_ENDPOINTS.DASHBOARD_SUMMARY);
};

/**
 * Trigger Picooc sync via Azure Functions
 */
const syncPicoocData = async () => {
  return syncPicooc();
};

/**
 * Hook for fetching weight history
 */
export const useWeightHistory = (startDate: string, endDate: string) => {
  return useQuery({
    queryKey: weightKeys.history(`${startDate}-${endDate}`),
    queryFn: () => fetchWeightHistory(startDate, endDate),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook for fetching latest weight metrics (uses dashboard summary)
 */
export const useLatestWeight = () => {
  return useQuery({
    queryKey: weightKeys.dashboard(),
    queryFn: fetchDashboardSummary,
    staleTime: 5 * 60 * 1000, // 5 minutes
    select: (data) => ({
      weight: data.weight,
      bodyFat: data.bodyFat,
      bmi: data.bmi,
      lastUpdated: data.lastUpdated,
    }),
  });
};

/**
 * Hook for Picooc data sync
 */
export const usePicoocSync = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: syncPicoocData,
    onSuccess: () => {
      // Invalidate all weight queries to refetch
      queryClient.invalidateQueries({ queryKey: weightKeys.all });
    },
  });
};
