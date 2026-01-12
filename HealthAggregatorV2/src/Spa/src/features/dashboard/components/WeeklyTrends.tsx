import React, { useRef, useEffect } from 'react';
import { Chart, registerables, ChartConfiguration } from 'chart.js';
import 'chartjs-adapter-date-fns';
import styles from './WeeklyTrends.module.css';

Chart.register(...registerables);

interface TrendDataPoint {
  date: string;
  sleepScore: number | null;
  readinessScore: number | null;
  weight: number | null;
}

interface WeeklyTrendsProps {
  data: TrendDataPoint[];
}

/**
 * Weekly trends chart showing sleep score, readiness, and weight
 */
export const WeeklyTrends: React.FC<WeeklyTrendsProps> = ({ data }) => {
  const chartRef = useRef<HTMLCanvasElement>(null);
  const chartInstance = useRef<Chart | null>(null);

  useEffect(() => {
    if (!chartRef.current || data.length === 0) return;

    // Destroy existing chart
    if (chartInstance.current) {
      chartInstance.current.destroy();
    }

    const ctx = chartRef.current.getContext('2d');
    if (!ctx) return;

    // Prepare datasets - filter out null values
    const sleepData = data
      .filter(d => d.sleepScore !== null)
      .map(d => ({ x: new Date(d.date).getTime(), y: d.sleepScore as number }));
    
    const readinessData = data
      .filter(d => d.readinessScore !== null)
      .map(d => ({ x: new Date(d.date).getTime(), y: d.readinessScore as number }));
    
    const weightData = data
      .filter(d => d.weight !== null)
      .map(d => ({ x: new Date(d.date).getTime(), y: d.weight as number }));

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        datasets: [
          {
            label: 'Sleep Score',
            data: sleepData,
            borderColor: '#00a99d',
            backgroundColor: 'rgba(0, 169, 157, 0.1)',
            fill: false,
            tension: 0.3,
            pointRadius: 4,
            pointHoverRadius: 6,
            yAxisID: 'y',
          },
          {
            label: 'Readiness',
            data: readinessData,
            borderColor: '#ff6b6b',
            backgroundColor: 'rgba(255, 107, 107, 0.1)',
            fill: false,
            tension: 0.3,
            pointRadius: 4,
            pointHoverRadius: 6,
            yAxisID: 'y',
          },
          {
            label: 'Weight',
            data: weightData,
            borderColor: '#4ecdc4',
            backgroundColor: 'rgba(78, 205, 196, 0.1)',
            fill: false,
            tension: 0.3,
            pointRadius: 4,
            pointHoverRadius: 6,
            yAxisID: 'y1',
          },
        ],
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
                day: 'MMM d',
              },
            },
            grid: {
              color: 'rgba(255, 255, 255, 0.05)',
            },
            ticks: {
              color: '#888',
            },
          },
          y: {
            type: 'linear',
            display: true,
            position: 'left',
            min: 0,
            max: 100,
            title: {
              display: true,
              text: 'Score',
              color: '#888',
            },
            grid: {
              color: 'rgba(255, 255, 255, 0.05)',
            },
            ticks: {
              color: '#888',
            },
          },
          y1: {
            type: 'linear',
            display: true,
            position: 'right',
            title: {
              display: true,
              text: 'Weight (kg)',
              color: '#888',
            },
            grid: {
              drawOnChartArea: false,
            },
            ticks: {
              color: '#888',
            },
          },
        },
        plugins: {
          legend: {
            display: true,
            position: 'top',
            labels: {
              color: '#e0e0e0',
              usePointStyle: true,
              padding: 15,
            },
          },
          tooltip: {
            backgroundColor: 'rgba(26, 26, 46, 0.95)',
            titleColor: '#e0e0e0',
            bodyColor: '#e0e0e0',
            borderColor: 'rgba(255, 255, 255, 0.1)',
            borderWidth: 1,
            cornerRadius: 8,
            padding: 12,
          },
        },
      },
    };

    chartInstance.current = new Chart(ctx, config);

    return () => {
      if (chartInstance.current) {
        chartInstance.current.destroy();
      }
    };
  }, [data]);

  return (
    <div className={styles.container}>
      <h3 className={styles.title}>📈 Weekly Trends</h3>
      <div className={styles.chartWrapper}>
        {data.length > 0 ? (
          <canvas ref={chartRef} />
        ) : (
          <div className={styles.noData}>
            <p>No trend data available</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default WeeklyTrends;
