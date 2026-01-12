import React from 'react';
import styles from './StatCard.module.css';

interface StatisticsData {
  min: number | null;
  max: number | null;
  avg: number | null;
}

interface StatCardProps {
  title: string;
  value: string | number | null;
  unit?: string;
  details?: string;
  variant?: 'default' | 'weight' | 'oura' | 'food';
  statistics?: StatisticsData;
}

/**
 * Stat card component for displaying a single metric with optional min/max/avg statistics
 */
export const StatCard: React.FC<StatCardProps> = ({
  title,
  value,
  unit,
  details,
  variant = 'default',
  statistics
}) => {
  const formatStatValue = (val: number | null) => {
    if (val === null || val === undefined) return '--';
    return val.toFixed(1);
  };

  return (
    <div className={`${styles.statCard} ${styles[variant]}`}>
      <h3 className={styles.title}>{title}</h3>
      <div className={styles.value}>
        {value ?? '--'}
        {unit && <span className={styles.unit}>{unit}</span>}
      </div>
      {details && <div className={styles.details}>{details}</div>}
      {statistics && (
        <div className={styles.statistics}>
          <div className={styles.statItem}>
            <span className={styles.statLabel}>Min</span>
            <span className={styles.statValue}>{formatStatValue(statistics.min)}</span>
          </div>
          <div className={styles.statItem}>
            <span className={styles.statLabel}>Max</span>
            <span className={styles.statValue}>{formatStatValue(statistics.max)}</span>
          </div>
          <div className={styles.statItem}>
            <span className={styles.statLabel}>Avg</span>
            <span className={styles.statValue}>{formatStatValue(statistics.avg)}</span>
          </div>
        </div>
      )}
    </div>
  );
};

export default StatCard;
