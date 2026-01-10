import React, { useState } from 'react';
import { Button } from '@fluentui/react-components';
import { 
  FoodStatCard, 
  NutrientsGrid, 
  CalorieTrendChart, 
  MacroChart 
} from './components';
import { TimeRangeSelector } from '../weight/components';
import { useLatestNutrition, useCronometerSync } from './hooks';
import { LoadingSpinner } from '@shared/components/LoadingSpinner';
import { ErrorMessage } from '@shared/components/ErrorMessage';
import { formatDate, todayISO } from '@shared/utils';
import styles from './FoodPage.module.css';
import type { TimeRange, NutrientProgress, MacroBreakdown } from './types';

// Default nutrient goals
const defaultNutrients: NutrientProgress[] = [
  { name: 'Fiber', value: null, goal: 30, unit: 'g', variant: 'fiber' },
  { name: 'Sugar', value: null, goal: 50, unit: 'g', variant: 'sugar' },
  { name: 'Sodium', value: null, goal: 2300, unit: 'mg', variant: 'sodium' },
  { name: 'Cholesterol', value: null, goal: 300, unit: 'mg', variant: 'cholesterol' },
  { name: 'Vitamin D', value: null, goal: 20, unit: 'µg', variant: 'vitamin' },
  { name: 'Calcium', value: null, goal: 1000, unit: 'mg', variant: 'mineral' },
  { name: 'Iron', value: null, goal: 18, unit: 'mg', variant: 'mineral' },
  { name: 'Potassium', value: null, goal: 3500, unit: 'mg', variant: 'mineral' },
];

/**
 * Food & Nutrition tracking page
 * Displays Cronometer nutrition data
 */
export const FoodPage: React.FC = () => {
  const [timeRange, setTimeRange] = useState<TimeRange>('7d');
  const [selectedDate, setSelectedDate] = useState(todayISO());

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const { data: _latestData, isLoading, error, refetch } = useLatestNutrition();
  const { mutate: syncData, isPending: isSyncing } = useCronometerSync();

  // Mock data - would be replaced with real API data
  const mockCalorieData: Array<{ date: string; calories: number | null }> = [];
  const mockMacros: MacroBreakdown | null = null;

  const handleSync = () => {
    syncData();
  };

  const handlePrevDay = () => {
    const date = new Date(selectedDate);
    date.setDate(date.getDate() - 1);
    const dateStr = date.toISOString().split('T')[0] ?? '';
    setSelectedDate(dateStr);
  };

  const handleNextDay = () => {
    const today = todayISO();
    if (selectedDate < today) {
      const date = new Date(selectedDate);
      date.setDate(date.getDate() + 1);
      const dateStr = date.toISOString().split('T')[0] ?? '';
      setSelectedDate(dateStr);
    }
  };

  // Latest values (would extract from latestData)
  const calories = null;
  const protein = null;
  const carbs = null;
  const fat = null;

  const isToday = selectedDate === todayISO();

  if (isLoading) {
    return <LoadingSpinner label="Loading nutrition data..." />;
  }

  if (error) {
    return (
      <ErrorMessage
        title="Failed to load nutrition data"
        message={error.message}
        onRetry={() => refetch()}
      />
    );
  }

  return (
    <div className={styles.foodPage}>
      {/* Controls */}
      <div className={styles.controls}>
        <Button
          appearance="primary"
          onClick={handleSync}
          disabled={isSyncing}
          className={styles.syncBtn}
        >
          {isSyncing ? '🔄 Syncing...' : '🔄 Sync Cronometer'}
        </Button>
      </div>

      {/* Today's Summary Cards */}
      <div className={styles.statsGrid}>
        <FoodStatCard
          title="Calories"
          value={calories}
          unit="kcal"
          details={calories ? 'Today' : 'No data'}
          targetValue={2000}
        />
        <FoodStatCard
          title="Protein"
          value={protein}
          unit="g"
          details={protein ? 'Today' : 'No data'}
          targetValue={150}
        />
        <FoodStatCard
          title="Carbs"
          value={carbs}
          unit="g"
          details={carbs ? 'Today' : 'No data'}
          targetValue={250}
        />
        <FoodStatCard
          title="Fat"
          value={fat}
          unit="g"
          details={fat ? 'Today' : 'No data'}
          targetValue={65}
        />
      </div>

      {/* Calorie Trend Chart */}
      <div className={styles.chartSection}>
        <TimeRangeSelector
          value={timeRange}
          onChange={setTimeRange}
          variant="food"
        />
        <CalorieTrendChart data={mockCalorieData} />
      </div>

      {/* Macro Breakdown */}
      <MacroChart data={mockMacros} />

      {/* Key Nutrients */}
      <div className={styles.nutrientsSection}>
        <h2 className={styles.sectionTitle}>🧪 Key Nutrients</h2>
        <NutrientsGrid nutrients={defaultNutrients} />
      </div>

      {/* Food Log Section */}
      <div className={styles.foodLogSection}>
        <div className={styles.foodLogHeader}>
          <h2 className={styles.sectionTitle}>🍽️ Today's Food Log</h2>
          <div className={styles.dateSelector}>
            <Button
              appearance="subtle"
              onClick={handlePrevDay}
              className={styles.dateBtn}
            >
              ◀
            </Button>
            <span className={styles.dateDisplay}>
              {isToday ? 'Today' : formatDate(selectedDate)}
            </span>
            <Button
              appearance="subtle"
              onClick={handleNextDay}
              disabled={isToday}
              className={styles.dateBtn}
            >
              ▶
            </Button>
          </div>
        </div>
        <div className={styles.foodLogContent}>
          <div className={styles.emptyState}>
            <h3>No food logged</h3>
            <p>Sync your Cronometer data to see your food log</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default FoodPage;
