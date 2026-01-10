import React from 'react';
import styles from './TimeRangeSelector.module.css';
import type { TimeRange } from '../types';

interface TimeRangeSelectorProps {
  value: TimeRange;
  onChange: (range: TimeRange) => void;
  variant?: 'default' | 'oura' | 'food';
}

const timeRanges: { value: TimeRange; label: string }[] = [
  { value: '7d', label: '7 Days' },
  { value: '30d', label: '30 Days' },
  { value: '90d', label: '3 Months' },
  { value: '6m', label: '6 Months' },
  { value: '1y', label: '1 Year' },
  { value: 'all', label: 'All Time' },
];

/**
 * Time range selector for charts
 */
export const TimeRangeSelector: React.FC<TimeRangeSelectorProps> = ({
  value,
  onChange,
  variant = 'default'
}) => {
  return (
    <div className={styles.selector}>
      {timeRanges.map((range) => (
        <button
          key={range.value}
          type="button"
          className={`${styles.button} ${styles[variant]} ${value === range.value ? styles.active : ''}`}
          onClick={() => onChange(range.value)}
        >
          {range.label}
        </button>
      ))}
    </div>
  );
};

export default TimeRangeSelector;
