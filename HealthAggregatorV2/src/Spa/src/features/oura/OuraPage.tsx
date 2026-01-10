import React, { useState, useMemo } from 'react';
import { Button } from '@fluentui/react-components';
import { 
  OuraStatCard, 
  CollapsibleSection, 
  OuraChart, 
  SleepTable, 
  ActivityTable 
} from './components';
import { TimeRangeSelector } from '../weight/components';
import { useLatestOura, useSleepData, useActivityData, useOuraSync } from './hooks';
import { LoadingSpinner } from '@shared/components/LoadingSpinner';
import { ErrorMessage } from '@shared/components/ErrorMessage';
import { formatDuration, getDateRangeFromTimeRange, formatRelativeTime } from '@shared/utils';
import styles from './OuraPage.module.css';
import type { TimeRange, ChartSeries } from './types';

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

/**
 * Oura Ring data page
 * Displays Sleep, Readiness, and Activity data
 */
export const OuraPage: React.FC = () => {
  const [timeRange, setTimeRange] = useState<TimeRange>('1y');
  const [scoreSeries, setScoreSeries] = useState<ChartSeries[]>(defaultScoreSeries);
  const [sleepSeries, setSleepSeries] = useState<ChartSeries[]>(defaultSleepSeries);

  // Get date range for history queries
  const { startDate, endDate } = useMemo(() => 
    getDateRangeFromTimeRange(timeRange),
    [timeRange]
  );

  // Fetch data from API
  const { data: latestData, isLoading: isLoadingLatest, error: latestError, refetch } = useLatestOura();
  const { data: sleepData, isLoading: isLoadingSleep, error: sleepError } = useSleepData(startDate, endDate);
  const { data: activityData, isLoading: isLoadingActivity, error: activityError } = useActivityData(startDate, endDate);
  const { mutate: syncData, isPending: isSyncing } = useOuraSync();

  const isLoading = isLoadingLatest || isLoadingSleep || isLoadingActivity;
  const error = latestError || sleepError || activityError;

  // Transform sleep data for charts (convert seconds to hours for display)
  const scoreChartData = useMemo(() => {
    if (!sleepData || !activityData) return [];
    
    // Create a map of activity data by date
    const activityByDate = new Map(activityData.map(a => [a.date, a]));
    
    return sleepData.map(d => {
      const activity = activityByDate.get(d.date);
      return {
        date: d.date,
        sleepScore: d.score,
        activityScore: activity?.score ?? null,
        steps: activity?.steps ?? null,
      };
    });
  }, [sleepData, activityData]);

  const sleepChartData = useMemo(() => {
    if (!sleepData) return [];
    return sleepData.map(d => ({
      date: d.date,
      totalSleepHours: d.totalSleep !== null ? d.totalSleep / 3600 : null,
      deepSleepHours: d.deepSleep !== null ? d.deepSleep / 3600 : null,
      remSleepHours: d.remSleep !== null ? d.remSleep / 3600 : null,
      avgHeartRate: d.avgHeartRate,
    }));
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
  const sleepHours = latestData?.totalSleepHours ?? null;
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
      </div>

      {/* Primary Scores */}
      <div className={styles.statsGrid}>
        <OuraStatCard
          title="Sleep Score"
          value={sleepScore}
          details={lastUpdated ? formatRelativeTime(lastUpdated) : 'No data'}
          variant="score"
        />
        <OuraStatCard
          title="Readiness Score"
          value={readinessScore}
          details={lastUpdated ? formatRelativeTime(lastUpdated) : 'No data'}
          variant="score"
        />
        <OuraStatCard
          title="Activity Score"
          value={activityScore}
          details={lastUpdated ? formatRelativeTime(lastUpdated) : 'No data'}
          variant="score"
        />
        <OuraStatCard
          title="Sleep Duration"
          value={sleepHours !== null ? formatDuration(sleepHours * 60) : null}
          details={lastUpdated ? formatRelativeTime(lastUpdated) : 'No data'}
          variant="metric"
        />
      </div>

      {/* Advanced Health Metrics */}
      <CollapsibleSection title="Advanced Health Metrics" icon="🧬" defaultExpanded={false}>
        <div className={styles.statsGridSmall}>
          <OuraStatCard
            title="Daily Stress"
            icon="🧘"
            value={null}
            details="No data"
            variant="stress"
          />
          <OuraStatCard
            title="Resilience"
            icon="💪"
            value={null}
            details="No data"
            variant="resilience"
          />
          <OuraStatCard
            title="VO2 Max"
            icon="🫀"
            value={null}
            unit="ml/kg/min"
            details="No data"
            variant="vo2"
          />
          <OuraStatCard
            title="Cardio Age"
            icon="❤️"
            value={null}
            unit="years"
            details="No data"
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
            value={null}
            unit="%"
            details="No data"
            variant="spo2"
          />
          <OuraStatCard
            title="Optimal Bedtime"
            icon="🛏️"
            value={null}
            details="No data"
            variant="bedtime"
          />
          <OuraStatCard
            title="Workouts"
            icon="🏋️"
            value={null}
            details="No data"
            variant="workout"
          />
        </div>
      </CollapsibleSection>

      {/* Score Chart */}
      <div className={styles.chartSection}>
        <TimeRangeSelector
          value={timeRange}
          onChange={setTimeRange}
          variant="oura"
        />
        <OuraChart
          title="📊 Health Scores"
          data={scoreChartData}
          series={scoreSeries}
          onSeriesToggle={handleScoreSeriesToggle}
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
    </div>
  );
};

export default OuraPage;
