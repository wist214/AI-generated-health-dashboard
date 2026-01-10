import { format, formatDistanceToNow, parseISO, isValid } from 'date-fns';

/**
 * Parse a date string safely
 * @param dateString ISO date string or Date object
 * @returns Date object or null if invalid
 */
export function parseDate(dateString: string | Date | null | undefined): Date | null {
  if (!dateString) return null;
  
  if (dateString instanceof Date) {
    return isValid(dateString) ? dateString : null;
  }
  
  try {
    const parsed = parseISO(dateString);
    return isValid(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

/**
 * Format a date as "Jan 1, 2024"
 */
export function formatDate(date: string | Date | null | undefined): string {
  const parsed = parseDate(date);
  if (!parsed) return '--';
  return format(parsed, 'MMM d, yyyy');
}

/**
 * Format a date as "Jan 1, 2024 14:30"
 */
export function formatDateTime(date: string | Date | null | undefined): string {
  const parsed = parseDate(date);
  if (!parsed) return '--';
  return format(parsed, 'MMM d, yyyy HH:mm');
}

/**
 * Format a date as "January 1, 2024"
 */
export function formatDateLong(date: string | Date | null | undefined): string {
  const parsed = parseDate(date);
  if (!parsed) return '--';
  return format(parsed, 'MMMM d, yyyy');
}

/**
 * Format a date as "Mon, Jan 1"
 */
export function formatDateShort(date: string | Date | null | undefined): string {
  const parsed = parseDate(date);
  if (!parsed) return '--';
  return format(parsed, 'EEE, MMM d');
}

/**
 * Format as relative time (e.g., "2 hours ago")
 */
export function formatRelativeTime(date: string | Date | null | undefined): string {
  const parsed = parseDate(date);
  if (!parsed) return '--';
  return formatDistanceToNow(parsed, { addSuffix: true });
}

/**
 * Format time only (e.g., "14:30")
 */
export function formatTime(date: string | Date | null | undefined): string {
  const parsed = parseDate(date);
  if (!parsed) return '--';
  return format(parsed, 'HH:mm');
}

/**
 * Format duration in minutes to "Xh Ym"
 */
export function formatDuration(minutes: number | null | undefined): string {
  if (minutes === null || minutes === undefined) return '--';
  
  const hours = Math.floor(minutes / 60);
  const mins = Math.round(minutes % 60);
  
  if (hours === 0) return `${mins}m`;
  if (mins === 0) return `${hours}h`;
  return `${hours}h ${mins}m`;
}

/**
 * Format duration in seconds to "Xh Ym Zs"
 */
export function formatDurationSeconds(seconds: number | null | undefined): string {
  if (seconds === null || seconds === undefined) return '--';
  
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = Math.round(seconds % 60);
  
  if (hours > 0) {
    return minutes > 0 ? `${hours}h ${minutes}m` : `${hours}h`;
  }
  if (minutes > 0) {
    return secs > 0 ? `${minutes}m ${secs}s` : `${minutes}m`;
  }
  return `${secs}s`;
}

/**
 * Get ISO date string (YYYY-MM-DD)
 */
export function toISODateString(date: Date): string {
  return format(date, 'yyyy-MM-dd');
}

/**
 * Get the start of today in ISO format
 */
export function todayISO(): string {
  return toISODateString(new Date());
}

/**
 * Get a date N days ago in ISO format
 */
export function daysAgoISO(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() - days);
  return toISODateString(date);
}
