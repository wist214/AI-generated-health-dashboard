import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { get, API_ENDPOINTS } from '@shared/api';
import { syncCronometer } from '@shared/api/syncClient';
import type { MacroBreakdown, NutritionHistoryItem } from '../types';

/**
 * API response types matching Cronometer API
 * NOTE: API returns 'energy' field for calories, and 'foodName' for food item name
 */
interface CronometerDataResponse {
  dailyNutrition: Array<{
    date: string;
    energy: number | null;  // API uses 'energy' for calories
    protein: number | null;
    carbs: number | null;
    fat: number | null;
    fiber: number | null;
    sugars: number | null;  // API uses 'sugars'
    sodium: number | null;
    cholesterol: number | null;
    vitaminD: number | null;
    calcium: number | null;
    iron: number | null;
    potassium: number | null;
  }>;
  foodServings: Array<{
    day: string;
    foodName: string;  // API uses 'foodName' not 'name'
    amount: string | null;  // Combined amount string like "1.00 Bar"
    energy: number | null;  // API uses 'energy' for calories
    protein: number | null;
    carbs: number | null;
    fat: number | null;
    fiber: number | null;
    sugars: number | null;
    sodium: number | null;
    group: string | null;
    category: string | null;
  }>;
  exercises: Array<{
    day: string;
    name: string | null;
    caloriesBurned: number | null;
    minutes: number | null;
  }>;
  lastSync: string | null;
}

interface CronometerDailyResponse {
  date: string;
  nutrition: {
    date: string;
    energy: number | null;  // API uses 'energy' for calories
    protein: number | null;
    carbs: number | null;
    fat: number | null;
    fiber: number | null;
    sugar: number | null;
    sodium: number | null;
    cholesterol: number | null;
    vitaminD: number | null;
    calcium: number | null;
    iron: number | null;
    potassium: number | null;
  } | null;
  servings: Array<{
    day: string;
    foodName: string;  // API uses 'foodName' not 'name'
    amount: string | null;  // Combined amount string like "1.00 Bar"
    energy: number | null;  // API uses 'energy' for calories
    protein: number | null;
    carbs: number | null;
    fat: number | null;
    fiber: number | null;
    sugars: number | null;
    sodium: number | null;
    group: string | null;
    category: string | null;
  }>;
}

/**
 * Query key factory for food data
 */
export const foodKeys = {
  all: ['food'] as const,
  daily: (date: string) => [...foodKeys.all, 'daily', date] as const,
  history: (range: string) => [...foodKeys.all, 'history', range] as const,
  latest: () => [...foodKeys.all, 'latest'] as const,
  data: () => [...foodKeys.all, 'data'] as const,
};

/**
 * Fetch all Cronometer data
 */
const fetchCronometerData = async (): Promise<CronometerDataResponse> => {
  return get<CronometerDataResponse>(API_ENDPOINTS.CRONOMETER.DATA);
};

/**
 * Fetch daily Cronometer data
 */
const fetchCronometerDaily = async (date: string): Promise<CronometerDailyResponse> => {
  return get<CronometerDailyResponse>(API_ENDPOINTS.CRONOMETER.DAILY(date));
};

/**
 * Trigger Cronometer sync via Azure Functions
 */
const syncCronometerData = async () => {
  return syncCronometer();
};

/**
 * Hook for fetching daily nutrition
 */
export const useDailyNutrition = (date: string) => {
  return useQuery({
    queryKey: foodKeys.daily(date),
    queryFn: () => fetchCronometerDaily(date),
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Nutrition history item with all nutrient fields
 */
interface FullNutritionHistoryItem extends NutritionHistoryItem {
  fiber: number | null;
  sugars: number | null;
  sodium: number | null;
  cholesterol: number | null;
  vitaminD: number | null;
  calcium: number | null;
  iron: number | null;
  potassium: number | null;
}

/**
 * Hook for fetching nutrition history
 */
export const useNutritionHistory = (_startDate: string, _endDate: string) => {
  return useQuery({
    queryKey: foodKeys.history('all'),
    queryFn: fetchCronometerData,
    staleTime: 5 * 60 * 1000,
    select: (data): FullNutritionHistoryItem[] => data.dailyNutrition
      .map(d => ({
        date: d.date,
        calories: d.energy,  // Map 'energy' to 'calories' for frontend
        protein: d.protein,
        carbs: d.carbs,
        fat: d.fat,
        fiber: d.fiber,
        sugars: d.sugars,
        sodium: d.sodium,
        cholesterol: d.cholesterol,
        vitaminD: d.vitaminD,
        calcium: d.calcium,
        iron: d.iron,
        potassium: d.potassium,
      })),
  });
};

/**
 * Hook for fetching latest nutrition
 */
export const useLatestNutrition = () => {
  return useQuery({
    queryKey: foodKeys.data(),
    queryFn: fetchCronometerData,
    staleTime: 5 * 60 * 1000,
    select: (data) => {
      const latest = data.dailyNutrition?.[0];
      const hasValidMacros = latest && 
        latest.protein !== null && 
        latest.carbs !== null && 
        latest.fat !== null;
      return {
        calories: latest?.energy ?? null,  // Map 'energy' to 'calories'
        protein: latest?.protein ?? null,
        carbs: latest?.carbs ?? null,
        fat: latest?.fat ?? null,
        lastUpdated: data.lastSync,
        macros: hasValidMacros 
          ? { protein: latest.protein!, carbs: latest.carbs!, fat: latest.fat! } as MacroBreakdown
          : null,
        // Include food servings for the food log
        foodServings: data.foodServings ?? [],
      };
    },
  });
};

/**
 * Hook for Cronometer data sync
 */
export const useCronometerSync = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: syncCronometerData,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: foodKeys.all });
    },
  });
};
