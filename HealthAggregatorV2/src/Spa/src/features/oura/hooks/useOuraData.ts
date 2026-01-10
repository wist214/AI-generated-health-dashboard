import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient, API_ENDPOINTS } from '@shared/api';

/**
 * Query key factory for Oura data
 */
export const ouraKeys = {
  all: ['oura'] as const,
  sleep: (range: string) => [...ouraKeys.all, 'sleep', range] as const,
  activity: (range: string) => [...ouraKeys.all, 'activity', range] as const,
  readiness: (range: string) => [...ouraKeys.all, 'readiness', range] as const,
  latest: () => [...ouraKeys.all, 'latest'] as const,
};

/**
 * Fetch sleep metrics for a date range
 */
const fetchSleepData = async (startDate: string, endDate: string) => {
  const params = new URLSearchParams({
    startDate,
    endDate,
  });
  
  const response = await apiClient.get(`${API_ENDPOINTS.METRICS_BY_CATEGORY('Sleep')}?${params}`);
  return response;
};

/**
 * Fetch activity metrics for a date range
 */
const fetchActivityData = async (startDate: string, endDate: string) => {
  const params = new URLSearchParams({
    startDate,
    endDate,
  });
  
  const response = await apiClient.get(`${API_ENDPOINTS.METRICS_BY_CATEGORY('Activity')}?${params}`);
  return response;
};

/**
 * Fetch latest Oura metrics
 */
const fetchLatestOura = async () => {
  // Fetch from multiple categories
  const [sleepData, activityData, heartData] = await Promise.all([
    apiClient.get(API_ENDPOINTS.METRICS_BY_CATEGORY('Sleep')),
    apiClient.get(API_ENDPOINTS.METRICS_BY_CATEGORY('Activity')),
    apiClient.get(API_ENDPOINTS.METRICS_BY_CATEGORY('Heart')),
  ]);
  
  return {
    sleep: sleepData,
    activity: activityData,
    heart: heartData,
  };
};

/**
 * Trigger Oura sync
 */
const syncOuraData = async () => {
  // TODO: Implement when sync endpoint is available
  return { success: true, message: 'Sync started' };
};

/**
 * Hook for fetching sleep data
 */
export const useSleepData = (startDate: string, endDate: string) => {
  return useQuery({
    queryKey: ouraKeys.sleep(`${startDate}-${endDate}`),
    queryFn: () => fetchSleepData(startDate, endDate),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching activity data
 */
export const useActivityData = (startDate: string, endDate: string) => {
  return useQuery({
    queryKey: ouraKeys.activity(`${startDate}-${endDate}`),
    queryFn: () => fetchActivityData(startDate, endDate),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching latest Oura metrics
 */
export const useLatestOura = () => {
  return useQuery({
    queryKey: ouraKeys.latest(),
    queryFn: fetchLatestOura,
    staleTime: 5 * 60 * 1000,
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
