import React, { useState } from 'react';
import styles from './OuraTable.module.css';
import { formatDate, formatDurationSeconds } from '@shared/utils';
import type { OuraSleepData, OuraActivityData } from '../types';

/**
 * Get CSS class for score value based on Oura scoring guidelines
 * 85+ = Optimal (green), 70-84 = Good (yellow/orange), <70 = Pay attention (red)
 */
const getScoreColorClass = (score: number | null): string => {
  if (score === null) return '';
  if (score >= 85) return styles.scoreOptimal ?? '';
  if (score >= 70) return styles.scoreGood ?? '';
  return styles.scoreAttention ?? '';
};

interface OuraSleepTableProps {
  data: OuraSleepData[];
}

/** Format date for modal header */
const formatFullDate = (dateStr: string): string => {
  const date = new Date(dateStr);
  return date.toLocaleDateString('en-US', {
    weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
  });
};

/** Format ISO datetime to time string (e.g., "01:32 AM") */
const formatTime = (isoStr: string | null): string => {
  if (!isoStr) return '--';
  const date = new Date(isoStr);
  return date.toLocaleTimeString('en-US', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: true
  });
};

/** Calculate sleep stage percentages for the stage bar */
const calculateStagePercentages = (item: OuraSleepData) => {
  const total = (item.deepSleep || 0) + (item.lightSleep || 0) + 
                (item.remSleep || 0) + (item.awakeTime || 0);
  if (total === 0) return { deep: 0, light: 0, rem: 0, awake: 0 };
  return {
    deep: Math.round(((item.deepSleep || 0) / total) * 100),
    light: Math.round(((item.lightSleep || 0) / total) * 100),
    rem: Math.round(((item.remSleep || 0) / total) * 100),
    awake: Math.round(((item.awakeTime || 0) / total) * 100)
  };
};

/**
 * Sleep history table component with detail modal
 */
export const SleepTable: React.FC<OuraSleepTableProps> = ({ data }) => {
  const [selectedItem, setSelectedItem] = useState<OuraSleepData | null>(null);

  const handleRowClick = (item: OuraSleepData) => {
    setSelectedItem(item);
  };

  const closeModal = () => {
    setSelectedItem(null);
  };

  const stagePercentages = selectedItem ? calculateStagePercentages(selectedItem) : null;

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
                <tr key={`${row.date}-${index}`} onClick={() => handleRowClick(row)}>
                  <td>{formatDate(row.date)}</td>
                  <td>
                    <span className={`${styles.scoreBadge} ${getScoreColorClass(row.score)}`}>
                      {row.score ?? '--'}
                    </span>
                  </td>
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

      {/* Sleep Detail Modal - V1 Style */}
      {selectedItem && (
        <div className={styles.modalOverlay} onClick={closeModal}>
          <div className={styles.sleepModal} onClick={(e) => e.stopPropagation()}>
            <div className={styles.sleepModalHeader}>
              <div className={styles.headerContent}>
                <h2>Sleep Analysis</h2>
                <span className={styles.modalDate}>{formatFullDate(selectedItem.date)}</span>
              </div>
              <button className={styles.closeButton} onClick={closeModal}>×</button>
            </div>
            <div className={styles.modalBody}>
              {/* Hero Metrics Row */}
              <div className={styles.metricHero}>
                <div className={styles.heroMetric}>
                  <div className={styles.heroIcon}>😴</div>
                  <div className={styles.heroValue}>{selectedItem.score ?? '--'}</div>
                  <div className={styles.heroLabel}>Sleep Score</div>
                </div>
                <div className={styles.heroMetric}>
                  <div className={styles.heroIcon}>⏰</div>
                  <div className={styles.heroValue}>
                    {selectedItem.totalSleep ? formatDurationSeconds(selectedItem.totalSleep) : '--'}
                  </div>
                  <div className={styles.heroLabel}>Total Sleep</div>
                </div>
                <div className={styles.heroMetric}>
                  <div className={styles.heroIcon}>✨</div>
                  <div className={styles.heroValue}>
                    {selectedItem.sleepEfficiency ? `${selectedItem.sleepEfficiency}%` : '--'}
                  </div>
                  <div className={styles.heroLabel}>Efficiency</div>
                </div>
              </div>

              {/* Sleep Timing Section */}
              <div className={styles.sleepTimingSection}>
                <div className={styles.timingRow}>
                  <div className={styles.timingItem}>
                    <span className={styles.timingLabel}>Bedtime</span>
                    <span className={styles.timingValue}>{formatTime(selectedItem.bedtimeStart)}</span>
                  </div>
                  <div className={styles.timingArrow}>→</div>
                  <div className={styles.timingItem}>
                    <span className={styles.timingLabel}>Wake Time</span>
                    <span className={styles.timingValue}>{formatTime(selectedItem.bedtimeEnd)}</span>
                  </div>
                  <div className={`${styles.timingItem} ${styles.timingEfficiency}`}>
                    <span className={styles.timingLabel}>Efficiency</span>
                    <span className={styles.timingValueGreen}>
                      {selectedItem.sleepEfficiency ? `${selectedItem.sleepEfficiency}%` : '--'}
                    </span>
                  </div>
                </div>
              </div>

              {/* Sleep Stages Section */}
              <div className={styles.section}>
                <h3 className={styles.sectionTitle}><span className={styles.sectionIcon}>🌙</span> Sleep Stages</h3>
                <div className={styles.stageBar}>
                  {stagePercentages && (
                    <>
                      <div 
                        className={`${styles.stage} ${styles.stageDeep}`} 
                        style={{ width: `${stagePercentages.deep}%` }}
                      >
                        {stagePercentages.deep > 10 ? `${stagePercentages.deep}%` : ''}
                      </div>
                      <div 
                        className={`${styles.stage} ${styles.stageLight}`} 
                        style={{ width: `${stagePercentages.light}%` }}
                      >
                        {stagePercentages.light > 10 ? `${stagePercentages.light}%` : ''}
                      </div>
                      <div 
                        className={`${styles.stage} ${styles.stageRem}`} 
                        style={{ width: `${stagePercentages.rem}%` }}
                      >
                        {stagePercentages.rem > 10 ? `${stagePercentages.rem}%` : ''}
                      </div>
                      <div 
                        className={`${styles.stage} ${styles.stageAwake}`} 
                        style={{ width: `${stagePercentages.awake}%` }}
                      >
                        {stagePercentages.awake > 10 ? `${stagePercentages.awake}%` : ''}
                      </div>
                    </>
                  )}
                </div>
                <div className={styles.stageLegend}>
                  <div className={styles.legendItem}>
                    <span className={`${styles.dot} ${styles.dotDeep}`}></span>
                    Deep: {selectedItem.deepSleep ? formatDurationSeconds(selectedItem.deepSleep) : '--'}
                  </div>
                  <div className={styles.legendItem}>
                    <span className={`${styles.dot} ${styles.dotLight}`}></span>
                    Light: {selectedItem.lightSleep ? formatDurationSeconds(selectedItem.lightSleep) : '--'}
                  </div>
                  <div className={styles.legendItem}>
                    <span className={`${styles.dot} ${styles.dotRem}`}></span>
                    REM: {selectedItem.remSleep ? formatDurationSeconds(selectedItem.remSleep) : '--'}
                  </div>
                  <div className={styles.legendItem}>
                    <span className={`${styles.dot} ${styles.dotAwake}`}></span>
                    Awake: {selectedItem.awakeTime ? formatDurationSeconds(selectedItem.awakeTime) : '--'}
                  </div>
                </div>
              </div>

              {/* Vitals Section */}
              <div className={styles.section}>
                <h3 className={styles.sectionTitle}><span className={styles.sectionIcon}>💗</span> Sleep Vitals</h3>
                <div className={styles.vitalsGrid}>
                  <div className={styles.vitalCard}>
                    <div className={styles.vitalIcon}>❤️</div>
                    <div className={styles.vitalValue}>{selectedItem.avgHeartRate?.toFixed(2) ?? '--'}</div>
                    <div className={styles.vitalLabel}>Avg Heart Rate</div>
                    <div className={styles.vitalSub}>
                      {selectedItem.lowestHeartRate ? `Lowest: ${selectedItem.lowestHeartRate} bpm` : 'bpm'}
                    </div>
                  </div>
                  <div className={styles.vitalCard}>
                    <div className={styles.vitalIcon}>💓</div>
                    <div className={styles.vitalValue}>{selectedItem.avgHrv ?? '--'}</div>
                    <div className={styles.vitalLabel}>HRV (RMSSD)</div>
                    <div className={styles.vitalSub}>ms</div>
                  </div>
                  <div className={styles.vitalCard}>
                    <div className={styles.vitalIcon}>�️</div>
                    <div className={styles.vitalValue}>
                      {selectedItem.avgBreath ? selectedItem.avgBreath.toFixed(1) : '--'}
                    </div>
                    <div className={styles.vitalLabel}>Breathing Rate</div>
                    <div className={styles.vitalSub}>breaths/min</div>
                  </div>
                  <div className={styles.vitalCard}>
                    <div className={styles.vitalIcon}>⏳</div>
                    <div className={styles.vitalValue}>
                      {selectedItem.sleepLatency ? `${Math.round(selectedItem.sleepLatency / 60)}m` : '--'}
                    </div>
                    <div className={styles.vitalLabel}>Sleep Latency</div>
                    <div className={styles.vitalSub}>time to fall asleep</div>
                  </div>
                </div>
              </div>

              {/* Time in Bed Info */}
              <div className={styles.timeInBedInfo}>
                <span>Time in bed: {selectedItem.timeInBed ? formatDurationSeconds(selectedItem.timeInBed) : '--'}</span>
                <span>Restless periods: {selectedItem.restlessPeriods ?? '--'}</span>
              </div>

              {/* Score Contributors Section */}
              {selectedItem.contributors && (
                <div className={styles.contributorsSection}>
                  <h3 className={styles.contributorTitle}>Score Contributors</h3>
                  <div className={styles.contributorList}>
                    <div className={styles.contributorItem}>
                      <span className={styles.contributorName}>Deep Sleep</span>
                      <span className={styles.contributorValue}>{selectedItem.contributors.deepSleep ?? '--'}</span>
                    </div>
                    <div className={styles.contributorItem}>
                      <span className={styles.contributorName}>Efficiency</span>
                      <span className={styles.contributorValue}>{selectedItem.contributors.efficiency ?? '--'}</span>
                    </div>
                    <div className={styles.contributorItem}>
                      <span className={styles.contributorName}>Latency</span>
                      <span className={styles.contributorValue}>{selectedItem.contributors.latency ?? '--'}</span>
                    </div>
                    <div className={styles.contributorItem}>
                      <span className={styles.contributorName}>REM Sleep</span>
                      <span className={styles.contributorValue}>{selectedItem.contributors.remSleep ?? '--'}</span>
                    </div>
                    <div className={styles.contributorItem}>
                      <span className={styles.contributorName}>Restfulness</span>
                      <span className={styles.contributorValue}>{selectedItem.contributors.restfulness ?? '--'}</span>
                    </div>
                    <div className={styles.contributorItem}>
                      <span className={styles.contributorName}>Timing</span>
                      <span className={styles.contributorValue}>{selectedItem.contributors.timing ?? '--'}</span>
                    </div>
                    <div className={styles.contributorItem}>
                      <span className={styles.contributorName}>Total Sleep</span>
                      <span className={styles.contributorValue}>{selectedItem.contributors.totalSleep ?? '--'}</span>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

interface OuraActivityTableProps {
  data: OuraActivityData[];
}

/** Calculate activity time percentages for activity bar */
const calculateActivityPercentages = (item: OuraActivityData) => {
  const total = (item.highActivity || 0) + (item.mediumActivity || 0) + (item.lowActivity || 0);
  if (total === 0) return { high: 0, medium: 0, low: 0 };
  return {
    high: Math.round(((item.highActivity || 0) / total) * 100),
    medium: Math.round(((item.mediumActivity || 0) / total) * 100),
    low: Math.round(((item.lowActivity || 0) / total) * 100)
  };
};

/**
 * Activity history table component with detail modal
 */
export const ActivityTable: React.FC<OuraActivityTableProps> = ({ data }) => {
  const [selectedItem, setSelectedItem] = useState<OuraActivityData | null>(null);

  const handleRowClick = (item: OuraActivityData) => {
    setSelectedItem(item);
  };

  const closeModal = () => {
    setSelectedItem(null);
  };

  const formatDistance = (meters: number | null) => {
    if (meters === null) return '--';
    return `${(meters / 1000).toFixed(1)} km`;
  };

  const activityPercentages = selectedItem ? calculateActivityPercentages(selectedItem) : null;

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
                <tr key={`${row.date}-${index}`} onClick={() => handleRowClick(row)}>
                  <td>{formatDate(row.date)}</td>
                  <td>
                    <span className={`${styles.scoreBadge} ${getScoreColorClass(row.score)}`}>
                      {row.score ?? '--'}
                    </span>
                  </td>
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

      {/* Activity Detail Modal - V1 Style */}
      {selectedItem && (
        <div className={styles.modalOverlay} onClick={closeModal}>
          <div className={styles.activityModal} onClick={(e) => e.stopPropagation()}>
            <div className={styles.activityModalHeader}>
              <div className={styles.headerContent}>
                <h2>Activity Analysis</h2>
                <span className={styles.modalDate}>{formatFullDate(selectedItem.date)}</span>
              </div>
              <button className={styles.closeButton} onClick={closeModal}>×</button>
            </div>
            <div className={styles.modalBody}>
              {/* Hero Metrics Row */}
              <div className={styles.activityHero}>
                <div className={styles.heroMetric}>
                  <div className={styles.heroIcon}>🏆</div>
                  <div className={styles.heroValue}>{selectedItem.score ?? '--'}</div>
                  <div className={styles.heroLabel}>Activity Score</div>
                </div>
                <div className={styles.heroMetric}>
                  <div className={styles.heroIcon}>👟</div>
                  <div className={styles.heroValue}>{selectedItem.steps?.toLocaleString() ?? '--'}</div>
                  <div className={styles.heroLabel}>Steps</div>
                </div>
                <div className={styles.heroMetric}>
                  <div className={styles.heroIcon}>📏</div>
                  <div className={styles.heroValue}>{formatDistance(selectedItem.distance)}</div>
                  <div className={styles.heroLabel}>Distance</div>
                </div>
              </div>

              {/* Calories Section */}
              <div className={styles.section}>
                <h3>🔥 Calories Burned</h3>
                <div className={styles.caloriesGrid}>
                  <div className={`${styles.calorieCard} ${styles.activeCard}`}>
                    <span className={styles.calorieValue}>{selectedItem.activeCalories ?? '--'}</span>
                    <span className={styles.calorieLabel}>Active Calories</span>
                  </div>
                  <div className={`${styles.calorieCard} ${styles.totalCard}`}>
                    <span className={styles.calorieValue}>{selectedItem.totalCalories ?? '--'}</span>
                    <span className={styles.calorieLabel}>Total Calories</span>
                  </div>
                </div>
              </div>

              {/* Activity Time Section */}
              <div className={styles.section}>
                <h3>⏱️ Activity Time</h3>
                <div className={styles.activityBar}>
                  {activityPercentages && (activityPercentages.high > 0 || activityPercentages.medium > 0 || activityPercentages.low > 0) ? (
                    <>
                      <div 
                        className={`${styles.activitySegment} ${styles.activityHigh}`} 
                        style={{ width: `${activityPercentages.high}%` }}
                      ></div>
                      <div 
                        className={`${styles.activitySegment} ${styles.activityMedium}`} 
                        style={{ width: `${activityPercentages.medium}%` }}
                      ></div>
                      <div 
                        className={`${styles.activitySegment} ${styles.activityLow}`} 
                        style={{ width: `${activityPercentages.low}%` }}
                      ></div>
                      <span className={styles.activityBarLabel}>100.0%</span>
                    </>
                  ) : (
                    <div className={styles.activityBarEmpty}></div>
                  )}
                </div>
                <div className={styles.activityLegend}>
                  <div className={styles.legendItem}>
                    <span className={`${styles.dot} ${styles.dotHigh}`}></span>
                    High: {selectedItem.highActivity ? formatDurationSeconds(selectedItem.highActivity) : '--'}
                  </div>
                  <div className={styles.legendItem}>
                    <span className={`${styles.dot} ${styles.dotMedium}`}></span>
                    Medium: {selectedItem.mediumActivity ? formatDurationSeconds(selectedItem.mediumActivity) : '--'}
                  </div>
                  <div className={styles.legendItem}>
                    <span className={`${styles.dot} ${styles.dotLow}`}></span>
                    Low: {selectedItem.lowActivity ? formatDurationSeconds(selectedItem.lowActivity) : '--'}
                  </div>
                </div>
              </div>

              {/* Rest Section */}
              <div className={styles.section}>
                <h3>🛋️ Rest & Recovery</h3>
                <div className={styles.restGrid}>
                  <div className={styles.restItem}>
                    <span className={styles.restIcon}>🪑</span>
                    <span className={styles.restValue}>
                      {selectedItem.sedentaryTime ? formatDurationSeconds(selectedItem.sedentaryTime) : '--'}
                    </span>
                    <span className={styles.restLabel}>Sedentary</span>
                  </div>
                  <div className={styles.restItem}>
                    <span className={styles.restIcon}>😴</span>
                    <span className={styles.restValue}>
                      {selectedItem.restingTime ? formatDurationSeconds(selectedItem.restingTime) : '--'}
                    </span>
                    <span className={styles.restLabel}>Resting</span>
                  </div>
                  <div className={styles.restItem}>
                    <span className={styles.restIcon}>🔔</span>
                    <span className={styles.restValue}>
                      {selectedItem.inactivityAlerts ?? '--'}
                    </span>
                    <span className={styles.restLabel}>Inactivity Alerts</span>
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

export default SleepTable;
