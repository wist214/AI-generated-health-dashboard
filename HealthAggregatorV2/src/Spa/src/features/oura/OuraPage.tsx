import React, { useState, useMemo } from 'react';
import { Button } from '@fluentui/react-components';
import { 
  OuraStatCard, 
  CollapsibleSection, 
  OuraChart, 
  SleepTable, 
  ActivityTable,
  StressModal
} from './components';
import { useLatestOura, useSleepData, useActivityData, useReadinessData, useStressData, useOuraSync } from './hooks';
import { LoadingSpinner } from '@shared/components/LoadingSpinner';
import { ErrorMessage } from '@shared/components/ErrorMessage';
import { formatDuration, getDateRangeFromTimeRange, formatRelativeTime } from '@shared/utils';
import styles from './OuraPage.module.css';
import type { TimeRange, ChartSeries } from './types';

// Helper function to format bedtime offset (seconds from midnight) to readable time
const formatBedtimeOffset = (offsetSeconds: number | null): string | null => {
  if (offsetSeconds === null) return null;
  
  const totalMinutes = Math.floor(offsetSeconds / 60);
  let hours = Math.floor(totalMinutes / 60);
  const minutes = Math.abs(totalMinutes % 60);
  
  // Handle negative offsets (before midnight)
  if (hours < 0) {
    hours = 24 + hours;
  }
  
  const period = hours >= 12 ? 'PM' : 'AM';
  const displayHours = hours > 12 ? hours - 12 : (hours === 0 ? 12 : hours);
  
  return `${displayHours}:${minutes.toString().padStart(2, '0')} ${period}`;
};

// Default score chart series (using 'score' from OuraSleepData, merged with activity scores)
const defaultScoreSeries: ChartSeries[] = [
  { id: 'sleepScore', label: 'Sleep', color: '#3b82f6', enabled: true },
  { id: 'activityScore', label: 'Activity', color: '#f59e0b', enabled: true },
  { id: 'steps', label: 'Steps', color: '#8b5cf6', enabled: false },
];

// Default sleep chart series (values in hours for display)
const defaultSleepSeries: ChartSeries[] = [
  { id: 'totalSleepHours', label: 'Total Sleep', color: '#3b82f6', enabled: true },
  { id: 'deepSleepHours', label: 'Deep Sleep', color: '#1e40af', enabled: true },
  { id: 'remSleepHours', label: 'REM Sleep', color: '#8b5cf6', enabled: true },
  { id: 'avgHeartRate', label: 'Avg HR', color: '#ef4444', enabled: false },
];

// Helper to calculate statistics from an array of numbers
const calculateStats = (values: (number | null | undefined)[]) => {
  const validValues = values.filter((v): v is number => v !== null && v !== undefined);
  if (validValues.length === 0) {
    return { min: null, max: null, avg: null };
  }
  return {
    min: Math.min(...validValues),
    max: Math.max(...validValues),
    avg: validValues.reduce((a, b) => a + b, 0) / validValues.length,
  };
};

/**
 * Oura Ring data page
 * Displays Sleep, Readiness, and Activity data
 */
export const OuraPage: React.FC = () => {
  const [timeRange, setTimeRange] = useState<TimeRange>('1y');
  const [scoreSeries, setScoreSeries] = useState<ChartSeries[]>(defaultScoreSeries);
  const [sleepSeries, setSleepSeries] = useState<ChartSeries[]>(defaultSleepSeries);
  const [isStressModalOpen, setIsStressModalOpen] = useState(false);

  // Get date range for history queries
  const { startDate, endDate } = useMemo(() => 
    getDateRangeFromTimeRange(timeRange),
    [timeRange]
  );

  // Fetch data from API
  const { data: latestData, isLoading: isLoadingLatest, error: latestError, refetch } = useLatestOura();
  const { data: sleepData, isLoading: isLoadingSleep, error: sleepError } = useSleepData(startDate, endDate);
  const { data: activityData, isLoading: isLoadingActivity, error: activityError } = useActivityData(startDate, endDate);
  const { data: readinessData, isLoading: isLoadingReadiness, error: readinessError } = useReadinessData(startDate, endDate);
  const { data: stressData } = useStressData();
  const { mutate: syncData, isPending: isSyncing } = useOuraSync();

  const isLoading = isLoadingLatest || isLoadingSleep || isLoadingActivity || isLoadingReadiness;
  const error = latestError || sleepError || activityError || readinessError;

  // Transform sleep data for charts (convert seconds to hours for display)
  // Filter by time range
  const scoreChartData = useMemo(() => {
    if (!sleepData || !activityData) return [];
    
    // Create a map of activity data by date
    const activityByDate = new Map(activityData.map(a => [a.date, a]));
    
    return sleepData
      .filter(d => d.date >= startDate && d.date <= endDate)
      .map(d => {
        const activity = activityByDate.get(d.date);
        return {
          date: d.date,
          sleepScore: d.score,
          activityScore: activity?.score ?? null,
          steps: activity?.steps ?? null,
        };
      });
  }, [sleepData, activityData, startDate, endDate]);

  const sleepChartData = useMemo(() => {
    if (!sleepData) return [];
    return sleepData
      .filter(d => d.date >= startDate && d.date <= endDate)
      .map(d => ({
        date: d.date,
        totalSleepHours: d.totalSleep !== null ? d.totalSleep / 3600 : null,
        deepSleepHours: d.deepSleep !== null ? d.deepSleep / 3600 : null,
        remSleepHours: d.remSleep !== null ? d.remSleep / 3600 : null,
        avgHeartRate: d.avgHeartRate,
      }));
  }, [sleepData, startDate, endDate]);

  // Calculate statistics for each metric
  const sleepScoreStats = useMemo(() => calculateStats(sleepData?.map(d => d.score) ?? []), [sleepData]);
  const readinessScoreStats = useMemo(() => calculateStats(readinessData?.map(d => d.score) ?? []), [readinessData]);
  const activityScoreStats = useMemo(() => calculateStats(activityData?.map(d => d.score) ?? []), [activityData]);
  const sleepHoursStats = useMemo(() => {
    const hours = sleepData?.map(d => d.totalSleep !== null ? d.totalSleep / 3600 : null) ?? [];
    return calculateStats(hours);
  }, [sleepData]);

  const handleScoreSeriesToggle = (id: string) => {
    setScoreSeries(prev => prev.map(s => 
      s.id === id ? { ...s, enabled: !s.enabled } : s
    ));
  };

  const handleSleepSeriesToggle = (id: string) => {
    setSleepSeries(prev => prev.map(s => 
      s.id === id ? { ...s, enabled: !s.enabled } : s
    ));
  };

  const handleSync = () => {
    syncData();
  };

  // Extract latest values from dashboard summary
  const sleepScore = latestData?.sleepScore ?? null;
  const readinessScore = latestData?.readinessScore ?? null;
  const activityScore = latestData?.activityScore ?? null;
  // Get sleep hours from latestData, or fall back to most recent sleep record
  const latestSleepHours = latestData?.totalSleepHours ?? null;
  const recentSleepHours = sleepData?.[0]?.totalSleep !== null && sleepData?.[0]?.totalSleep !== undefined 
    ? sleepData[0].totalSleep / 3600 
    : null;
  const sleepHours = latestSleepHours ?? recentSleepHours;
  const dailyStress = latestData?.dailyStress ?? null;
  const resilienceLevel = latestData?.resilienceLevel ?? null;
  const vo2Max = latestData?.vo2Max ?? null;
  const cardiovascularAge = latestData?.cardiovascularAge ?? null;
  const spO2Average = latestData?.spO2Average ?? null;
  const optimalBedtimeStart = latestData?.optimalBedtimeStart ?? null;
  const optimalBedtimeEnd = latestData?.optimalBedtimeEnd ?? null;
  const workoutCount = latestData?.workoutCount ?? null;
  const lastUpdated = latestData?.lastUpdated;

  if (isLoading) {
    return <LoadingSpinner label="Loading Oura data..." />;
  }

  if (error) {
    return (
      <ErrorMessage
        title="Failed to load Oura data"
        message={(error as Error).message}
        onRetry={() => refetch()}
      />
    );
  }

  return (
    <div className={styles.ouraPage}>
      {/* Controls */}
      <div className={styles.controls}>
        <Button
          appearance="primary"
          onClick={handleSync}
          disabled={isSyncing}
          className={styles.syncBtn}
        >
          {isSyncing ? '🔄 Syncing...' : '🔄 Sync Oura Data'}
        </Button>
        {lastUpdated && (
          <span className={styles.lastUpdated}>
            Last updated: {formatRelativeTime(lastUpdated)}
          </span>
        )}
      </div>

      {/* Primary Scores */}
      <div className={styles.statsGrid}>
        <OuraStatCard
          title="Sleep Score"
          value={sleepScore}
          variant="score"
          statistics={sleepScoreStats}
        />
        <OuraStatCard
          title="Readiness Score"
          value={readinessScore}
          variant="score"
          statistics={readinessScoreStats}
        />
        <OuraStatCard
          title="Activity Score"
          value={activityScore}
          variant="score"
          statistics={activityScoreStats}
        />
        <OuraStatCard
          title="Sleep Duration"
          value={sleepHours !== null ? formatDuration(sleepHours * 60) : null}
          variant="metric"
          statistics={sleepHoursStats}
        />
      </div>

      {/* Advanced Health Metrics */}
      <CollapsibleSection title="Advanced Health Metrics" icon="🧬" defaultExpanded={false}>
        <div className={styles.statsGridSmall}>
          <OuraStatCard
            title="Daily Stress"
            icon="🧘"
            value={dailyStress ? dailyStress.charAt(0).toUpperCase() + dailyStress.slice(1) : null}
            variant="stress"
            onClick={() => setIsStressModalOpen(true)}
          />
          <OuraStatCard
            title="Resilience"
            icon="💪"
            value={resilienceLevel ? resilienceLevel.charAt(0).toUpperCase() + resilienceLevel.slice(1) : null}
            variant="resilience"
          />
          <OuraStatCard
            title="VO2 Max"
            icon="🫀"
            value={vo2Max !== null ? vo2Max.toFixed(1) : null}
            unit="ml/kg/min"
            variant="vo2"
          />
          <OuraStatCard
            title="Cardio Age"
            icon="❤️"
            value={cardiovascularAge}
            unit="years"
            variant="cardio"
          />
        </div>
      </CollapsibleSection>

      {/* Recovery & Vitals */}
      <CollapsibleSection title="Recovery & Vitals" icon="💓" defaultExpanded={false}>
        <div className={styles.statsGridSmall}>
          <OuraStatCard
            title="SpO2"
            icon="🩸"
            value={spO2Average !== null ? spO2Average.toFixed(1) : null}
            unit="%"
            variant="spo2"
          />
          <OuraStatCard
            title="Optimal Bedtime"
            icon="🛏️"
            value={
              optimalBedtimeStart !== null && optimalBedtimeEnd !== null
                ? `${formatBedtimeOffset(optimalBedtimeStart)} - ${formatBedtimeOffset(optimalBedtimeEnd)}`
                : null
            }
            variant="bedtime"
          />
          <OuraStatCard
            title="Workouts"
            icon="🏋️"
            value={workoutCount}
            variant="workout"
          />
        </div>
      </CollapsibleSection>

      {/* Score Chart */}
      <div className={styles.chartSection}>
        <OuraChart
          title="📊 Health Scores"
          data={scoreChartData}
          series={scoreSeries}
          onSeriesToggle={handleScoreSeriesToggle}
          timeRange={timeRange}
          onTimeRangeChange={setTimeRange}
        />
      </div>

      {/* Sleep Duration Chart */}
      <OuraChart
        title="😴 Sleep Duration"
        data={sleepChartData}
        series={sleepSeries}
        onSeriesToggle={handleSleepSeriesToggle}
        yAxisLabel="Hours"
      />

      {/* Sleep History Table */}
      <SleepTable data={sleepData ?? []} />

      {/* Activity History Table */}
      <ActivityTable data={activityData ?? []} />

      {/* Stress Detail Modal */}
      <StressModal
        isOpen={isStressModalOpen}
        onClose={() => setIsStressModalOpen(false)}
        stressData={stressData ?? []}
        latestStress={stressData?.[0] ?? null}
      />
    </div>
  );
};

export default OuraPage;
