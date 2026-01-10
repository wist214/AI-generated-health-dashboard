import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient, API_ENDPOINTS } from '@shared/api';

/**
 * Query key factory for weight data
 */
export const weightKeys = {
  all: ['weight'] as const,
  history: (range: string) => [...weightKeys.all, 'history', range] as const,
  latest: () => [...weightKeys.all, 'latest'] as const,
};

/**
 * Fetch weight metrics for a date range
 */
const fetchWeightHistory = async (startDate: string, endDate: string) => {
  const params = new URLSearchParams({
    startDate,
    endDate,
    metricTypes: 'weight,body_fat,bmi,skeletal_muscle_mass,body_water,metabolic_age,visceral_fat,bone_mass'
  });
  
  const response = await apiClient.get(`${API_ENDPOINTS.METRICS_RANGE}?${params}`);
  return response;
};

/**
 * Fetch latest weight metrics
 */
const fetchLatestWeight = async () => {
  const response = await apiClient.get(API_ENDPOINTS.METRICS_BY_CATEGORY('Body'));
  return response;
};

/**
 * Trigger Picooc sync
 */
const syncPicoocData = async () => {
  // TODO: Implement when sync endpoint is available
  return { success: true, message: 'Sync started' };
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
 * Hook for fetching latest weight metrics
 */
export const useLatestWeight = () => {
  return useQuery({
    queryKey: weightKeys.latest(),
    queryFn: fetchLatestWeight,
    staleTime: 5 * 60 * 1000, // 5 minutes
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
