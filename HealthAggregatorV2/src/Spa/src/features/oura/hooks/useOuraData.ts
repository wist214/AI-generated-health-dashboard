import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { get, API_ENDPOINTS } from '@shared/api';
import { syncOura } from '@shared/api/syncClient';
import type { OuraSleepData, OuraActivityData } from '../types';

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
  // Advanced Oura metrics
  dailyStress: string | null;
  resilienceLevel: string | null;
  vo2Max: number | null;
  cardiovascularAge: number | null;
  spO2Average: number | null;
  optimalBedtimeStart: number | null;
  optimalBedtimeEnd: number | null;
  workoutCount: number | null;
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
 * Query key factory for Oura data
 */
export const ouraKeys = {
  all: ['oura'] as const,
  sleep: (range: string) => [...ouraKeys.all, 'sleep', range] as const,
  activity: (range: string) => [...ouraKeys.all, 'activity', range] as const,
  readiness: (range: string) => [...ouraKeys.all, 'readiness', range] as const,
  latest: () => [...ouraKeys.all, 'latest'] as const,
  dashboard: () => [...ouraKeys.all, 'dashboard'] as const,
  history: (range: string) => [...ouraKeys.all, 'history', range] as const,
};

/**
 * Convert hours to seconds
 */
const hoursToSeconds = (hours: number | null): number | null => 
  hours !== null ? Math.round(hours * 3600) : null;

/**
 * Transform daily summaries to OuraSleepData format
 */
const transformToSleepData = (summaries: DailySummaryDto[]): OuraSleepData[] => {
  return summaries
    .filter(s => s.sleepScore !== null || s.totalSleepHours !== null)
    .map(s => {
      const totalSleep = hoursToSeconds(s.totalSleepHours);
      const deepSleep = hoursToSeconds(s.deepSleepHours);
      const remSleep = hoursToSeconds(s.remSleepHours);
      const lightSleep = totalSleep !== null && deepSleep !== null && remSleep !== null 
        ? totalSleep - deepSleep - remSleep 
        : null;

      return {
        date: s.date,
        score: s.sleepScore,
        totalSleep,
        deepSleep,
        remSleep,
        lightSleep,
        awakeTime: null,
        sleepEfficiency: s.sleepEfficiency,
        sleepLatency: null,
        avgHeartRate: s.restingHeartRate,
        avgHrv: s.hrvAverage,
        avgBreath: null,
      };
    });
};

/**
 * Transform daily summaries to OuraActivityData format
 */
const transformToActivityData = (summaries: DailySummaryDto[]): OuraActivityData[] => {
  return summaries
    .filter(s => s.activityScore !== null || s.steps !== null)
    .map(s => ({
      date: s.date,
      score: s.activityScore,
      steps: s.steps,
      activeCalories: s.activeCalories,
      totalCalories: null,
      distance: null,
      highActivity: null,
      mediumActivity: null,
      lowActivity: null,
      sedentaryTime: null,
    }));
};

/**
 * Fetch dashboard history for sleep/activity data
 */
const fetchOuraHistory = async (startDate: string, endDate: string): Promise<DailySummaryDto[]> => {
  const params = new URLSearchParams({
    from: startDate,
    to: endDate,
  });
  return get<DailySummaryDto[]>(`${API_ENDPOINTS.DASHBOARD_HISTORY}?${params}`);
};

/**
 * Fetch dashboard summary for latest Oura data
 */
const fetchDashboardSummary = async (): Promise<DashboardSummaryDto> => {
  return get<DashboardSummaryDto>(API_ENDPOINTS.DASHBOARD_SUMMARY);
};

/**
 * Trigger Oura sync via Azure Functions
 */
const syncOuraData = async () => {
  return syncOura();
};

/**
 * Hook for fetching sleep data
 */
export const useSleepData = (startDate: string, endDate: string) => {
  return useQuery({
    queryKey: ouraKeys.sleep(`${startDate}-${endDate}`),
    queryFn: async () => {
      const data = await fetchOuraHistory(startDate, endDate);
      return transformToSleepData(data);
    },
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching activity data
 */
export const useActivityData = (startDate: string, endDate: string) => {
  return useQuery({
    queryKey: ouraKeys.activity(`${startDate}-${endDate}`),
    queryFn: async () => {
      const data = await fetchOuraHistory(startDate, endDate);
      return transformToActivityData(data);
    },
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching Oura history (combined sleep and activity)
 */
export const useOuraHistory = (startDate: string, endDate: string) => {
  return useQuery({
    queryKey: ouraKeys.history(`${startDate}-${endDate}`),
    queryFn: () => fetchOuraHistory(startDate, endDate),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching latest Oura metrics (uses dashboard summary)
 */
export const useLatestOura = () => {
  return useQuery({
    queryKey: ouraKeys.dashboard(),
    queryFn: fetchDashboardSummary,
    staleTime: 5 * 60 * 1000,
    select: (data) => ({
      sleepScore: data.sleepScore,
      readinessScore: data.readinessScore,
      activityScore: data.activityScore,
      totalSleepHours: data.totalSleepHours,
      deepSleepHours: data.deepSleepHours,
      remSleepHours: data.remSleepHours,
      sleepEfficiency: data.sleepEfficiency,
      steps: data.steps,
      activeCalories: data.activeCalories,
      heartRateAvg: data.heartRateAvg,
      hrvAverage: data.hrvAverage,
      // Advanced Oura metrics
      dailyStress: data.dailyStress,
      resilienceLevel: data.resilienceLevel,
      vo2Max: data.vo2Max,
      cardiovascularAge: data.cardiovascularAge,
      spO2Average: data.spO2Average,
      optimalBedtimeStart: data.optimalBedtimeStart,
      optimalBedtimeEnd: data.optimalBedtimeEnd,
      workoutCount: data.workoutCount,
      lastUpdated: data.lastUpdated,
    }),
  });
};

/**
 * Hook for Oura data sync
 */
export const useOuraSync = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: syncOuraData,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ouraKeys.all });
    },
  });
};
