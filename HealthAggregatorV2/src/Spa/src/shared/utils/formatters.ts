/**
 * Number formatting utilities
 */

/**
 * Format a number with specified decimal places
 * @param value Number to format, or null
 * @param decimals Number of decimal places
 * @returns Formatted string or '--' for null values
 */
export function formatNumber(value: number | null | undefined, decimals = 0): string {
  if (value === null || value === undefined) return '--';
  return value.toLocaleString(undefined, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  });
}

/**
 * Format a percentage value
 * @param value Percentage value
 * @param decimals Number of decimal places (default 1)
 */
export function formatPercentage(value: number | null | undefined, decimals = 1): string {
  if (value === null || value === undefined) return '--';
  return `${value.toFixed(decimals)}%`;
}

/**
 * Format weight in kilograms
 * @param kg Weight value in kg
 * @param decimals Number of decimal places
 */
export function formatWeight(kg: number | null | undefined, decimals = 1): string {
  if (kg === null || kg === undefined) return '--';
  return `${kg.toFixed(decimals)} kg`;
}

/**
 * Format calories
 * @param value Calorie value
 */
export function formatCalories(value: number | null | undefined): string {
  if (value === null || value === undefined) return '--';
  return `${Math.round(value)} kcal`;
}

/**
 * Format grams (for macros)
 * @param value Value in grams
 * @param decimals Number of decimal places
 */
export function formatGrams(value: number | null | undefined, decimals = 0): string {
  if (value === null || value === undefined) return '--';
  return `${value.toFixed(decimals)}g`;
}

/**
 * Format a score (0-100)
 * @param value Score value
 */
export function formatScore(value: number | null | undefined): string {
  if (value === null || value === undefined) return '--';
  return Math.round(value).toString();
}

/**
 * Format steps with thousands separator
 * @param value Steps count
 */
export function formatSteps(value: number | null | undefined): string {
  if (value === null || value === undefined) return '--';
  return value.toLocaleString();
}

/**
 * Calculate and format change between two values
 * @param current Current value
 * @param previous Previous value
 * @param decimals Decimal places
 * @returns Object with formatted change and type
 */
export function formatChange(
  current: number | null | undefined,
  previous: number | null | undefined,
  decimals = 1
): { value: string; type: 'positive' | 'negative' | 'neutral' } {
  if (current === null || current === undefined || 
      previous === null || previous === undefined) {
    return { value: '--', type: 'neutral' };
  }

  const diff = current - previous;
  const sign = diff > 0 ? '+' : '';
  const type = diff > 0 ? 'positive' : diff < 0 ? 'negative' : 'neutral';
  
  return {
    value: `${sign}${diff.toFixed(decimals)}`,
    type,
  };
}

/**
 * Calculate percentage change
 * @param current Current value
 * @param previous Previous value
 */
export function formatPercentageChange(
  current: number | null | undefined,
  previous: number | null | undefined
): { value: string; type: 'positive' | 'negative' | 'neutral' } {
  if (current === null || current === undefined || 
      previous === null || previous === undefined || 
      previous === 0) {
    return { value: '--', type: 'neutral' };
  }

  const percentChange = ((current - previous) / previous) * 100;
  const sign = percentChange > 0 ? '+' : '';
  const type = percentChange > 0 ? 'positive' : percentChange < 0 ? 'negative' : 'neutral';
  
  return {
    value: `${sign}${percentChange.toFixed(1)}%`,
    type,
  };
}
