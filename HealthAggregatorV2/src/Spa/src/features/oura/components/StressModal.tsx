import React, { useState, useMemo } from 'react';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';
import { Bar } from 'react-chartjs-2';
import styles from './StressModal.module.css';

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend);

interface StressRecord {
  day: string;
  stressHigh: number | null;
  recoveryHigh: number | null;
  daySummary: string | null;
}

interface StressModalProps {
  isOpen: boolean;
  onClose: () => void;
  stressData: StressRecord[];
  latestStress: StressRecord | null;
}

type TimeRange = '7d' | '30d' | '90d' | 'all';

const formatDate = (dateStr: string): string => {
  const date = new Date(dateStr + 'T00:00:00');
  return date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
};

const formatShortDate = (dateStr: string): string => {
  const date = new Date(dateStr + 'T00:00:00');
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
};

const secondsToMinutes = (seconds: number | null): number => {
  if (seconds === null) return 0;
  return Math.round(seconds / 60);
};

const getStatusIcon = (summary: string | null): string => {
  if (!summary) return '--';
  switch (summary.toLowerCase()) {
    case 'restored': return '✨';
    case 'normal': return '😊';
    case 'strained': return '😓';
    case 'high': return '😰';
    default: return '--';
  }
};

const getStatusLabel = (summary: string | null | undefined): string => {
  if (!summary) return '--';
  return summary.charAt(0).toUpperCase() + summary.slice(1);
};

export const StressModal: React.FC<StressModalProps> = ({
  isOpen,
  onClose,
  stressData,
  latestStress,
}) => {
  const [timeRange, setTimeRange] = useState<TimeRange>('30d');

  const filteredData = useMemo(() => {
    if (!stressData?.length) return [];
    
    const now = new Date();
    let cutoffDate: Date;
    
    switch (timeRange) {
      case '7d':
        cutoffDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
        break;
      case '30d':
        cutoffDate = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
        break;
      case '90d':
        cutoffDate = new Date(now.getTime() - 90 * 24 * 60 * 60 * 1000);
        break;
      default:
        cutoffDate = new Date(0);
    }
    
    return stressData
      .filter(d => new Date(d.day) >= cutoffDate)
      .sort((a, b) => a.day.localeCompare(b.day));
  }, [stressData, timeRange]);

  // Calculate 30-day average stress
  const avgStress = useMemo(() => {
    const thirtyDaysAgo = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000);
    const recentData = stressData?.filter(d => new Date(d.day) >= thirtyDaysAgo) ?? [];
    if (recentData.length === 0) return 0;
    const totalMinutes = recentData.reduce((sum, d) => sum + secondsToMinutes(d.stressHigh), 0);
    return Math.round(totalMinutes / recentData.length);
  }, [stressData]);

  // Chart data
  const chartData = useMemo(() => ({
    labels: filteredData.map(d => formatShortDate(d.day)),
    datasets: [
      {
        label: 'Stress (min)',
        data: filteredData.map(d => secondsToMinutes(d.stressHigh)),
        backgroundColor: '#ef4444',
        borderRadius: 4,
      },
      {
        label: 'Recovery (min)',
        data: filteredData.map(d => secondsToMinutes(d.recoveryHigh)),
        backgroundColor: '#22c55e',
        borderRadius: 4,
      },
    ],
  }), [filteredData]);

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'top' as const,
        labels: {
          color: 'rgba(255, 255, 255, 0.8)',
          usePointStyle: true,
          pointStyle: 'rectRounded',
        },
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        titleColor: '#fff',
        bodyColor: '#fff',
        borderColor: 'rgba(255, 255, 255, 0.1)',
        borderWidth: 1,
        callbacks: {
          label: (context: { dataset: { label?: string }; raw: unknown }) => {
            return `${context.dataset.label}: ${context.raw} min`;
          },
        },
      },
    },
    scales: {
      x: {
        grid: {
          color: 'rgba(255, 255, 255, 0.1)',
        },
        ticks: {
          color: 'rgba(255, 255, 255, 0.7)',
        },
      },
      y: {
        grid: {
          color: 'rgba(255, 255, 255, 0.1)',
        },
        ticks: {
          color: 'rgba(255, 255, 255, 0.7)',
          callback: (value: string | number) => `${value} min`,
        },
      },
    },
  };

  if (!isOpen) return null;

  const stressMinutes = secondsToMinutes(latestStress?.stressHigh ?? null);
  const recoveryMinutes = secondsToMinutes(latestStress?.recoveryHigh ?? null);
  const todayStatus = latestStress?.daySummary;

  return (
    <div className={styles.modalOverlay} onClick={onClose}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <div className={styles.modalHeader}>
          <h2>🧘 Daily Stress Analysis</h2>
          <button className={styles.closeButton} onClick={onClose}>×</button>
        </div>
        
        <div className={styles.modalBody}>
          {/* Summary Cards */}
          <div className={styles.summaryCards}>
            <div className={styles.summaryCard}>
              <div className={styles.cardLabel}>TODAY'S STATUS</div>
              <div className={styles.cardValue}>
                <span className={styles.statusDot} data-status={todayStatus?.toLowerCase()} />
                {getStatusLabel(todayStatus)}
              </div>
            </div>
            <div className={styles.summaryCard}>
              <div className={styles.cardLabel}>STRESS TIME</div>
              <div className={styles.cardValueRed}>{stressMinutes} min</div>
            </div>
            <div className={styles.summaryCard}>
              <div className={styles.cardLabel}>RECOVERY TIME</div>
              <div className={styles.cardValueGreen}>{recoveryMinutes} min</div>
            </div>
            <div className={styles.summaryCard}>
              <div className={styles.cardLabel}>30-DAY AVG STRESS</div>
              <div className={styles.cardValueRed}>{avgStress} min</div>
            </div>
          </div>

          {/* Chart Section */}
          <div className={styles.chartSection}>
            <div className={styles.chartHeader}>
              <h3>Stress & Recovery Trend</h3>
              <div className={styles.timeRangeButtons}>
                {(['7d', '30d', '90d', 'all'] as const).map(range => (
                  <button
                    key={range}
                    className={`${styles.rangeBtn} ${timeRange === range ? styles.active : ''}`}
                    onClick={() => setTimeRange(range)}
                  >
                    {range === '7d' ? '7 Days' : range === '30d' ? '30 Days' : range === '90d' ? '90 Days' : 'All'}
                  </button>
                ))}
              </div>
            </div>
            <div className={styles.chartContainer}>
              <Bar data={chartData} options={chartOptions} />
            </div>
          </div>

          {/* History Table */}
          <div className={styles.historySection}>
            <h3>Recent History</h3>
            <div className={styles.tableWrapper}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>DATE</th>
                    <th>STATUS</th>
                    <th>STRESS (MIN)</th>
                    <th>RECOVERY (MIN)</th>
                  </tr>
                </thead>
                <tbody>
                  {[...filteredData].reverse().slice(0, 10).map(record => (
                    <tr key={record.day}>
                      <td>{formatDate(record.day)}</td>
                      <td>
                        {record.daySummary ? (
                          <span className={styles.statusBadge} data-status={record.daySummary.toLowerCase()}>
                            {getStatusIcon(record.daySummary)} {getStatusLabel(record.daySummary)}
                          </span>
                        ) : '--'}
                      </td>
                      <td>{secondsToMinutes(record.stressHigh)}</td>
                      <td>{secondsToMinutes(record.recoveryHigh)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default StressModal;
