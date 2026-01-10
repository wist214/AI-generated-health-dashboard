import React from 'react';
import styles from './NutrientProgressBar.module.css';
import type { NutrientProgress } from '../types';

interface NutrientProgressBarProps {
  nutrient: NutrientProgress;
}

/**
 * Nutrient progress bar component
 */
export const NutrientProgressBar: React.FC<NutrientProgressBarProps> = ({ nutrient }) => {
  const percentage = nutrient.value 
    ? Math.min(100, (nutrient.value / nutrient.goal) * 100) 
    : 0;

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.name}>{nutrient.name}</span>
        <span className={styles.value}>
          {nutrient.value?.toFixed(0) ?? '--'} / {nutrient.goal} {nutrient.unit}
        </span>
      </div>
      <div className={styles.progressBar}>
        <div 
          className={`${styles.fill} ${styles[nutrient.variant]}`} 
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
};

interface NutrientsGridProps {
  nutrients: NutrientProgress[];
}

/**
 * Grid of nutrient progress bars
 */
export const NutrientsGrid: React.FC<NutrientsGridProps> = ({ nutrients }) => {
  return (
    <div className={styles.grid}>
      {nutrients.map((nutrient) => (
        <NutrientProgressBar key={nutrient.name} nutrient={nutrient} />
      ))}
    </div>
  );
};

export default NutrientProgressBar;
