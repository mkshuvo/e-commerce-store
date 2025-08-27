'use client';

import React, { createContext, useContext, ReactNode } from 'react';
import { useApiClient, UseApiClientReturn } from '@/hooks/useApiClient';

// Create the API context
const ApiContext = createContext<UseApiClientReturn | undefined>(undefined);

// Provider component
export interface ApiProviderProps {
  children: ReactNode;
}

export function ApiProvider({ children }: ApiProviderProps) {
  const apiClientState = useApiClient();

  return (
    <ApiContext.Provider value={apiClientState}>
      {children}
    </ApiContext.Provider>
  );
}

// Hook to use the API context
export function useApi(): UseApiClientReturn {
  const context = useContext(ApiContext);
  
  if (context === undefined) {
    throw new Error('useApi must be used within an ApiProvider');
  }
  
  return context;
}

// Connection status component
export function ApiConnectionStatus() {
  const { isConnected, isLoading, error, baseUrl, refreshConnection } = useApi();

  if (isLoading) {
    return (
      <div className="flex items-center space-x-2 text-yellow-600">
        <div className="w-2 h-2 bg-yellow-500 rounded-full animate-pulse"></div>
        <span className="text-sm">Connecting to API...</span>
      </div>
    );
  }

  if (!isConnected) {
    return (
      <div className="flex items-center space-x-2 text-red-600">
        <div className="w-2 h-2 bg-red-500 rounded-full"></div>
        <span className="text-sm">API Disconnected</span>
        {error && (
          <span className="text-xs text-gray-500">({error})</span>
        )}
        <button
          onClick={refreshConnection}
          className="text-xs underline hover:no-underline"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div className="flex items-center space-x-2 text-green-600">
      <div className="w-2 h-2 bg-green-500 rounded-full"></div>
      <span className="text-sm">API Connected</span>
      <span className="text-xs text-gray-500">({new URL(baseUrl).host})</span>
    </div>
  );
}

// API configuration panel component
export function ApiConfigPanel() {
  const { baseUrl, updateEndpoint, refreshConnection } = useApi();
  const [newUrl, setNewUrl] = React.useState(baseUrl);
  const [isEditing, setIsEditing] = React.useState(false);

  const handleUpdateEndpoint = () => {
    if (newUrl && newUrl !== baseUrl) {
      updateEndpoint(newUrl);
    }
    setIsEditing(false);
  };

  const handleCancel = () => {
    setNewUrl(baseUrl);
    setIsEditing(false);
  };

  return (
    <div className="p-4 border rounded-lg bg-gray-50">
      <h3 className="text-lg font-semibold mb-3">API Configuration</h3>
      
      <div className="space-y-3">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            API Endpoint
          </label>
          {isEditing ? (
            <div className="flex space-x-2">
              <input
                type="url"
                value={newUrl}
                onChange={(e) => setNewUrl(e.target.value)}
                className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="https://api.example.com"
              />
              <button
                onClick={handleUpdateEndpoint}
                className="px-3 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
              >
                Save
              </button>
              <button
                onClick={handleCancel}
                className="px-3 py-2 bg-gray-300 text-gray-700 rounded-md hover:bg-gray-400"
              >
                Cancel
              </button>
            </div>
          ) : (
            <div className="flex items-center space-x-2">
              <code className="flex-1 px-3 py-2 bg-gray-100 border rounded-md text-sm">
                {baseUrl}
              </code>
              <button
                onClick={() => setIsEditing(true)}
                className="px-3 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700"
              >
                Edit
              </button>
            </div>
          )}
        </div>
        
        <div className="flex space-x-2">
          <button
            onClick={refreshConnection}
            className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700"
          >
            Test Connection
          </button>
        </div>
        
        <ApiConnectionStatus />
      </div>
    </div>
  );
}