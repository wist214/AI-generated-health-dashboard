import React, { useState } from 'react';
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
 * Weight history table component with detail modal
 */
export const WeightTable: React.FC<WeightTableProps> = ({
  data,
  page,
  pageSize,
  totalPages,
  onPageChange
}) => {
  const [selectedItem, setSelectedItem] = useState<WeightMetric | null>(null);
  
  const startIndex = page * pageSize;
  const pageData = data.slice(startIndex, startIndex + pageSize);

  const handleRowClick = (item: WeightMetric) => {
    setSelectedItem(item);
  };

  const closeModal = () => {
    setSelectedItem(null);
  };

  const formatValue = (val: number | null | undefined, decimals = 1) => {
    if (val === null || val === undefined) return '--';
    return val.toFixed(decimals);
  };

  // Get body fat status
  const getBodyFatStatus = (bodyFat: number | null | undefined): { emoji: string; label: string; color: string } => {
    if (bodyFat === null || bodyFat === undefined) return { emoji: '', label: '', color: '' };
    if (bodyFat < 14) return { emoji: '💪', label: 'Athletic', color: 'var(--color-info)' };
    if (bodyFat < 18) return { emoji: '✅', label: 'Fit', color: 'var(--color-success)' };
    if (bodyFat < 25) return { emoji: '👍', label: 'Average', color: 'var(--color-warning)' };
    return { emoji: '⚠️', label: 'Above Average', color: 'var(--color-error)' };
  };

  // Get BMI status
  const getBmiStatus = (bmi: number | null | undefined): { emoji: string; label: string; color: string } => {
    if (bmi === null || bmi === undefined) return { emoji: '', label: '', color: '' };
    if (bmi < 18.5) return { emoji: '⚠️', label: 'Underweight', color: 'var(--color-warning)' };
    if (bmi < 25) return { emoji: '✅', label: 'Normal', color: 'var(--color-success)' };
    if (bmi < 30) return { emoji: '⚠️', label: 'Overweight', color: 'var(--color-warning)' };
    return { emoji: '❌', label: 'Obese', color: 'var(--color-error)' };
  };

  // Get visceral fat status
  const getVisceralFatStatus = (vf: number | null | undefined): { emoji: string; label: string; color: string } => {
    if (vf === null || vf === undefined) return { emoji: '', label: '', color: '' };
    if (vf <= 9) return { emoji: '✅', label: 'Healthy', color: 'var(--color-success)' };
    if (vf <= 14) return { emoji: '⚠️', label: 'High', color: 'var(--color-warning)' };
    return { emoji: '❌', label: 'Very High', color: 'var(--color-error)' };
  };

  // Format date with full details
  const formatFullDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', { 
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  };

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
                <tr key={`${row.date}-${index}`} onClick={() => handleRowClick(row)}>
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

      {/* Detail Modal */}
      {selectedItem && (
        <div className={styles.modalOverlay} onClick={closeModal}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <h3>Body Composition</h3>
              <span className={styles.modalDate}>{formatFullDate(selectedItem.date)}</span>
              <button className={styles.closeButton} onClick={closeModal}>×</button>
            </div>
            <div className={styles.modalBody}>
              {/* Hero Metrics */}
              <div className={styles.mainMetrics}>
                <div className={styles.mainMetricItem}>
                  <div className={styles.metricIcon}>⚖️</div>
                  <div className={styles.metricValue}>{formatValue(selectedItem.weight)}</div>
                  <div className={styles.metricLabel}>Weight (kg)</div>
                </div>
                <div className={styles.mainMetricItem}>
                  <div className={styles.metricIcon}>🔥</div>
                  <div className={styles.metricValue}>{formatValue(selectedItem.bodyFat)}</div>
                  <div className={styles.metricLabel}>Body Fat</div>
                  {selectedItem.bodyFat && (
                    <div className={styles.statusBadge}>
                      {getBodyFatStatus(selectedItem.bodyFat).emoji} {getBodyFatStatus(selectedItem.bodyFat).label}
                    </div>
                  )}
                </div>
                <div className={styles.mainMetricItem}>
                  <div className={styles.metricIcon}>📊</div>
                  <div className={styles.metricValue}>{formatValue(selectedItem.bmi)}</div>
                  <div className={styles.metricLabel}>BMI</div>
                  {selectedItem.bmi && (
                    <div className={styles.statusBadge}>
                      {getBmiStatus(selectedItem.bmi).emoji} {getBmiStatus(selectedItem.bmi).label}
                    </div>
                  )}
                </div>
              </div>

              {/* Body Composition Section */}
              <div className={styles.section}>
                <h4 className={styles.sectionTitle}>Body Composition</h4>
                <div className={styles.progressList}>
                  <div className={styles.progressItem}>
                    <div className={styles.progressHeader}>
                      <span className={styles.progressLabel}>💪 Muscle Mass</span>
                      <span className={styles.progressValue}>{formatValue(selectedItem.muscleMass)}</span>
                      <span className={styles.progressUnit}>kg</span>
                    </div>
                    <div className={styles.progressBar}>
                      <div 
                        className={`${styles.progressFill} ${styles.muscle}`}
                        style={{ width: `${Math.min((selectedItem.muscleMass || 0) / 60 * 100, 100)}%` }}
                      />
                    </div>
                  </div>
                  <div className={styles.progressItem}>
                    <div className={styles.progressHeader}>
                      <span className={styles.progressLabel}>🔥 Body Fat</span>
                      <span className={styles.progressValue}>{formatValue(selectedItem.bodyFat)}</span>
                      <span className={styles.progressUnit}>%</span>
                    </div>
                    <div className={styles.progressBar}>
                      <div 
                        className={`${styles.progressFill} ${styles.fat}`}
                        style={{ width: `${Math.min((selectedItem.bodyFat || 0) / 40 * 100, 100)}%` }}
                      />
                    </div>
                  </div>
                  <div className={styles.progressItem}>
                    <div className={styles.progressHeader}>
                      <span className={styles.progressLabel}>💧 Body Water</span>
                      <span className={styles.progressValue}>{formatValue(selectedItem.bodyWater)}</span>
                      <span className={styles.progressUnit}>%</span>
                    </div>
                    <div className={styles.progressBar}>
                      <div 
                        className={`${styles.progressFill} ${styles.water}`}
                        style={{ width: `${Math.min((selectedItem.bodyWater || 0) / 70 * 100, 100)}%` }}
                      />
                    </div>
                  </div>
                  <div className={styles.progressItem}>
                    <div className={styles.progressHeader}>
                      <span className={styles.progressLabel}>🦴 Bone Mass</span>
                      <span className={styles.progressValue}>{formatValue(selectedItem.boneMass)}</span>
                      <span className={styles.progressUnit}>kg</span>
                    </div>
                    <div className={styles.progressBar}>
                      <div 
                        className={`${styles.progressFill} ${styles.bone}`}
                        style={{ width: `${Math.min((selectedItem.boneMass || 0) / 6 * 100, 100)}%` }}
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* Health Indicators Section */}
              <div className={styles.section}>
                <h4 className={styles.sectionTitle}>Health Indicators</h4>
                <div className={styles.indicatorGrid}>
                  <div className={styles.indicatorItem}>
                    <div className={styles.indicatorIcon}>🎂</div>
                    <div className={styles.indicatorValue}>{formatValue(selectedItem.metabolicAge, 1)}</div>
                    <div className={styles.indicatorLabel}>Metabolic Age</div>
                    <div className={styles.indicatorUnit}>years</div>
                  </div>
                  <div className={styles.indicatorItem}>
                    <div className={styles.indicatorIcon}>🎯</div>
                    <div className={styles.indicatorValue}>{formatValue(selectedItem.visceralFat)}</div>
                    <div className={styles.indicatorLabel}>Visceral Fat</div>
                    {selectedItem.visceralFat && (
                      <div className={styles.indicatorStatus} style={{ color: getVisceralFatStatus(selectedItem.visceralFat).color }}>
                        {getVisceralFatStatus(selectedItem.visceralFat).emoji} {getVisceralFatStatus(selectedItem.visceralFat).label}
                      </div>
                    )}
                  </div>
                  <div className={styles.indicatorItem}>
                    <div className={styles.indicatorIcon}>⚡</div>
                    <div className={styles.indicatorValue}>{formatValue(selectedItem.bmr, 1)}</div>
                    <div className={styles.indicatorLabel}>BMR</div>
                    <div className={styles.indicatorUnit}>kcal/day</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default WeightTable;
