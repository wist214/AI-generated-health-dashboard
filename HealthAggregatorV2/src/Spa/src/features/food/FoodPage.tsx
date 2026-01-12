import React, { useState, useMemo } from 'react';
import { Button } from '@fluentui/react-components';
import { 
  FoodStatCard, 
  NutrientsGrid, 
  CalorieTrendChart, 
  MacroChart,
  FoodLogTable 
} from './components';
import { useLatestNutrition, useNutritionHistory, useCronometerSync } from './hooks';
import { LoadingSpinner } from '@shared/components/LoadingSpinner';
import { ErrorMessage } from '@shared/components/ErrorMessage';
import { formatDate, todayISO, getDateRangeFromTimeRange, formatRelativeTime } from '@shared/utils';
import styles from './FoodPage.module.css';
import type { TimeRange, NutrientProgress } from './types';

// Helper to check if date is yesterday
const isYesterday = (dateStr: string): boolean => {
  const date = new Date(dateStr);
  const yesterday = new Date();
  yesterday.setDate(yesterday.getDate() - 1);
  return date.toDateString() === yesterday.toDateString();
};

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

  // Transform history data for chart (filtered by time range)
  const calorieChartData = useMemo(() => {
    if (!historyData) return [];
    return historyData
      .filter(d => d.date >= startDate && d.date <= endDate)
      .map(d => ({
        date: d.date,
        calories: d.calories,
      }));
  }, [historyData, startDate, endDate]);

  // Get data for selected date
  const selectedDateData = useMemo(() => {
    if (!historyData) return null;
    return historyData.find(d => d.date === selectedDate) || null;
  }, [historyData, selectedDate]);

  // Filter food servings for selected date
  const filteredFoodServings = useMemo(() => {
    const allServings = latestData?.foodServings ?? [];
    return allServings.filter(s => s.day === selectedDate);
  }, [latestData?.foodServings, selectedDate]);

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

  // Extract values for selected date only (no fallback - show null if no data for that date)
  const calories = selectedDateData?.calories ?? null;
  const protein = selectedDateData?.protein ?? null;
  const carbs = selectedDateData?.carbs ?? null;
  const fat = selectedDateData?.fat ?? null;
  const macros = selectedDateData ? { protein: selectedDateData.protein ?? 0, carbs: selectedDateData.carbs ?? 0, fat: selectedDateData.fat ?? 0 } : null;
  const lastUpdated = latestData?.lastUpdated;

  const isToday = selectedDate === todayISO();

  // Build nutrients from selected date data
  const nutrients: NutrientProgress[] = useMemo(() => [
    { name: 'Fiber', value: selectedDateData?.fiber ?? null, goal: 30, unit: 'g', variant: 'fiber' },
    { name: 'Sugar', value: selectedDateData?.sugars ?? null, goal: 50, unit: 'g', variant: 'sugar' },
    { name: 'Sodium', value: selectedDateData?.sodium ?? null, goal: 2300, unit: 'mg', variant: 'sodium' },
    { name: 'Cholesterol', value: selectedDateData?.cholesterol ?? null, goal: 300, unit: 'mg', variant: 'cholesterol' },
    { name: 'Vitamin D', value: selectedDateData?.vitaminD ?? null, goal: 20, unit: 'µg', variant: 'vitamin' },
    { name: 'Calcium', value: selectedDateData?.calcium ?? null, goal: 1000, unit: 'mg', variant: 'mineral' },
    { name: 'Iron', value: selectedDateData?.iron ?? null, goal: 18, unit: 'mg', variant: 'mineral' },
    { name: 'Potassium', value: selectedDateData?.potassium ?? null, goal: 3500, unit: 'mg', variant: 'mineral' },
  ], [selectedDateData]);

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

      {/* Selected Date Summary Cards */}
      <div className={styles.statsGrid}>
        <FoodStatCard
          title="Calories"
          value={calories}
          unit="kcal"
          details={isToday ? (lastUpdated ? formatRelativeTime(lastUpdated) : 'No data') : (isYesterday(selectedDate) ? 'Yesterday' : formatDate(selectedDate))}
          targetValue={2000}
        />
        <FoodStatCard
          title="Protein"
          value={protein}
          unit="g"
          details={isToday ? (lastUpdated ? formatRelativeTime(lastUpdated) : 'No data') : (isYesterday(selectedDate) ? 'Yesterday' : formatDate(selectedDate))}
          targetValue={150}
        />
        <FoodStatCard
          title="Carbs"
          value={carbs}
          unit="g"
          details={isToday ? (lastUpdated ? formatRelativeTime(lastUpdated) : 'No data') : (isYesterday(selectedDate) ? 'Yesterday' : formatDate(selectedDate))}
          targetValue={250}
        />
        <FoodStatCard
          title="Fat"
          value={fat}
          unit="g"
          details={isToday ? (lastUpdated ? formatRelativeTime(lastUpdated) : 'No data') : (isYesterday(selectedDate) ? 'Yesterday' : formatDate(selectedDate))}
          targetValue={65}
        />
      </div>

      {/* Calorie Trend Chart */}
      <div className={styles.chartSection}>
        <CalorieTrendChart 
          data={calorieChartData}
          timeRange={timeRange}
          onTimeRangeChange={setTimeRange}
        />
      </div>

      {/* Macro Breakdown */}
      <MacroChart data={macros} />

      {/* Key Nutrients */}
      <div className={styles.nutrientsSection}>
        <h2 className={styles.sectionTitle}>🧪 Key Nutrients</h2>
        <NutrientsGrid nutrients={nutrients} />
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
              {isToday ? 'Today' : isYesterday(selectedDate) ? 'Yesterday' : formatDate(selectedDate)}
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
          <FoodLogTable data={filteredFoodServings} selectedDate={selectedDate} />
        </div>
      </div>
    </div>
  );
};

export default FoodPage;
