import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { get, API_ENDPOINTS } from '@shared/api';
import { syncCronometer } from '@shared/api/syncClient';
import type { MacroBreakdown } from '../types';

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
 * Query key factory for food data
 */
export const foodKeys = {
  all: ['food'] as const,
  daily: (date: string) => [...foodKeys.all, 'daily', date] as const,
  history: (range: string) => [...foodKeys.all, 'history', range] as const,
  latest: () => [...foodKeys.all, 'latest'] as const,
  dashboard: () => [...foodKeys.all, 'dashboard'] as const,
};

/**
 * Fetch dashboard history for nutrition data
 */
const fetchNutritionHistory = async (startDate: string, endDate: string): Promise<DailySummaryDto[]> => {
  const params = new URLSearchParams({
    from: startDate,
    to: endDate,
  });
  return get<DailySummaryDto[]>(`${API_ENDPOINTS.DASHBOARD_HISTORY}?${params}`);
};

/**
 * Fetch dashboard summary for latest nutrition data
 */
const fetchDashboardSummary = async (): Promise<DashboardSummaryDto> => {
  return get<DashboardSummaryDto>(API_ENDPOINTS.DASHBOARD_SUMMARY);
};

/**
 * Trigger Cronometer sync via Azure Functions
 */
const syncCronometerData = async () => {
  return syncCronometer();
};

/**
 * Hook for fetching daily nutrition
 */
export const useDailyNutrition = (date: string) => {
  return useQuery({
    queryKey: foodKeys.daily(date),
    queryFn: async () => {
      const data = await fetchNutritionHistory(date, date);
      return data[0] ?? null;
    },
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching nutrition history
 */
export const useNutritionHistory = (startDate: string, endDate: string) => {
  return useQuery({
    queryKey: foodKeys.history(`${startDate}-${endDate}`),
    queryFn: () => fetchNutritionHistory(startDate, endDate),
    staleTime: 5 * 60 * 1000,
    select: (data) => data.filter(d => d.caloriesConsumed !== null).map(d => ({
      date: d.date,
      calories: d.caloriesConsumed,
      protein: d.protein,
      carbs: d.carbs,
      fat: d.fat,
    })),
  });
};

/**
 * Hook for fetching latest nutrition (uses dashboard summary)
 */
export const useLatestNutrition = () => {
  return useQuery({
    queryKey: foodKeys.dashboard(),
    queryFn: fetchDashboardSummary,
    staleTime: 5 * 60 * 1000,
    select: (data) => ({
      calories: data.caloriesConsumed,
      protein: data.protein,
      carbs: data.carbs,
      fat: data.fat,
      lastUpdated: data.lastUpdated,
      macros: (data.protein !== null && data.carbs !== null && data.fat !== null) 
        ? { protein: data.protein, carbs: data.carbs, fat: data.fat } as MacroBreakdown
        : null,
    }),
  });
};

/**
 * Hook for Cronometer data sync
 */
export const useCronometerSync = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: syncCronometerData,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: foodKeys.all });
    },
  });
};
