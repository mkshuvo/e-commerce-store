'use client';

import { useEffect, useState, useCallback } from 'react';
import { apiClient, serviceDiscovery } from '@/lib/api-client';

export interface ApiState {
  isConnected: boolean;
  isLoading: boolean;
  error: string | null;
  baseUrl: string;
}

export interface UseApiClientReturn extends ApiState {
  client: typeof apiClient;
  refreshConnection: () => Promise<void>;
  updateEndpoint: (url: string) => void;
}

/**
 * Custom hook for API client management with Aspire integration
 */
export function useApiClient(): UseApiClientReturn {
  const [state, setState] = useState<ApiState>({
    isConnected: false,
    isLoading: true,
    error: null,
    baseUrl: apiClient.getBaseUrl(),
  });

  /**
   * Check API connectivity and update state
   */
  const checkConnection = useCallback(async () => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));
    
    try {
      const isValid = await serviceDiscovery.validateAndUpdateEndpoint();
      
      setState({
        isConnected: isValid,
        isLoading: false,
        error: isValid ? null : 'API service is unavailable',
        baseUrl: apiClient.getBaseUrl(),
      });
    } catch (error) {
      setState({
        isConnected: false,
        isLoading: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred',
        baseUrl: apiClient.getBaseUrl(),
      });
    }
  }, []);

  /**
   * Refresh API connection
   */
  const refreshConnection = useCallback(async () => {
    await checkConnection();
  }, [checkConnection]);

  /**
   * Update API endpoint manually
   */
  const updateEndpoint = useCallback((url: string) => {
    apiClient.updateBaseUrl(url);
    setState(prev => ({ ...prev, baseUrl: url }));
    // Automatically check connection with new endpoint
    checkConnection();
  }, [checkConnection]);

  // Initialize connection check on mount
  useEffect(() => {
    checkConnection();
  }, [checkConnection]);

  // Periodic health check (every 5 minutes)
  useEffect(() => {
    const interval = setInterval(() => {
      if (state.isConnected) {
        checkConnection();
      }
    }, 5 * 60 * 1000); // 5 minutes

    return () => clearInterval(interval);
  }, [state.isConnected, checkConnection]);

  return {
    ...state,
    client: apiClient,
    refreshConnection,
    updateEndpoint,
  };
}

/**
 * Hook for making API requests with automatic error handling
 */
export function useApiRequest<T>() {
  const { client, isConnected } = useApiClient();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const execute = useCallback(async (
    requestFn: (client: typeof apiClient) => Promise<T>
  ): Promise<T | null> => {
    if (!isConnected) {
      setError('API service is not available');
      return null;
    }

    setLoading(true);
    setError(null);

    try {
      const result = await requestFn(client);
      return result;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Request failed';
      setError(errorMessage);
      console.error('API request failed:', err);
      return null;
    } finally {
      setLoading(false);
    }
  }, [client, isConnected]);

  return {
    execute,
    loading,
    error,
    isConnected,
  };
}

// Authentication response types
interface AuthResponse {
  token: string;
  user: {
    id: string;
    email: string;
    name: string;
  };
}

/**
 * Hook for authentication-related API calls
 */
export function useAuthApi() {
  const { execute, loading, error } = useApiRequest<AuthResponse>();

  const login = useCallback(async (credentials: { email: string; password: string }) => {
    return execute(client => client.post('/auth/login', credentials));
  }, [execute]);

  const register = useCallback(async (userData: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
  }) => {
    return execute(client => client.post('/auth/register', userData));
  }, [execute]);

  const logout = useCallback(async () => {
    return execute(client => client.post('/auth/logout'));
  }, [execute]);

  const refreshToken = useCallback(async () => {
    return execute(client => client.post('/auth/refresh'));
  }, [execute]);

  const getProfile = useCallback(async () => {
    return execute(client => client.get('/auth/profile'));
  }, [execute]);

  return {
    login,
    register,
    logout,
    refreshToken,
    getProfile,
    loading,
    error,
  };
}