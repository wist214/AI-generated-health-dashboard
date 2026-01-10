import React from 'react';
import styles from './OuraTable.module.css';
import { formatDate, formatDurationSeconds } from '@shared/utils';
import type { OuraSleepData, OuraActivityData } from '../types';

interface OuraSleepTableProps {
  data: OuraSleepData[];
}

/**
 * Sleep history table component
 */
export const SleepTable: React.FC<OuraSleepTableProps> = ({ data }) => {
  return (
    <div className={styles.tableContainer}>
      <h2 className={styles.title}>😴 Sleep History</h2>
      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Date</th>
              <th>Score</th>
              <th>Total Sleep</th>
              <th>Deep Sleep</th>
              <th>REM Sleep</th>
              <th>Avg HR</th>
              <th>HRV</th>
              <th>Efficiency</th>
            </tr>
          </thead>
          <tbody>
            {data.length > 0 ? (
              data.map((row, index) => (
                <tr key={`${row.date}-${index}`}>
                  <td>{formatDate(row.date)}</td>
                  <td className={styles.score}>{row.score ?? '--'}</td>
                  <td>{row.totalSleep ? formatDurationSeconds(row.totalSleep) : '--'}</td>
                  <td>{row.deepSleep ? formatDurationSeconds(row.deepSleep) : '--'}</td>
                  <td>{row.remSleep ? formatDurationSeconds(row.remSleep) : '--'}</td>
                  <td>{row.avgHeartRate ?? '--'}</td>
                  <td>{row.avgHrv ?? '--'}</td>
                  <td>{row.sleepEfficiency ? `${row.sleepEfficiency}%` : '--'}</td>
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={8} className={styles.emptyState}>
                  <h3>No data yet</h3>
                  <p>Click "Sync Oura Data" to fetch your sleep data</p>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

interface OuraActivityTableProps {
  data: OuraActivityData[];
}

/**
 * Activity history table component
 */
export const ActivityTable: React.FC<OuraActivityTableProps> = ({ data }) => {
  const formatDistance = (meters: number | null) => {
    if (meters === null) return '--';
    return `${(meters / 1000).toFixed(1)} km`;
  };

  return (
    <div className={styles.tableContainer}>
      <h2 className={styles.title}>🏃 Activity History</h2>
      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Date</th>
              <th>Score</th>
              <th>Steps</th>
              <th>Active Cal</th>
              <th>Total Cal</th>
              <th>Distance</th>
              <th>High Activity</th>
              <th>Med Activity</th>
            </tr>
          </thead>
          <tbody>
            {data.length > 0 ? (
              data.map((row, index) => (
                <tr key={`${row.date}-${index}`}>
                  <td>{formatDate(row.date)}</td>
                  <td className={styles.score}>{row.score ?? '--'}</td>
                  <td>{row.steps?.toLocaleString() ?? '--'}</td>
                  <td>{row.activeCalories ?? '--'}</td>
                  <td>{row.totalCalories ?? '--'}</td>
                  <td>{formatDistance(row.distance)}</td>
                  <td>{row.highActivity ? formatDurationSeconds(row.highActivity) : '--'}</td>
                  <td>{row.mediumActivity ? formatDurationSeconds(row.mediumActivity) : '--'}</td>
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={8} className={styles.emptyState}>
                  <h3>No data yet</h3>
                  <p>Click "Sync Oura Data" to fetch your activity data</p>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default SleepTable;
