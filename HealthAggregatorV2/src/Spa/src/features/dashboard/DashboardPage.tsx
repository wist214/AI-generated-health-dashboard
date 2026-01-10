import React from 'react';
import { MetricCard } from './components/MetricCard';
import { QuickStats } from './components/QuickStats';
import { useDashboardSummary } from './hooks/useDashboardData';
import { LoadingSpinner } from '@shared/components/LoadingSpinner';
import { ErrorMessage } from '@shared/components/ErrorMessage';
import { formatDate, formatNumber } from '@shared/utils';
import styles from './DashboardPage.module.css';
import type { QuickStat } from './types';

/**
 * Dashboard page component
 * Main overview page showing key health metrics
 */
export const DashboardPage: React.FC = () => {
  const { data, isLoading, error, refetch } = useDashboardSummary();

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
      value: data.steps ? formatNumber(data.steps) : '--',
    },
    {
      icon: '🛏️',
      label: 'Sleep Duration',
      value: '--', // Would need sleep duration from API
    },
    {
      icon: '❤️',
      label: 'Avg Heart Rate',
      value: '--', // Would need HR from API
    },
    {
      icon: '🔥',
      label: 'Calories Burned',
      value: data.caloriesBurned ? formatNumber(data.caloriesBurned) : '--',
    },
  ];

  return (
    <div className={styles.dashboard}>
      <div className={styles.dashboardHeader}>
        <h2>🎯 Your Health Dashboard</h2>
        <p className={styles.date}>{formatDate(data.date)}</p>
      </div>

      <div className={styles.dashboardGrid}>
        {/* Weight Card */}
        <MetricCard
          title="Weight"
          value={data.weight ? data.weight.toFixed(1) : null}
          unit="kg"
          icon="⚖️"
          source="Picooc"
          date={data.date ? formatDate(data.date) : 'No data'}
          variant="weight"
          progress={[
            { label: 'vs Previous', value: '--', type: 'neutral' },
            { label: 'vs ~1 Week', value: '--', type: 'neutral' },
            { label: 'vs ~1 Month', value: '--', type: 'neutral' },
          ]}
        />

        {/* Body Fat Card */}
        <MetricCard
          title="Body Fat"
          value={data.bodyFatPercentage ? data.bodyFatPercentage.toFixed(1) : null}
          unit="%"
          icon="💪"
          source="Picooc"
          date={data.date ? formatDate(data.date) : 'No data'}
          variant="bodyScore"
          progress={[
            { label: 'vs Previous', value: '--', type: 'neutral' },
            { label: 'vs ~1 Week', value: '--', type: 'neutral' },
            { label: 'vs ~1 Month', value: '--', type: 'neutral' },
          ]}
        />

        {/* Sleep Score Card */}
        <MetricCard
          title="Sleep Score"
          value={data.sleepScore}
          icon="😴"
          source="Oura Ring"
          date={data.date ? formatDate(data.date) : 'No data'}
          variant="sleep"
          progress={[
            { label: 'vs Yesterday', value: '--', type: 'neutral' },
            { label: 'vs 1 Week', value: '--', type: 'neutral' },
            { label: 'vs 1 Month', value: '--', type: 'neutral' },
          ]}
        />

        {/* Readiness Card */}
        <MetricCard
          title="Readiness"
          value={data.readinessScore}
          icon="⚡"
          source="Oura Ring"
          date={data.date ? formatDate(data.date) : 'No data'}
          variant="readiness"
          progress={[
            { label: 'vs Yesterday', value: '--', type: 'neutral' },
            { label: 'vs 1 Week', value: '--', type: 'neutral' },
            { label: 'vs 1 Month', value: '--', type: 'neutral' },
          ]}
        />
      </div>

      <QuickStats stats={quickStats} />
    </div>
  );
};

export default DashboardPage;
