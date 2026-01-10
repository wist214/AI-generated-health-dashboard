import axios, { AxiosError } from 'axios';
import type { ApiError } from './types';

/**
 * Base URL for Azure Functions (sync operations)
 * Uses environment variable or defaults to localhost:7071
 */
const FUNCTIONS_BASE_URL = import.meta.env.VITE_FUNCTIONS_BASE_URL || 'http://localhost:7071';

/**
 * Axios instance configured for Azure Functions sync endpoints
 */
export const syncClient = axios.create({
  baseURL: FUNCTIONS_BASE_URL,
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
 * Uses the V2 Functions endpoint: POST /api/sync/{sourceName}
 */
export async function syncOura(): Promise<SyncResponse> {
  const response = await syncClient.post<SyncResponse>('/api/sync/oura');
  return response.data;
}

/**
 * Trigger Picooc data sync
 * Uses the V2 Functions endpoint: POST /api/sync/{sourceName}
 */
export async function syncPicooc(): Promise<SyncResponse> {
  const response = await syncClient.post<SyncResponse>('/api/sync/picooc');
  return response.data;
}

/**
 * Trigger Cronometer data sync
 * Uses the V2 Functions endpoint: POST /api/sync/{sourceName}
 */
export async function syncCronometer(): Promise<SyncResponse> {
  const response = await syncClient.post<SyncResponse>('/api/sync/cronometer');
  return response.data;
}

/**
 * Trigger sync for all sources
 */
export async function syncAll(): Promise<SyncResponse> {
  const response = await syncClient.post<SyncResponse>('/api/sync');
  return response.data;
}
