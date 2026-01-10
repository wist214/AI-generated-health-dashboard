import React, { useRef, useEffect } from 'react';
import { Chart, registerables, ChartConfiguration } from 'chart.js';
import styles from './MacroChart.module.css';
import type { MacroBreakdown } from '../types';

Chart.register(...registerables);

interface MacroChartProps {
  data: MacroBreakdown | null;
}

/**
 * Macro breakdown doughnut chart
 */
export const MacroChart: React.FC<MacroChartProps> = ({ data }) => {
  const chartRef = useRef<HTMLCanvasElement>(null);
  const chartInstance = useRef<Chart | null>(null);

  useEffect(() => {
    if (!chartRef.current) return;

    if (chartInstance.current) {
      chartInstance.current.destroy();
    }

    const ctx = chartRef.current.getContext('2d');
    if (!ctx) return;

    const hasData = data && (data.protein > 0 || data.carbs > 0 || data.fat > 0);

    const config: ChartConfiguration<'doughnut'> = {
      type: 'doughnut',
      data: {
        labels: ['Protein', 'Carbs', 'Fat'],
        datasets: [{
          data: hasData ? [data.protein, data.carbs, data.fat] : [1, 1, 1],
          backgroundColor: hasData 
            ? ['#3b82f6', '#f59e0b', '#ef4444']
            : ['#333', '#333', '#333'],
          borderWidth: 0,
          hoverOffset: 8,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: true,
        cutout: '60%',
        plugins: {
          legend: {
            display: false,
          },
          tooltip: {
            enabled: hasData ?? false,
            backgroundColor: 'rgba(26, 26, 46, 0.95)',
            titleColor: '#e0e0e0',
            bodyColor: '#e0e0e0',
            borderColor: 'rgba(255, 255, 255, 0.1)',
            borderWidth: 1,
            cornerRadius: 8,
            padding: 12,
            callbacks: {
              label: (context) => {
                const value = context.raw as number;
                const total = (data?.protein || 0) + (data?.carbs || 0) + (data?.fat || 0);
                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                return `${context.label}: ${value.toFixed(0)}g (${percentage}%)`;
              }
            }
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

  const hasData = data && (data.protein > 0 || data.carbs > 0 || data.fat > 0);
  const total = hasData ? data.protein + data.carbs + data.fat : 0;

  return (
    <div className={styles.container}>
      <h2 className={styles.title}>🥧 Macro Breakdown</h2>
      <div className={styles.content}>
        <div className={styles.chartWrapper}>
          <canvas ref={chartRef} />
        </div>
        <div className={styles.legend}>
          <div className={styles.legendItem}>
            <span className={`${styles.dot} ${styles.protein}`} />
            <span className={styles.label}>Protein</span>
            <span className={styles.value}>
              {hasData ? `${data.protein.toFixed(0)}g` : '--'}
              {hasData && total > 0 && (
                <span className={styles.percentage}>
                  ({((data.protein / total) * 100).toFixed(0)}%)
                </span>
              )}
            </span>
          </div>
          <div className={styles.legendItem}>
            <span className={`${styles.dot} ${styles.carbs}`} />
            <span className={styles.label}>Carbs</span>
            <span className={styles.value}>
              {hasData ? `${data.carbs.toFixed(0)}g` : '--'}
              {hasData && total > 0 && (
                <span className={styles.percentage}>
                  ({((data.carbs / total) * 100).toFixed(0)}%)
                </span>
              )}
            </span>
          </div>
          <div className={styles.legendItem}>
            <span className={`${styles.dot} ${styles.fat}`} />
            <span className={styles.label}>Fat</span>
            <span className={styles.value}>
              {hasData ? `${data.fat.toFixed(0)}g` : '--'}
              {hasData && total > 0 && (
                <span className={styles.percentage}>
                  ({((data.fat / total) * 100).toFixed(0)}%)
                </span>
              )}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default MacroChart;
