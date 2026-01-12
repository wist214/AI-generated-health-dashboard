import React from 'react';
import styles from './OuraStatCard.module.css';

interface StatisticsData {
  min: number | null;
  max: number | null;
  avg: number | null;
}

interface OuraStatCardProps {
  title: string;
  value: string | number | null;
  unit?: string;
  details?: string;
  icon?: string;
  onClick?: () => void;
  variant?: 'score' | 'metric' | 'stress' | 'resilience' | 'vo2' | 'cardio' | 'spo2' | 'bedtime' | 'workout';
  statistics?: StatisticsData;
}

/**
 * Oura-styled stat card component
 */
export const OuraStatCard: React.FC<OuraStatCardProps> = ({
  title,
  value,
  unit,
  details,
  icon,
  onClick,
  variant = 'score',
  statistics
}) => {
  const isClickable = !!onClick;

  const formatStatValue = (val: number | null) => {
    if (val === null || val === undefined) return '--';
    return val.toFixed(1);
  };
  
  return (
    <div 
      className={`${styles.card} ${styles[variant]} ${isClickable ? styles.clickable : ''}`}
      onClick={onClick}
      role={isClickable ? 'button' : undefined}
      tabIndex={isClickable ? 0 : undefined}
      onKeyDown={(e) => {
        if (isClickable && (e.key === 'Enter' || e.key === ' ')) {
          onClick?.();
        }
      }}
    >
      <h3 className={styles.title}>
        {icon && <span className={styles.icon}>{icon}</span>}
        {title}
      </h3>
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

export default OuraStatCard;
