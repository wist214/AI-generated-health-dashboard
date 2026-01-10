import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient, API_ENDPOINTS } from '@shared/api';

/**
 * Query key factory for food data
 */
export const foodKeys = {
  all: ['food'] as const,
  daily: (date: string) => [...foodKeys.all, 'daily', date] as const,
  history: (range: string) => [...foodKeys.all, 'history', range] as const,
  latest: () => [...foodKeys.all, 'latest'] as const,
};

/**
 * Fetch nutrition data for a specific date
 */
const fetchDailyNutrition = async (date: string) => {
  const params = new URLSearchParams({
    startDate: date,
    endDate: date,
  });
  
  const response = await apiClient.get(`${API_ENDPOINTS.METRICS_BY_CATEGORY('Nutrition')}?${params}`);
  return response;
};

/**
 * Fetch nutrition history for a date range
 */
const fetchNutritionHistory = async (startDate: string, endDate: string) => {
  const params = new URLSearchParams({
    startDate,
    endDate,
  });
  
  const response = await apiClient.get(`${API_ENDPOINTS.METRICS_BY_CATEGORY('Nutrition')}?${params}`);
  return response;
};

/**
 * Fetch latest nutrition metrics
 */
const fetchLatestNutrition = async () => {
  const response = await apiClient.get(API_ENDPOINTS.METRICS_BY_CATEGORY('Nutrition'));
  return response;
};

/**
 * Trigger Cronometer sync
 */
const syncCronometerData = async () => {
  // TODO: Implement when sync endpoint is available
  return { success: true, message: 'Sync started' };
};

/**
 * Hook for fetching daily nutrition
 */
export const useDailyNutrition = (date: string) => {
  return useQuery({
    queryKey: foodKeys.daily(date),
    queryFn: () => fetchDailyNutrition(date),
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
  });
};

/**
 * Hook for fetching latest nutrition
 */
export const useLatestNutrition = () => {
  return useQuery({
    queryKey: foodKeys.latest(),
    queryFn: fetchLatestNutrition,
    staleTime: 5 * 60 * 1000,
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
