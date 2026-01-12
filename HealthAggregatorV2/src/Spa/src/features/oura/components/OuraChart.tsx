import React, { useRef, useEffect } from 'react';
import { Chart, registerables, ChartConfiguration } from 'chart.js';
import 'chartjs-adapter-date-fns';
import styles from './OuraChart.module.css';
import type { ChartSeries, TimeRange } from '../types';

Chart.register(...registerables);

const timeRanges: { value: TimeRange; label: string }[] = [
  { value: '7d', label: '7 Days' },
  { value: '30d', label: '30 Days' },
  { value: '90d', label: '3 Months' },
  { value: '6m', label: '6 Months' },
  { value: '1y', label: '1 Year' },
  { value: 'all', label: 'All Time' },
];

interface OuraChartProps {
  title: string;
  data: Array<{ date: string; [key: string]: string | number | null }>;
  series: ChartSeries[];
  onSeriesToggle: (id: string) => void;
  yAxisLabel?: string;
  timeRange?: TimeRange;
  onTimeRangeChange?: (range: TimeRange) => void;
}

/**
 * Oura-themed chart component
 */
export const OuraChart: React.FC<OuraChartProps> = ({
  title,
  data,
  series,
  onSeriesToggle,
  yAxisLabel,
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

    const datasets = series
      .filter(s => s.enabled)
      .map(s => ({
        label: s.label,
        data: data.map(d => ({
          x: new Date(d.date).getTime(),
          y: d[s.id] as number | null
        })).filter(d => d.y !== null),
        borderColor: s.color,
        backgroundColor: `${s.color}20`,
        fill: false,
        tension: 0.3,
        pointRadius: 4,
        pointHoverRadius: 6,
      }));

    const config: ChartConfiguration = {
      type: 'line',
      data: { datasets: datasets as never },
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
            title: yAxisLabel ? {
              display: true,
              text: yAxisLabel,
              color: '#888'
            } : undefined
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
            borderColor: 'rgba(0, 169, 157, 0.3)',
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
  }, [data, series, yAxisLabel]);

  return (
    <div className={styles.chartContainer}>
      <div className={styles.chartHeader}>
        <h2>{title}</h2>
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
        <div className={styles.toggles}>
          {series.map(s => (
            <label key={s.id} className={styles.toggle}>
              <input
                type="checkbox"
                checked={s.enabled}
                onChange={() => onSeriesToggle(s.id)}
              />
              <span style={{ color: s.color }}>● {s.label}</span>
            </label>
          ))}
        </div>
      </div>
      <div className={styles.chartWrapper}>
        {data.length > 0 ? (
          <canvas ref={chartRef} />
        ) : (
          <div className={styles.noData}>
            <p>No data available</p>
            <p>Sync your Oura Ring to see trends</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default OuraChart;
