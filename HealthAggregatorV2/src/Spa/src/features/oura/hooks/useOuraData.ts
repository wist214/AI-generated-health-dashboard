import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { get, API_ENDPOINTS } from '@shared/api';
import { syncOura } from '@shared/api/syncClient';
import type { OuraSleepData, OuraActivityData } from '../types';

/**
 * Oura Data API response (from /api/oura/data)
 */
interface OuraDataResponse {
  dailySleep: Array<{
    id: string;
    day: string;
    score: number | null;
    contributors?: {
      deepSleep?: number;
      efficiency?: number;
      latency?: number;
      remSleep?: number;
      restfulness?: number;
      timing?: number;
      totalSleep?: number;
    };
  }>;
  sleepRecords: Array<{
    id: string;
    day: string;
    totalSleepDuration: number | null;
    deepSleepDuration: number | null;
    remSleepDuration: number | null;
    lightSleepDuration: number | null;
    awakeTime: number | null;
    efficiency: number | null;
    latency: number | null;
    averageHeartRate: number | null;
    averageHrv: number | null;
    lowestHeartRate: number | null;
    averageBreath: number | null;
    bedtimeStart: string | null;
    bedtimeEnd: string | null;
    timeInBed: number | null;
    restlessPeriods: number | null;
    isLongSleep: boolean;
  }>;
  activity: Array<{
    id: string;
    day: string;
    score: number | null;
    activeCalories: number | null;
    totalCalories: number | null;
    steps: number | null;
    equivalentWalkingDistance: number | null;
    highActivityTime: number | null;
    mediumActivityTime: number | null;
    lowActivityTime: number | null;
    sedentaryTime: number | null;
    restingTime: number | null;
    inactivityAlerts: number | null;
  }>;
  readiness: Array<{
    id: string;
    day: string;
    score: number | null;
  }>;
  dailyStress: Array<{
    id: string;
    day: string;
    stressHigh: number | null;
    recoveryHigh: number | null;
    daySummary: string | null;
  }>;
  dailyResilience: Array<{
    id: string;
    day: string;
    level: string | null;
  }>;
  vo2Max: Array<{
    day: string;
    vo2Max: number | null;
  }>;
  cardiovascularAge: Array<{
    day: string;
    vascularAge: number | null;
  }>;
  spo2: Array<{
    id: string;
    day: string;
    spo2Percentage: { average: number } | null;
  }>;
  sleepTime: Array<{
    id: string;
    day: string;
    optimalBedtime: { start: number; end: number } | null;
    recommendation: string | null;
  }>;
  workouts: Array<{
    id: string;
    day: string;
  }>;
  lastSync: string | null;
}

/**
 * Query key factory for Oura data
 */
export const ouraKeys = {
  all: ['oura'] as const,
  sleep: (range: string) => [...ouraKeys.all, 'sleep', range] as const,
  activity: (range: string) => [...ouraKeys.all, 'activity', range] as const,
  readiness: (range: string) => [...ouraKeys.all, 'readiness', range] as const,
  stress: () => [...ouraKeys.all, 'stress'] as const,
  latest: () => [...ouraKeys.all, 'latest'] as const,
  dashboard: () => [...ouraKeys.all, 'dashboard'] as const,
  history: (range: string) => [...ouraKeys.all, 'history', range] as const,
};

/**
 * Transform API sleep data to OuraSleepData format
 * Merges dailySleep (scores) with sleepRecords (durations)
 */
const transformToSleepData = (ouraData: OuraDataResponse): OuraSleepData[] => {
  const sleepRecordsMap = new Map<string, OuraDataResponse['sleepRecords'][0]>();
  
  // Build map of sleepRecords by day (use the long_sleep record if multiple)
  ouraData.sleepRecords?.forEach(record => {
    const existing = sleepRecordsMap.get(record.day);
    if (!existing || record.isLongSleep) {
      sleepRecordsMap.set(record.day, record);
    }
  });

  // Merge dailySleep scores with sleepRecords durations
  return ouraData.dailySleep
    ?.filter(s => s.score !== null)
    .map(s => {
      const record = sleepRecordsMap.get(s.day);
      return {
        date: s.day,
        score: s.score,
        totalSleep: record?.totalSleepDuration ?? null,
        deepSleep: record?.deepSleepDuration ?? null,
        remSleep: record?.remSleepDuration ?? null,
        lightSleep: record?.lightSleepDuration ?? null,
        awakeTime: record?.awakeTime ?? null,
        sleepEfficiency: record?.efficiency ?? null,
        sleepLatency: record?.latency ?? null,
        avgHeartRate: record?.averageHeartRate ?? null,
        lowestHeartRate: record?.lowestHeartRate ?? null,
        avgHrv: record?.averageHrv ?? null,
        avgBreath: record?.averageBreath ?? null,
        bedtimeStart: record?.bedtimeStart ?? null,
        bedtimeEnd: record?.bedtimeEnd ?? null,
        timeInBed: record?.timeInBed ?? null,
        restlessPeriods: record?.restlessPeriods ?? null,
        contributors: s.contributors ? {
          deepSleep: s.contributors.deepSleep ?? null,
          efficiency: s.contributors.efficiency ?? null,
          latency: s.contributors.latency ?? null,
          remSleep: s.contributors.remSleep ?? null,
          restfulness: s.contributors.restfulness ?? null,
          timing: s.contributors.timing ?? null,
          totalSleep: s.contributors.totalSleep ?? null,
        } : null,
      };
    }) ?? [];
};

/**
 * Transform API activity data to OuraActivityData format
 */
const transformToActivityData = (ouraData: OuraDataResponse): OuraActivityData[] => {
  return ouraData.activity
    ?.filter(a => a.score !== null || a.steps !== null)
    .map(a => ({
      date: a.day,
      score: a.score,
      steps: a.steps,
      activeCalories: a.activeCalories,
      totalCalories: a.totalCalories,
      distance: a.equivalentWalkingDistance,
      highActivity: a.highActivityTime,
      mediumActivity: a.mediumActivityTime,
      lowActivity: a.lowActivityTime,
      sedentaryTime: a.sedentaryTime,
      restingTime: a.restingTime,
      inactivityAlerts: a.inactivityAlerts,
    })) ?? [];
};

/**
 * Fetch Oura data from /api/oura/data (contains sleepRecords with duration data)
 */
const fetchOuraData = async (): Promise<OuraDataResponse> => {
  return get<OuraDataResponse>(API_ENDPOINTS.OURA.DATA);
};

/**
 * Trigger Oura sync via Azure Functions
 */
const syncOuraData = async () => {
  return syncOura();
};

/**
 * Hook for fetching sleep data (from /api/oura/data which has sleepRecords)
 */
export const useSleepData = (_startDate: string, _endDate: string) => {
  return useQuery({
    queryKey: ouraKeys.sleep('all'),
    queryFn: fetchOuraData,
    staleTime: 5 * 60 * 1000,
    select: (data) => transformToSleepData(data),
  });
};

/**
 * Hook for fetching activity data (from /api/oura/data)
 */
export const useActivityData = (_startDate: string, _endDate: string) => {
  return useQuery({
    queryKey: ouraKeys.activity('all'),
    queryFn: fetchOuraData,
    staleTime: 5 * 60 * 1000,
    select: (data) => transformToActivityData(data),
  });
};

/**
 * Hook for fetching readiness data (from /api/oura/data)
 */
export const useReadinessData = (_startDate: string, _endDate: string) => {
  return useQuery({
    queryKey: ouraKeys.readiness('all'),
    queryFn: fetchOuraData,
    staleTime: 5 * 60 * 1000,
    select: (data) => data.readiness?.map(r => ({
      date: r.day,
      score: r.score,
    })) ?? [],
  });
};

/**
 * Hook for fetching stress data (from /api/oura/data)
 */
export const useStressData = () => {
  return useQuery({
    queryKey: ouraKeys.stress(),
    queryFn: fetchOuraData,
    staleTime: 5 * 60 * 1000,
    select: (data) => data.dailyStress?.map(s => ({
      day: s.day,
      stressHigh: s.stressHigh,
      recoveryHigh: s.recoveryHigh,
      daySummary: s.daySummary,
    })) ?? [],
  });
};

/**
 * Hook for fetching Oura history (combined sleep and activity from /api/oura/data)
 */
export const useOuraHistory = (_startDate: string, _endDate: string) => {
  return useQuery({
    queryKey: ouraKeys.history('all'),
    queryFn: fetchOuraData,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook for fetching latest Oura metrics (uses oura data endpoint for full data)
 */
export const useLatestOura = () => {
  return useQuery({
    queryKey: ouraKeys.latest(),
    queryFn: fetchOuraData,
    staleTime: 5 * 60 * 1000,
    select: (data) => {
      // Get the most recent sleep record (long sleep preferred)
      const sleepRecordsMap = new Map<string, typeof data.sleepRecords[0]>();
      data.sleepRecords?.forEach(record => {
        const existing = sleepRecordsMap.get(record.day);
        if (!existing || record.isLongSleep) {
          sleepRecordsMap.set(record.day, record);
        }
      });
      const latestSleepRecord = data.sleepRecords?.length 
        ? sleepRecordsMap.get(data.sleepRecords[0]?.day ?? '') 
        : null;
      
      const latestDailySleep = data.dailySleep?.[0];
      const latestReadiness = data.readiness?.[0];
      const latestActivity = data.activity?.[0];
      const latestStress = data.dailyStress?.[0];
      const latestResilience = data.dailyResilience?.[0];
      const latestVo2Max = data.vo2Max?.[0];
      const latestCardioAge = data.cardiovascularAge?.[0];
      const latestSpo2 = data.spo2?.[0];
      const latestSleepTime = data.sleepTime?.[0];
      
      // Get today's workout count
      const today = new Date().toISOString().split('T')[0];
      const todayWorkouts = data.workouts?.filter(w => w.day === today) ?? [];
      
      // Determine stress level from daySummary or stressHigh value
      let stressLevel: string | null = null;
      if (latestStress?.daySummary) {
        stressLevel = latestStress.daySummary;
      } else if (latestStress?.stressHigh !== null && latestStress?.stressHigh !== undefined) {
        // Convert stress high seconds to level
        const stressMinutes = latestStress.stressHigh / 60;
        if (stressMinutes < 30) stressLevel = 'low';
        else if (stressMinutes < 60) stressLevel = 'normal';
        else if (stressMinutes < 120) stressLevel = 'elevated';
        else stressLevel = 'high';
      }
      
      return {
        sleepScore: latestDailySleep?.score ?? null,
        readinessScore: latestReadiness?.score ?? null,
        activityScore: latestActivity?.score ?? null,
        totalSleepHours: latestSleepRecord?.totalSleepDuration ? latestSleepRecord.totalSleepDuration / 3600 : null,
        deepSleepHours: latestSleepRecord?.deepSleepDuration ? latestSleepRecord.deepSleepDuration / 3600 : null,
        remSleepHours: latestSleepRecord?.remSleepDuration ? latestSleepRecord.remSleepDuration / 3600 : null,
        sleepEfficiency: latestSleepRecord?.efficiency ?? null,
        steps: latestActivity?.steps ?? null,
        activeCalories: latestActivity?.activeCalories ?? null,
        heartRateAvg: latestSleepRecord?.averageHeartRate ?? null,
        hrvAverage: latestSleepRecord?.averageHrv ?? null,
        // Advanced Oura metrics
        dailyStress: stressLevel,
        resilienceLevel: latestResilience?.level ?? null,
        vo2Max: latestVo2Max?.vo2Max ?? null,
        cardiovascularAge: latestCardioAge?.vascularAge ?? null,
        spO2Average: latestSpo2?.spo2Percentage?.average ?? null,
        optimalBedtimeStart: latestSleepTime?.optimalBedtime?.start ?? null,
        optimalBedtimeEnd: latestSleepTime?.optimalBedtime?.end ?? null,
        workoutCount: todayWorkouts.length,
        lastUpdated: data.lastSync ?? null,
      };
    },
  });
};

/**
 * Hook for Oura data sync
 */
export const useOuraSync = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: syncOuraData,
    onSuccess: async () => {
      // Invalidate all oura-related queries
      await queryClient.invalidateQueries({ queryKey: ouraKeys.all });
      // Force refetch to ensure UI updates with fresh data
      await queryClient.refetchQueries({ queryKey: ouraKeys.all, type: 'active' });
    },
  });
};
