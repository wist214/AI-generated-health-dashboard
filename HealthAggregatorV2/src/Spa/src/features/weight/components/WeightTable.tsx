import React from 'react';
import styles from './WeightTable.module.css';
import { formatDate } from '@shared/utils';
import type { WeightMetric } from '../types';

interface WeightTableProps {
  data: WeightMetric[];
  page: number;
  pageSize: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}

/**
 * Weight history table component
 */
export const WeightTable: React.FC<WeightTableProps> = ({
  data,
  page,
  pageSize,
  totalPages,
  onPageChange
}) => {
  const startIndex = page * pageSize;
  const pageData = data.slice(startIndex, startIndex + pageSize);

  return (
    <div className={styles.tableContainer}>
      <h2 className={styles.title}>📋 Measurement History</h2>
      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Date</th>
              <th>Weight (kg)</th>
              <th>Body Fat (%)</th>
              <th>BMI</th>
              <th>Muscle (kg)</th>
              <th>Water (%)</th>
              <th>Metabolic Age</th>
            </tr>
          </thead>
          <tbody>
            {pageData.length > 0 ? (
              pageData.map((row, index) => (
                <tr key={`${row.date}-${index}`}>
                  <td>{formatDate(row.date)}</td>
                  <td>{row.weight?.toFixed(1) ?? '--'}</td>
                  <td>{row.bodyFat?.toFixed(1) ?? '--'}</td>
                  <td>{row.bmi?.toFixed(1) ?? '--'}</td>
                  <td>{row.muscleMass?.toFixed(1) ?? '--'}</td>
                  <td>{row.bodyWater?.toFixed(1) ?? '--'}</td>
                  <td>{row.metabolicAge ?? '--'}</td>
                </tr>
              ))
            ) : (
              <tr>
                <td colSpan={7} className={styles.emptyState}>
                  <h3>No data yet</h3>
                  <p>Click "Sync Data" to fetch your measurements</p>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
      
      {totalPages > 1 && (
        <div className={styles.pagination}>
          <button
            type="button"
            className={styles.pageBtn}
            onClick={() => onPageChange(page - 1)}
            disabled={page === 0}
          >
            ← Previous
          </button>
          <span className={styles.pageInfo}>
            Page {page + 1} of {totalPages}
          </span>
          <button
            type="button"
            className={styles.pageBtn}
            onClick={() => onPageChange(page + 1)}
            disabled={page >= totalPages - 1}
          >
            Next →
          </button>
        </div>
      )}
    </div>
  );
};

export default WeightTable;
