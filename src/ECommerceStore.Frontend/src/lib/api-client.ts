// API Client Configuration for Aspire Integration
export class ApiClient {
  private baseUrl: string;
  private timeout: number;
  private retryAttempts: number;

  constructor() {
    this.baseUrl = this.resolveApiUrl();
    this.timeout = 30000; // 30 seconds
    this.retryAttempts = 3;
  }

  /**
   * Resolve API URL from environment variables
   */
  private resolveApiUrl(): string {
    // Priority order: environment variable > fallback based on current location
    return (
      process.env.NEXT_PUBLIC_API_URL ||
      (typeof window !== 'undefined' && window.location.origin.includes('localhost')
        ? 'http://localhost:5000'
        : 'https://api.ecommercestore.com')
    );
  }

  /**
   * Get the current API base URL
   */
  public getBaseUrl(): string {
    return this.baseUrl;
  }

  /**
   * Update the API base URL (useful for dynamic service discovery)
   */
  public updateBaseUrl(newUrl: string): void {
    this.baseUrl = newUrl;
  }

  /**
   * Generic HTTP request method with retry logic
   */
  private async request<T>(
    endpoint: string,
    options: RequestInit = {},
    attempt: number = 1
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    
    const defaultHeaders = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    const config: RequestInit = {
      ...options,
      headers: {
        ...defaultHeaders,
        ...options.headers,
      },
    };

    // Add timeout using AbortController
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);
    config.signal = controller.signal;

    try {
      const response = await fetch(url, config);
      clearTimeout(timeoutId);

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        return await response.json();
      }
      
      return await response.text() as unknown as T;
    } catch (error) {
      clearTimeout(timeoutId);
      
      // Retry logic for network errors
      if (attempt < this.retryAttempts && this.isRetryableError(error)) {
        console.warn(`API request failed (attempt ${attempt}/${this.retryAttempts}):`, error);
        await this.delay(1000 * attempt); // Exponential backoff
        return this.request<T>(endpoint, options, attempt + 1);
      }
      
      throw error;
    }
  }

  /**
   * Check if an error is retryable
   */
  private isRetryableError(error: unknown): boolean {
    if (error instanceof Error) {
      return (
        error.name === 'AbortError' ||
        error.name === 'TypeError' ||
        (error.message ? error.message.includes('fetch') : false)
      );
    }
    return false;
  }

  /**
   * Delay utility for retry logic
   */
  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * GET request
   */
  public async get<T>(endpoint: string, headers?: Record<string, string>): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'GET',
      headers,
    });
  }

  /**
   * POST request
   */
  public async post<T>(
    endpoint: string,
    data?: unknown,
    headers?: Record<string, string>
  ): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      headers,
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  /**
   * PUT request
   */
  public async put<T>(
    endpoint: string,
    data?: unknown,
    headers?: Record<string, string>
  ): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      headers,
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  /**
   * DELETE request
   */
  public async delete<T>(endpoint: string, headers?: Record<string, string>): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'DELETE',
      headers,
    });
  }

  /**
   * Health check for API connectivity
   */
  public async healthCheck(): Promise<{ status: string; timestamp: string }> {
    try {
      return await this.get<{ status: string; timestamp: string }>('/health');
    } catch (error) {
      console.error('API health check failed:', error);
      throw new Error('API service is unavailable');
    }
  }
}

// Singleton instance
export const apiClient = new ApiClient();

// Service discovery utilities
export const serviceDiscovery = {
  /**
   * Discover and update API endpoint from Aspire service discovery
   */
  async discoverApiEndpoint(): Promise<string> {
    try {
      // In Aspire, service discovery information might be available through environment variables
      // or a discovery endpoint. This is a placeholder for future implementation.
      const discoveredUrl = process.env.NEXT_PUBLIC_API_URL;
      
      if (discoveredUrl && discoveredUrl !== apiClient.getBaseUrl()) {
        apiClient.updateBaseUrl(discoveredUrl);
        console.log('API endpoint updated via service discovery:', discoveredUrl);
      }
      
      return apiClient.getBaseUrl();
    } catch (error) {
      console.warn('Service discovery failed, using configured endpoint:', error);
      return apiClient.getBaseUrl();
    }
  },

  /**
   * Validate API connectivity and update endpoint if needed
   */
  async validateAndUpdateEndpoint(): Promise<boolean> {
    try {
      await apiClient.healthCheck();
      return true;
    } catch (error) {
      console.warn('Current API endpoint failed health check:', error);
      
      // Try to discover a new endpoint
      await this.discoverApiEndpoint();
      
      try {
        await apiClient.healthCheck();
        return true;
      } catch (retryError) {
        console.error('API endpoint validation failed after discovery:', retryError);
        return false;
      }
    }
  },
};