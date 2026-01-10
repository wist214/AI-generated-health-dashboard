import React from 'react';
import styles from './QuickStats.module.css';
import type { QuickStat } from '../types';

interface QuickStatsProps {
  stats: QuickStat[];
}

/**
 * Quick stats row component matching V1 dashboard design
 */
export const QuickStats: React.FC<QuickStatsProps> = ({ stats }) => {
  return (
    <div className={styles.quickStats}>
      {stats.map((stat, index) => (
        <div key={index} className={styles.quickStat}>
          <div className={styles.icon}>{stat.icon}</div>
          <div className={styles.info}>
            <h4>{stat.label}</h4>
            <div className={styles.value}>{stat.value}</div>
          </div>
        </div>
      ))}
    </div>
  );
};
