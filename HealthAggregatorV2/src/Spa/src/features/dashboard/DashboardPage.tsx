import React, { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { MetricCard } from './components/MetricCard';
import { QuickStats } from './components/QuickStats';
import { WeeklyTrends } from './components/WeeklyTrends';
import { Insights } from './components/Insights';
import { useDashboardSummary } from './hooks/useDashboardData';
import { LoadingSpinner } from '@shared/components/LoadingSpinner';
import { ErrorMessage } from '@shared/components/ErrorMessage';
import { formatDate, formatNumber } from '@shared/utils';
import styles from './DashboardPage.module.css';
import type { QuickStat, ProgressItem } from './types';

/**
 * Interface for oura data response (sleep records with durations)
 */
interface OuraDataSleepRecord {
  day: string;
  totalSleepDuration: number | null;
  averageHeartRate: number | null;
}

interface OuraDataResponse {
  sleepRecords: OuraDataSleepRecord[];
}

/**
 * Calculate progress comparison values
 */
function calculateProgressValue(current: number | null, previous: number | null, unit = ''): ProgressItem {
  if (current === null || previous === null) {
    return { label: '', value: '--', type: 'neutral' };
  }
  const diff = current - previous;
  const sign = diff >= 0 ? '+' : '';
  return {
    label: '',
    value: `${sign}${diff.toFixed(1)}${unit}`,
    type: diff > 0 ? 'positive' : diff < 0 ? 'negative' : 'neutral',
  };
}

/**
 * Find data point from approximately N days ago
 */
function findDataPoint<T extends { day?: string; date?: string }>(
  data: T[] | undefined,
  daysAgo: number
): T | null {
  if (!data || data.length === 0) return null;
  
  const targetDate = new Date();
  targetDate.setDate(targetDate.getDate() - daysAgo);
  const targetStr = targetDate.toISOString().substring(0, 10);
  
  // Sort by date descending
  const sorted = [...data].sort((a, b) => {
    const dateA = a.day ?? a.date ?? '';
    const dateB = b.day ?? b.date ?? '';
    return dateB.localeCompare(dateA);
  });
  
  // Find closest date before or equal to target
  for (const item of sorted) {
    const itemDate = item.day ?? item.date ?? '';
    if (itemDate <= targetStr) {
      return item;
    }
  }
  return null;
}

/**
 * Dashboard page component
 * Main overview page showing key health metrics
 */
export const DashboardPage: React.FC = () => {
  const { data, isLoading, error, refetch } = useDashboardSummary();
  
  // Also fetch oura data for sleep duration and heart rate (not in dashboard API)
  const { data: ouraData } = useQuery<OuraDataResponse>({
    queryKey: ['oura', 'data'],
    queryFn: async () => {
      const response = await fetch('/api/oura/data');
      if (!response.ok) throw new Error('Failed to fetch oura data');
      return response.json();
    },
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });

  // Extract data from actual API response (safe to call even if data is null)
  const latest = data?.latest;
  const oura = data?.oura;
  const picooc = data?.picooc;
  const latestActivity = oura?.activity?.[0];
  
  // Get sleep record from oura data (has duration and heart rate)
  const latestSleepRecord = useMemo(() => {
    if (!ouraData?.sleepRecords?.length) return null;
    // Sort by day descending and get the most recent long sleep
    const sorted = [...ouraData.sleepRecords].sort((a, b) => b.day.localeCompare(a.day));
    return sorted[0];
  }, [ouraData]);

  // Build weekly trends data from available data
  // Note: hooks must be called unconditionally (before early returns)
  const weeklyTrendsData = useMemo(() => {
    if (!oura && !picooc && !latest) return [];
    
    const trends: Array<{ date: string; sleepScore: number | null; readinessScore: number | null; weight: number | null }> = [];
    
    // Get last 7 days of data
    const last7Days: string[] = [];
    const today = new Date();
    for (let i = 6; i >= 0; i--) {
      const d = new Date(today);
      d.setDate(d.getDate() - i);
      const dateStr = d.toISOString().substring(0, 10);
      last7Days.push(dateStr);
    }

    // Build a map by date
    const dataByDate = new Map<string, { sleepScore: number | null; readinessScore: number | null; weight: number | null }>();
    
    // Initialize all dates
    for (const date of last7Days) {
      dataByDate.set(date, { sleepScore: null, readinessScore: null, weight: null });
    }

    // Add sleep scores
    oura?.dailySleep?.forEach(s => {
      if (dataByDate.has(s.day)) {
        const existing = dataByDate.get(s.day)!;
        existing.sleepScore = s.score;
      }
    });

    // Add readiness scores
    oura?.readiness?.forEach(r => {
      if (dataByDate.has(r.day)) {
        const existing = dataByDate.get(r.day)!;
        existing.readinessScore = r.score;
      }
    });

    // Add weight data from picooc measurements
    // Note: Picooc dates are full ISO timestamps, need to extract YYYY-MM-DD part
    picooc?.measurements?.forEach(m => {
      const dateOnly = m.date.substring(0, 10); // Extract YYYY-MM-DD from ISO timestamp
      if (dataByDate.has(dateOnly)) {
        const existing = dataByDate.get(dateOnly)!;
        existing.weight = m.weight;
      }
    });

    // Also add the latest weight if we have it and it's in the last 7 days
    if (latest?.weight && latest?.weightDate) {
      const latestDateOnly = latest.weightDate.substring(0, 10);
      if (dataByDate.has(latestDateOnly)) {
        const existing = dataByDate.get(latestDateOnly)!;
        // Only set if not already set (prefer latest over measurements)
        if (existing.weight === null) {
          existing.weight = latest.weight;
        }
      }
    }

    // Convert to array sorted by date
    for (const date of last7Days) {
      const d = dataByDate.get(date);
      if (d) {
        trends.push({ date, ...d });
      }
    }

    return trends.sort((a, b) => a.date.localeCompare(b.date));
  }, [oura, picooc, latest]);

  // Calculate average sleep score for insights
  const avgSleepScore = useMemo(() => {
    const scores = oura?.dailySleep?.filter(s => s.score !== null).map(s => s.score!) ?? [];
    return scores.length > 0 ? scores.reduce((a, b) => a + b, 0) / scores.length : null;
  }, [oura]);

  // Get weight from a week ago for insights
  const weekAgoWeight = useMemo(() => {
    if (!picooc?.measurements || picooc.measurements.length < 2) return null;
    const sorted = [...picooc.measurements].sort((a, b) => b.date.localeCompare(a.date));
    // Find measurement from about a week ago
    const today = new Date();
    const weekAgo = new Date(today);
    weekAgo.setDate(weekAgo.getDate() - 7);
    
    const weekAgoMeasurement = sorted.find(m => new Date(m.date) <= weekAgo);
    return weekAgoMeasurement?.weight ?? null;
  }, [picooc]);

  // Calculate weight progress values
  const weightProgress = useMemo((): ProgressItem[] => {
    const currentWeight = latest?.weight ?? null;
    const measurements = picooc?.measurements ?? [];
    const sorted = [...measurements].sort((a, b) => b.date.localeCompare(a.date));
    
    // Previous measurement (index 1 since index 0 is latest)
    const prevWeight = sorted[1]?.weight ?? null;
    // ~1 week ago
    const weekAgoData = findDataPoint(measurements, 7);
    // ~1 month ago
    const monthAgoData = findDataPoint(measurements, 30);
    
    return [
      { ...calculateProgressValue(currentWeight, prevWeight, 'kg'), label: 'vs Previous' },
      { ...calculateProgressValue(currentWeight, weekAgoData?.weight ?? null, 'kg'), label: 'vs ~1 Week' },
      { ...calculateProgressValue(currentWeight, monthAgoData?.weight ?? null, 'kg'), label: 'vs ~1 Month' },
    ];
  }, [latest?.weight, picooc?.measurements]);

  // Calculate body fat progress values
  const bodyFatProgress = useMemo((): ProgressItem[] => {
    const currentBodyFat = latest?.bodyFat ?? null;
    const measurements = picooc?.measurements ?? [];
    const sorted = [...measurements].sort((a, b) => b.date.localeCompare(a.date));
    
    const prevBodyFat = sorted[1]?.bodyFat ?? null;
    const weekAgoData = findDataPoint(measurements, 7);
    const monthAgoData = findDataPoint(measurements, 30);
    
    return [
      { ...calculateProgressValue(currentBodyFat, prevBodyFat, '%'), label: 'vs Previous' },
      { ...calculateProgressValue(currentBodyFat, weekAgoData?.bodyFat ?? null, '%'), label: 'vs ~1 Week' },
      { ...calculateProgressValue(currentBodyFat, monthAgoData?.bodyFat ?? null, '%'), label: 'vs ~1 Month' },
    ];
  }, [latest?.bodyFat, picooc?.measurements]);

  // Calculate sleep score progress values
  const sleepProgress = useMemo((): ProgressItem[] => {
    const currentScore = latest?.sleepScore ?? null;
    const sleepData = oura?.dailySleep ?? [];
    const sorted = [...sleepData].sort((a, b) => b.day.localeCompare(a.day));
    
    const prevScore = sorted[1]?.score ?? null;
    const weekAgoData = findDataPoint(sleepData, 7);
    const monthAgoData = findDataPoint(sleepData, 30);
    
    return [
      { ...calculateProgressValue(currentScore, prevScore), label: 'vs Yesterday' },
      { ...calculateProgressValue(currentScore, weekAgoData?.score ?? null), label: 'vs 1 Week' },
      { ...calculateProgressValue(currentScore, monthAgoData?.score ?? null), label: 'vs 1 Month' },
    ];
  }, [latest?.sleepScore, oura?.dailySleep]);

  // Calculate readiness progress values
  const readinessProgress = useMemo((): ProgressItem[] => {
    const currentScore = latest?.readinessScore ?? null;
    const readinessData = oura?.readiness ?? [];
    const sorted = [...readinessData].sort((a, b) => b.day.localeCompare(a.day));
    
    const prevScore = sorted[1]?.score ?? null;
    const weekAgoData = findDataPoint(readinessData, 7);
    const monthAgoData = findDataPoint(readinessData, 30);
    
    return [
      { ...calculateProgressValue(currentScore, prevScore), label: 'vs Yesterday' },
      { ...calculateProgressValue(currentScore, weekAgoData?.score ?? null), label: 'vs 1 Week' },
      { ...calculateProgressValue(currentScore, monthAgoData?.score ?? null), label: 'vs 1 Month' },
    ];
  }, [latest?.readinessScore, oura?.readiness]);

  // Early returns AFTER all hooks
  if (isLoading) {
    return <LoadingSpinner label="Loading dashboard..." />;
  }

  if (error) {
    return (
      <ErrorMessage
        title="Failed to load dashboard"
        message={error.message}
        onRetry={() => refetch()}
      />
    );
  }

  if (!data) {
    return (
      <ErrorMessage
        title="No data available"
        message="Dashboard data could not be loaded."
        onRetry={() => refetch()}
      />
    );
  }

  const quickStats: QuickStat[] = [
    {
      icon: '🏃',
      label: 'Steps Today',
      value: latest?.steps ? formatNumber(latest.steps) : '--',
    },
    {
      icon: '🛏️',
      label: 'Sleep Duration',
      value: latestSleepRecord?.totalSleepDuration 
        ? `${Math.floor(latestSleepRecord.totalSleepDuration / 3600)}h ${Math.floor((latestSleepRecord.totalSleepDuration % 3600) / 60)}m`
        : '--',
    },
    {
      icon: '❤️',
      label: 'Avg Heart Rate',
      value: latestSleepRecord?.averageHeartRate ? `${Math.round(latestSleepRecord.averageHeartRate)} bpm` : '--',
    },
    {
      icon: '🔥',
      label: 'Calories Burned',
      value: latestActivity?.totalCalories ? formatNumber(latestActivity.totalCalories) : '--',
    },
  ];

  return (
    <div className={styles.dashboard}>
      <div className={styles.dashboardHeader}>
        <h2>🎯 Your Health Dashboard</h2>
        <p className={styles.date}>{latest?.sleepDate ? formatDate(latest.sleepDate) : 'No data'}</p>
      </div>

      <div className={styles.dashboardGrid}>
        {/* Weight Card */}
        <MetricCard
          title="Weight"
          value={latest?.weight ? latest.weight.toFixed(1) : null}
          unit="kg"
          icon="⚖️"
          source="Picooc"
          date={latest?.weightDate ? formatDate(latest.weightDate) : 'No data'}
          variant="weight"
          progress={weightProgress}
        />

        {/* Body Fat Card */}
        <MetricCard
          title="Body Fat"
          value={latest?.bodyFat ? latest.bodyFat.toFixed(1) : null}
          unit="%"
          icon="💪"
          source="Picooc"
          date={latest?.weightDate ? formatDate(latest.weightDate) : 'No data'}
          variant="bodyScore"
          progress={bodyFatProgress}
        />

        {/* Sleep Score Card */}
        <MetricCard
          title="Sleep Score"
          value={latest?.sleepScore ?? null}
          icon="😴"
          source="Oura Ring"
          date={latest?.sleepDate ? formatDate(latest.sleepDate) : 'No data'}
          variant="sleep"
          progress={sleepProgress}
        />

        {/* Readiness Card */}
        <MetricCard
          title="Readiness"
          value={latest?.readinessScore ?? null}
          icon="⚡"
          source="Oura Ring"
          date={latest?.readinessDate ? formatDate(latest.readinessDate) : 'No data'}
          variant="readiness"
          progress={readinessProgress}
        />
      </div>

      <QuickStats stats={quickStats} />

      <WeeklyTrends data={weeklyTrendsData} />

      <Insights
        data={{
          latest: {
            weight: latest?.weight,
            bodyFat: latest?.bodyFat,
            weightDate: latest?.weightDate,
            sleepScore: latest?.sleepScore,
            readinessScore: latest?.readinessScore,
            steps: latest?.steps,
          },
          weekAgo: {
            weight: weekAgoWeight,
          },
          avgSleepScore,
        }}
      />
    </div>
  );
};

export default DashboardPage;
