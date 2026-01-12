import axios, { AxiosError } from 'axios';
import type { ApiError } from './types';

/**
 * Base URL for Azure Functions (sync operations)
 * Uses same base URL as the main API client
 */
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

/**
 * Axios instance configured for Azure Functions sync endpoints
 */
export const syncClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 120000, // 2 minutes - sync operations can take a while
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Response interceptor for error handling
 */
syncClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiError>) => {
    // Log errors in development
    if (import.meta.env.DEV) {
      console.error('Sync API Error:', {
        url: error.config?.url,
        status: error.response?.status,
        message: error.response?.data?.message || error.message,
      });
    }
    return Promise.reject(error);
  }
);

/**
 * Sync response type
 */
export interface SyncResponse {
  success: boolean;
  message: string;
  [key: string]: unknown; // Additional fields depending on the source
}

/**
 * Trigger Oura data sync
 * Route: POST /api/oura/sync
 */
export async function syncOura(): Promise<SyncResponse> {
  const response = await syncClient.post<SyncResponse>('/api/oura/sync');
  return response.data;
}

/**
 * Trigger Picooc data sync
 * Route: POST /api/picooc/sync
 */
export async function syncPicooc(): Promise<SyncResponse> {
  const response = await syncClient.post<SyncResponse>('/api/picooc/sync');
  return response.data;
}

/**
 * Trigger Cronometer data sync
 * Route: POST /api/cronometer/sync
 */
export async function syncCronometer(): Promise<SyncResponse> {
  const response = await syncClient.post<SyncResponse>('/api/cronometer/sync');
  return response.data;
}

/**
 * Trigger sync for all sources
 */
export async function syncAll(): Promise<SyncResponse> {
  // Sync all sources in parallel
  const results = await Promise.allSettled([
    syncOura(),
    syncPicooc(),
    syncCronometer(),
  ]);
  
  const succeeded = results.filter(r => r.status === 'fulfilled').length;
  const failed = results.filter(r => r.status === 'rejected').length;
  
  return {
    success: failed === 0,
    message: `Sync completed: ${succeeded} succeeded, ${failed} failed`,
  };
}
