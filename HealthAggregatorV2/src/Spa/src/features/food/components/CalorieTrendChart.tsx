import React, { useRef, useEffect } from 'react';
import { Chart, registerables, ChartConfiguration } from 'chart.js';
import 'chartjs-adapter-date-fns';
import styles from './CalorieTrendChart.module.css';
import type { TimeRange } from '../types';

Chart.register(...registerables);

const timeRanges: { value: TimeRange; label: string }[] = [
  { value: '7d', label: '7 Days' },
  { value: '30d', label: '30 Days' },
  { value: '90d', label: '3 Months' },
  { value: '6m', label: '6 Months' },
  { value: '1y', label: '1 Year' },
  { value: 'all', label: 'All Time' },
];

interface CalorieTrendChartProps {
  data: Array<{ date: string; calories: number | null }>;
  timeRange?: TimeRange;
  onTimeRangeChange?: (range: TimeRange) => void;
}

/**
 * Calorie trend line chart
 */
export const CalorieTrendChart: React.FC<CalorieTrendChartProps> = ({ 
  data,
  timeRange,
  onTimeRangeChange
}) => {
  const chartRef = useRef<HTMLCanvasElement>(null);
  const chartInstance = useRef<Chart | null>(null);

  useEffect(() => {
    if (!chartRef.current || data.length === 0) return;

    if (chartInstance.current) {
      chartInstance.current.destroy();
    }

    const ctx = chartRef.current.getContext('2d');
    if (!ctx) return;

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        datasets: [{
          label: 'Calories',
          data: data
            .filter(d => d.calories !== null)
            .map(d => ({
              x: new Date(d.date).getTime(),
              y: d.calories as number
            })) as never,
          borderColor: '#f97316',
          backgroundColor: 'rgba(249, 115, 22, 0.2)',
          fill: true,
          tension: 0.3,
          pointRadius: 4,
          pointHoverRadius: 6,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: {
          mode: 'index',
          intersect: false,
        },
        scales: {
          x: {
            type: 'time',
            time: {
              unit: 'day',
              displayFormats: {
                day: 'MMM d'
              }
            },
            grid: {
              color: 'rgba(255, 255, 255, 0.05)',
            },
            ticks: {
              color: '#888',
            }
          },
          y: {
            grid: {
              color: 'rgba(255, 255, 255, 0.05)',
            },
            ticks: {
              color: '#888',
            },
            title: {
              display: true,
              text: 'kcal',
              color: '#888'
            }
          }
        },
        plugins: {
          legend: {
            display: false,
          },
          tooltip: {
            backgroundColor: 'rgba(26, 26, 46, 0.95)',
            titleColor: '#e0e0e0',
            bodyColor: '#e0e0e0',
            borderColor: 'rgba(249, 115, 22, 0.3)',
            borderWidth: 1,
            cornerRadius: 8,
            padding: 12,
          }
        }
      }
    };

    chartInstance.current = new Chart(ctx, config);

    return () => {
      if (chartInstance.current) {
        chartInstance.current.destroy();
      }
    };
  }, [data]);

  return (
    <div className={styles.chartContainer}>
      <div className={styles.chartHeader}>
        <h2 className={styles.title}>📊 Calorie Trend</h2>
        {timeRange && onTimeRangeChange && (
          <div className={styles.timeRangeButtons}>
            {timeRanges.map((range) => (
              <button
                key={range.value}
                type="button"
                className={`${styles.rangeBtn} ${timeRange === range.value ? styles.active : ''}`}
                onClick={() => onTimeRangeChange(range.value)}
              >
                {range.label}
              </button>
            ))}
          </div>
        )}
      </div>
      <div className={styles.chartWrapper}>
        {data.length > 0 ? (
          <canvas ref={chartRef} />
        ) : (
          <div className={styles.noData}>
            <p>No calorie data available</p>
            <p>Sync your Cronometer to see trends</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default CalorieTrendChart;
