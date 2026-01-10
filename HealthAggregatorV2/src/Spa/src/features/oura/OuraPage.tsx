import React, { useState } from 'react';
import { Button } from '@fluentui/react-components';
import { 
  OuraStatCard, 
  CollapsibleSection, 
  OuraChart, 
  SleepTable, 
  ActivityTable 
} from './components';
import { TimeRangeSelector } from '../weight/components';
import { useLatestOura, useOuraSync } from './hooks';
import { LoadingSpinner } from '@shared/components/LoadingSpinner';
import { ErrorMessage } from '@shared/components/ErrorMessage';
import { formatDurationSeconds } from '@shared/utils';
import styles from './OuraPage.module.css';
import type { TimeRange, ChartSeries, OuraSleepData, OuraActivityData } from './types';

// Default score chart series
const defaultScoreSeries: ChartSeries[] = [
  { id: 'sleepScore', label: 'Sleep', color: '#3b82f6', enabled: true },
  { id: 'readinessScore', label: 'Readiness', color: '#22c55e', enabled: true },
  { id: 'activityScore', label: 'Activity', color: '#f59e0b', enabled: true },
  { id: 'steps', label: 'Steps', color: '#8b5cf6', enabled: false },
];

// Default sleep chart series
const defaultSleepSeries: ChartSeries[] = [
  { id: 'totalSleep', label: 'Total Sleep', color: '#3b82f6', enabled: true },
  { id: 'deepSleep', label: 'Deep Sleep', color: '#1e40af', enabled: true },
  { id: 'remSleep', label: 'REM Sleep', color: '#8b5cf6', enabled: true },
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

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const { data: _latestData, isLoading, error, refetch } = useLatestOura();
  const { mutate: syncData, isPending: isSyncing } = useOuraSync();

  // Mock data - would be replaced with real API data
  const mockSleepData: OuraSleepData[] = [];
  const mockActivityData: OuraActivityData[] = [];
  const mockScoreData: Array<{ date: string; sleepScore: number | null; readinessScore: number | null; activityScore: number | null; steps: number | null }> = [];
  const mockSleepChartData: Array<{ date: string; totalSleep: number | null; deepSleep: number | null; remSleep: number | null; avgHeartRate: number | null }> = [];

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

  // Latest values (would extract from latestData)
  const sleepScore = null;
  const readinessScore = null;
  const activityScore = null;
  const sleepDuration = null;

  if (isLoading) {
    return <LoadingSpinner label="Loading Oura data..." />;
  }

  if (error) {
    return (
      <ErrorMessage
        title="Failed to load Oura data"
        message={error.message}
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
          details={sleepScore ? 'Today' : 'No data'}
          variant="score"
        />
        <OuraStatCard
          title="Readiness Score"
          value={readinessScore}
          details={readinessScore ? 'Today' : 'No data'}
          variant="score"
        />
        <OuraStatCard
          title="Activity Score"
          value={activityScore}
          details={activityScore ? 'Today' : 'No data'}
          variant="score"
        />
        <OuraStatCard
          title="Sleep Duration"
          value={sleepDuration ? formatDurationSeconds(sleepDuration) : null}
          details={sleepDuration ? 'Last night' : 'No data'}
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
          data={mockScoreData}
          series={scoreSeries}
          onSeriesToggle={handleScoreSeriesToggle}
        />
      </div>

      {/* Sleep Duration Chart */}
      <OuraChart
        title="😴 Sleep Duration"
        data={mockSleepChartData}
        series={sleepSeries}
        onSeriesToggle={handleSleepSeriesToggle}
        yAxisLabel="Hours"
      />

      {/* Sleep History Table */}
      <SleepTable data={mockSleepData} />

      {/* Activity History Table */}
      <ActivityTable data={mockActivityData} />
    </div>
  );
};

export default OuraPage;
