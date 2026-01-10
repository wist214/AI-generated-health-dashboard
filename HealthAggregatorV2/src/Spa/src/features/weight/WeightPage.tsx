import React, { useState, useMemo } from 'react';
import { Button } from '@fluentui/react-components';
import { StatCard, TimeRangeSelector, WeightChart, WeightTable } from './components';
import { useLatestWeight, usePicoocSync } from './hooks';
import { LoadingSpinner } from '@shared/components/LoadingSpinner';
import { ErrorMessage } from '@shared/components/ErrorMessage';
import { getDateRangeFromTimeRange } from '@shared/utils';
import styles from './WeightPage.module.css';
import type { TimeRange, ChartSeries, WeightMetric } from './types';

// Default chart series configuration
const defaultSeries: ChartSeries[] = [
  { id: 'weight', label: 'Weight', color: '#00d4ff', enabled: true },
  { id: 'bodyFat', label: 'Body Fat', color: '#f472b6', enabled: true },
  { id: 'bmi', label: 'BMI', color: '#a78bfa', enabled: false },
  { id: 'muscleMass', label: 'Muscle', color: '#34d399', enabled: false },
];

/**
 * Weight tracking page
 * Displays Picooc smart scale data
 */
export const WeightPage: React.FC = () => {
  const [timeRange, setTimeRange] = useState<TimeRange>('all');
  const [series, setSeries] = useState<ChartSeries[]>(defaultSeries);
  const [tablePage, setTablePage] = useState(0);
  const pageSize = 10;

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const { data: _latestData, isLoading, error, refetch } = useLatestWeight();
  const { mutate: syncData, isPending: isSyncing } = usePicoocSync();

  // Mock data for demonstration (would be replaced with real API data)
  const mockData: WeightMetric[] = useMemo(() => {
    // Return empty array - will show "no data" state
    // Real implementation would use _latestData from API
    return [];
  }, []);

  // Filter data by time range
  const filteredData = useMemo(() => {
    if (mockData.length === 0) return [];
    const { startDate, endDate } = getDateRangeFromTimeRange(timeRange);
    return mockData.filter(d => {
      const date = new Date(d.date);
      return date >= new Date(startDate) && date <= new Date(endDate);
    });
  }, [mockData, timeRange]);

  const handleSeriesToggle = (id: string) => {
    setSeries(prev => prev.map(s => 
      s.id === id ? { ...s, enabled: !s.enabled } : s
    ));
  };

  const handleSync = () => {
    syncData();
  };

  // Extract latest values (would extract from API data)
  const latestWeight = null as number | null;
  const latestBodyFat = null as number | null;
  const latestBMI = null as number | null;
  const latestMuscle = null as number | null;

  const totalPages = Math.ceil(filteredData.length / pageSize);

  if (isLoading) {
    return <LoadingSpinner label="Loading weight data..." />;
  }

  if (error) {
    return (
      <ErrorMessage
        title="Failed to load weight data"
        message={error.message}
        onRetry={() => refetch()}
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
      </div>

      {/* Stats Grid */}
      <div className={styles.statsGrid}>
        <StatCard
          title="Weight"
          value={latestWeight !== null ? latestWeight.toFixed(1) : null}
          unit="kg"
          details={latestWeight ? 'Latest measurement' : 'No data'}
          variant="weight"
        />
        <StatCard
          title="Body Fat"
          value={latestBodyFat !== null ? latestBodyFat.toFixed(1) : null}
          unit="%"
          details={latestBodyFat ? 'Latest measurement' : 'No data'}
          variant="weight"
        />
        <StatCard
          title="BMI"
          value={latestBMI !== null ? latestBMI.toFixed(1) : null}
          details={latestBMI ? 'Latest measurement' : 'No data'}
          variant="weight"
        />
        <StatCard
          title="Muscle Mass"
          value={latestMuscle !== null ? latestMuscle.toFixed(1) : null}
          unit="kg"
          details={latestMuscle ? 'Latest measurement' : 'No data'}
          variant="weight"
        />
      </div>

      {/* Chart Section */}
      <div className={styles.chartSection}>
        <TimeRangeSelector
          value={timeRange}
          onChange={setTimeRange}
        />
        <WeightChart
          data={filteredData}
          series={series}
          onSeriesToggle={handleSeriesToggle}
        />
      </div>

      {/* Data Table */}
      <WeightTable
        data={filteredData}
        page={tablePage}
        pageSize={pageSize}
        totalPages={totalPages}
        onPageChange={setTablePage}
      />
    </div>
  );
};

export default WeightPage;
