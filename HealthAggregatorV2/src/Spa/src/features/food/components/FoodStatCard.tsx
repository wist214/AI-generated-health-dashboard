import React from 'react';
import styles from './FoodStatCard.module.css';

interface FoodStatCardProps {
  title: string;
  value: string | number | null;
  unit?: string;
  details?: string;
  targetValue?: number;
}

/**
 * Food-themed stat card component
 */
export const FoodStatCard: React.FC<FoodStatCardProps> = ({
  title,
  value,
  unit,
  details,
  targetValue
}) => {
  const percentage = value && targetValue 
    ? Math.min(100, (Number(value) / targetValue) * 100) 
    : 0;

  return (
    <div className={styles.card}>
      <h3 className={styles.title}>{title}</h3>
      <div className={styles.value}>
        {value ?? '--'}
        {unit && <span className={styles.unit}>{unit}</span>}
      </div>
      {targetValue && (
        <div className={styles.progressContainer}>
          <div 
            className={styles.progress} 
            style={{ width: `${percentage}%` }}
          />
        </div>
      )}
      {details && <div className={styles.details}>{details}</div>}
    </div>
  );
};

export default FoodStatCard;
