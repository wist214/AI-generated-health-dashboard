import React from 'react';
import styles from './MetricCard.module.css';
import type { MetricCardVariant, ProgressItem } from '../types';

interface MetricCardProps {
  title: string;
  value: number | string | null;
  unit?: string;
  icon: string;
  source?: string;
  date?: string;
  variant?: MetricCardVariant;
  progress?: ProgressItem[];
  onClick?: () => void;
}

/**
 * Metric card component matching V1 dashboard design
 * Displays a health metric with progress tracking
 */
export const MetricCard: React.FC<MetricCardProps> = ({
  title,
  value,
  unit,
  icon,
  source,
  date,
  variant = 'weight',
  progress,
  onClick,
}) => {
  const cardClasses = [
    styles.metricCard,
    styles[variant],
    onClick ? styles.clickable : '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div
      className={cardClasses}
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onKeyDown={onClick ? (e) => e.key === 'Enter' && onClick() : undefined}
    >
      <div className={styles.metricHeader}>
        <div className={styles.metricTitle}>
          <span className={styles.icon}>{icon}</span>
          <h3>{title}</h3>
        </div>
        {source && <span className={styles.metricSource}>{source}</span>}
      </div>

      <div className={`${styles.metricValue} ${styles[variant]}`}>
        {value !== null && value !== undefined ? value : '--'}
        {unit && <span className={styles.metricUnit}> {unit}</span>}
      </div>

      {date && <div className={styles.metricDate}>{date}</div>}

      {progress && progress.length > 0 && (
        <div className={styles.progressSection}>
          <h4>Progress</h4>
          <div className={styles.progressGrid}>
            {progress.map((item, index) => (
              <div key={index} className={styles.progressItem}>
                <div className={styles.label}>{item.label}</div>
                <div className={`${styles.change} ${styles[item.type]}`}>
                  {item.value}
                </div>
                {item.absValue && (
                  <div className={styles.absValue}>{item.absValue}</div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
