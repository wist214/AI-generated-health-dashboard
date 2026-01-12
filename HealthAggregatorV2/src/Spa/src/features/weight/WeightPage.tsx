import React, { useState, useMemo } from 'react';
import { Button } from '@fluentui/react-components';
import { StatCard, WeightChart, WeightTable } from './components';
import { useLatestWeight, useWeightHistory, usePicoocSync } from './hooks';
import { LoadingSpinner } from '@shared/components/LoadingSpinner';
import { ErrorMessage } from '@shared/components/ErrorMessage';
import { getDateRangeFromTimeRange, formatRelativeTime } from '@shared/utils';
import styles from './WeightPage.module.css';
import type { TimeRange, ChartSeries } from './types';

// Default chart series configuration
const defaultSeries: ChartSeries[] = [
  { id: 'weight', label: 'Weight', color: '#00d4ff', enabled: true },
  { id: 'bodyFat', label: 'Body Fat', color: '#f472b6', enabled: true },
  { id: 'bmi', label: 'BMI', color: '#a78bfa', enabled: false },
  { id: 'muscleMass', label: 'Muscle', color: '#34d399', enabled: false },
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
 * Weight tracking page
 * Displays Picooc smart scale data
 */
export const WeightPage: React.FC = () => {
  const [timeRange, setTimeRange] = useState<TimeRange>('all');
  const [series, setSeries] = useState<ChartSeries[]>(defaultSeries);
  const [tablePage, setTablePage] = useState(0);
  const pageSize = 10;

  // Get date range for history query
  const { startDate, endDate } = useMemo(() => 
    getDateRangeFromTimeRange(timeRange),
    [timeRange]
  );

  // Fetch data from API
  const { data: latestData, isLoading: isLoadingLatest, error: latestError, refetch: refetchLatest } = useLatestWeight();
  const { data: historyData, isLoading: isLoadingHistory, error: historyError } = useWeightHistory(startDate, endDate);
  const { mutate: syncData, isPending: isSyncing } = usePicoocSync();

  const isLoading = isLoadingLatest || isLoadingHistory;
  const error = latestError || historyError;

  // Use history data for charts and table, filtered by time range
  const weightData = useMemo(() => {
    const allData = historyData ?? [];
    if (timeRange === 'all') return allData;
    return allData.filter(d => d.date >= startDate && d.date <= endDate);
  }, [historyData, timeRange, startDate, endDate]);

  // Calculate statistics for each metric
  const weightStats = useMemo(() => calculateStats(weightData.map(d => d.weight)), [weightData]);
  const bodyFatStats = useMemo(() => calculateStats(weightData.map(d => d.bodyFat)), [weightData]);
  const bmiStats = useMemo(() => calculateStats(weightData.map(d => d.bmi)), [weightData]);
  const muscleStats = useMemo(() => calculateStats(weightData.map(d => d.muscleMass)), [weightData]);

  const handleSeriesToggle = (id: string) => {
    setSeries(prev => prev.map(s => 
      s.id === id ? { ...s, enabled: !s.enabled } : s
    ));
  };

  const handleSync = () => {
    syncData();
  };

  // Extract latest values from history data (first item is most recent)
  const latestFromHistory = weightData[0] ?? null;
  const latestWeight = latestData?.weight ?? null;
  const latestBodyFat = latestData?.bodyFat ?? null;
  const latestBMI = latestData?.bmi ?? null;
  const latestMuscle = latestFromHistory?.muscleMass ?? null;
  const lastUpdated = latestData?.lastUpdated;

  const totalPages = Math.ceil(weightData.length / pageSize);

  if (isLoading) {
    return <LoadingSpinner label="Loading weight data..." />;
  }

  if (error) {
    return (
      <ErrorMessage
        title="Failed to load weight data"
        message={(error as Error).message}
        onRetry={() => refetchLatest()}
      />
    );
  }

  return (
    <div className={styles.weightPage}>
      {/* Controls */}
      <div className={styles.controls}>
        <Button
          appearance="primary"
          onClick={handleSync}
          disabled={isSyncing}
          className={styles.syncBtn}
        >
          {isSyncing ? '🔄 Syncing...' : '🔄 Sync Data'}
        </Button>
        {lastUpdated && (
          <span className={styles.lastUpdated}>
            Last updated: {formatRelativeTime(lastUpdated)}
          </span>
        )}
      </div>

      {/* Stats Grid */}
      <div className={styles.statsGrid}>
        <StatCard
          title="Weight"
          value={latestWeight !== null ? latestWeight.toFixed(1) : null}
          unit="kg"
          variant="weight"
          statistics={weightStats}
        />
        <StatCard
          title="Body Fat"
          value={latestBodyFat !== null ? latestBodyFat.toFixed(1) : null}
          unit="%"
          variant="weight"
          statistics={bodyFatStats}
        />
        <StatCard
          title="BMI"
          value={latestBMI !== null ? latestBMI.toFixed(1) : null}
          variant="weight"
          statistics={bmiStats}
        />
        <StatCard
          title="Muscle Mass"
          value={latestMuscle !== null ? latestMuscle.toFixed(1) : null}
          unit="kg"
          variant="weight"
          statistics={muscleStats}
        />
      </div>

      {/* Chart Section */}
      <div className={styles.chartSection}>
        <WeightChart
          data={weightData}
          series={series}
          onSeriesToggle={handleSeriesToggle}
          timeRange={timeRange}
          onTimeRangeChange={setTimeRange}
        />
      </div>

      {/* Data Table */}
      <WeightTable
        data={weightData}
        page={tablePage}
        pageSize={pageSize}
        totalPages={totalPages}
        onPageChange={setTablePage}
      />
    </div>
  );
};

export default WeightPage;
