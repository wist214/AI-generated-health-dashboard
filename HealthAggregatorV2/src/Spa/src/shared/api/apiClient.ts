import axios, { AxiosError, AxiosRequestConfig, AxiosResponse } from 'axios';
import type { ApiError } from './types';

/**
 * Base URL for the API
 * Uses environment variable or defaults to localhost
 */
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

/**
 * Axios instance configured for the Health Aggregator API
 */
export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Request interceptor for adding auth tokens or logging
 */
apiClient.interceptors.request.use(
  (config) => {
    // Add auth token if available (for future use)
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

/**
 * Response interceptor for error handling
 */
apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  (error: AxiosError<ApiError>) => {
    // Handle specific error codes
    if (error.response?.status === 401) {
      // Handle unauthorized
      localStorage.removeItem('authToken');
      // Could redirect to login page if auth is implemented
    }

    // Log errors in development
    if (import.meta.env.DEV) {
      console.error('API Error:', {
        url: error.config?.url,
        status: error.response?.status,
        message: error.response?.data?.message || error.message,
      });
    }

    return Promise.reject(error);
  }
);

/**
 * Generic API request helper with type safety
 * @param config Axios request configuration
 * @returns Promise with typed response data
 */
export async function apiRequest<T>(config: AxiosRequestConfig): Promise<T> {
  try {
    const response = await apiClient.request<T>(config);
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error)) {
      const apiError: ApiError = {
        message: error.response?.data?.message || error.message,
        code: error.code,
        statusCode: error.response?.status,
        details: error.response?.data?.details,
      };
      throw apiError;
    }
    throw error;
  }
}

/**
 * GET request helper
 */
export async function get<T>(url: string, params?: Record<string, unknown>): Promise<T> {
  return apiRequest<T>({ method: 'GET', url, params });
}

/**
 * POST request helper
 */
export async function post<T>(url: string, data?: unknown): Promise<T> {
  return apiRequest<T>({ method: 'POST', url, data });
}

/**
 * PUT request helper
 */
export async function put<T>(url: string, data?: unknown): Promise<T> {
  return apiRequest<T>({ method: 'PUT', url, data });
}

/**
 * DELETE request helper
 */
export async function del<T>(url: string): Promise<T> {
  return apiRequest<T>({ method: 'DELETE', url });
}
