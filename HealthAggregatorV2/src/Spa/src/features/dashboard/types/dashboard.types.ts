/**
 * Dashboard feature types
 */

export interface DashboardMetric {
  title: string;
  value: number | null;
  unit?: string;
  icon: string;
  source: string;
  date: string;
  variant: MetricCardVariant;
  progress?: ProgressItem[];
}

export interface ProgressItem {
  label: string;
  value: string;
  absValue?: string;
  type: 'positive' | 'negative' | 'neutral';
}

export type MetricCardVariant = 
  | 'weight' 
  | 'bodyScore' 
  | 'sleep' 
  | 'readiness'
  | 'calories'
  | 'protein'
  | 'carbs'
  | 'fat';

export interface QuickStat {
  icon: string;
  label: string;
  value: string;
}

export interface DashboardData {
  metrics: DashboardMetric[];
  quickStats: QuickStat[];
  lastUpdated: string;
}
