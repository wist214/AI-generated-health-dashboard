import React, { useState, useMemo } from 'react';
import { Button } from '@fluentui/react-components';
import { 
  FoodStatCard, 
  NutrientsGrid, 
  CalorieTrendChart, 
  MacroChart 
} from './components';
import { TimeRangeSelector } from '../weight/components';
import { useLatestNutrition, useNutritionHistory, useCronometerSync } from './hooks';
import { LoadingSpinner } from '@shared/components/LoadingSpinner';
import { ErrorMessage } from '@shared/components/ErrorMessage';
import { formatDate, todayISO, getDateRangeFromTimeRange, formatRelativeTime } from '@shared/utils';
import styles from './FoodPage.module.css';
import type { TimeRange, NutrientProgress } from './types';

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

  // Get date range for history query
  const { startDate, endDate } = useMemo(() => 
    getDateRangeFromTimeRange(timeRange),
    [timeRange]
  );

  // Fetch data from API
  const { data: latestData, isLoading: isLoadingLatest, error: latestError, refetch } = useLatestNutrition();
  const { data: historyData, isLoading: isLoadingHistory, error: historyError } = useNutritionHistory(startDate, endDate);
  const { mutate: syncData, isPending: isSyncing } = useCronometerSync();

  const isLoading = isLoadingLatest || isLoadingHistory;
  const error = latestError || historyError;

  // Transform history data for chart
  const calorieChartData = useMemo(() => {
    if (!historyData) return [];
    return historyData.map(d => ({
      date: d.date,
      calories: d.calories,
    }));
  }, [historyData]);

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

  // Extract latest values from dashboard summary
  const calories = latestData?.calories ?? null;
  const protein = latestData?.protein ?? null;
  const carbs = latestData?.carbs ?? null;
  const fat = latestData?.fat ?? null;
  const macros = latestData?.macros ?? null;
  const lastUpdated = latestData?.lastUpdated;

  const isToday = selectedDate === todayISO();

  if (isLoading) {
    return <LoadingSpinner label="Loading nutrition data..." />;
  }

  if (error) {
    return (
      <ErrorMessage
        title="Failed to load nutrition data"
        message={(error as Error).message}
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
          details={lastUpdated ? formatRelativeTime(lastUpdated) : 'No data'}
          targetValue={2000}
        />
        <FoodStatCard
          title="Protein"
          value={protein}
          unit="g"
          details={lastUpdated ? formatRelativeTime(lastUpdated) : 'No data'}
          targetValue={150}
        />
        <FoodStatCard
          title="Carbs"
          value={carbs}
          unit="g"
          details={lastUpdated ? formatRelativeTime(lastUpdated) : 'No data'}
          targetValue={250}
        />
        <FoodStatCard
          title="Fat"
          value={fat}
          unit="g"
          details={lastUpdated ? formatRelativeTime(lastUpdated) : 'No data'}
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
        <CalorieTrendChart data={calorieChartData} />
      </div>

      {/* Macro Breakdown */}
      <MacroChart data={macros} />

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
