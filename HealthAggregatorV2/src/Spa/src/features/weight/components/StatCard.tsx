import React from 'react';
import styles from './StatCard.module.css';

interface StatCardProps {
  title: string;
  value: string | number | null;
  unit?: string;
  details?: string;
  variant?: 'default' | 'weight' | 'oura' | 'food';
}

/**
 * Stat card component for displaying a single metric
 */
export const StatCard: React.FC<StatCardProps> = ({
  title,
  value,
  unit,
  details,
  variant = 'default'
}) => {
  return (
    <div className={`${styles.statCard} ${styles[variant]}`}>
      <h3 className={styles.title}>{title}</h3>
      <div className={styles.value}>
        {value ?? '--'}
        {unit && <span className={styles.unit}>{unit}</span>}
      </div>
      {details && <div className={styles.details}>{details}</div>}
    </div>
  );
};

export default StatCard;
