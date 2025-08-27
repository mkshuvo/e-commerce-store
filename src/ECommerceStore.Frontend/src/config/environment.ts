/**
 * Environment configuration for the E-Commerce Store Frontend
 * Handles Aspire service discovery and environment variable validation
 */

// Environment variable names
export const ENV_VARS = {
  // API Configuration
  API_URL: 'NEXT_PUBLIC_API_URL',
  API_GATEWAY_URL: 'NEXT_PUBLIC_API_GATEWAY_URL',
  
  // Node Environment
  NODE_ENV: 'NODE_ENV',
  
  // Aspire Service Discovery
  ASPIRE_ENDPOINT: 'ASPIRE_ENDPOINT',
  ASPIRE_SERVICE_NAME: 'ASPIRE_SERVICE_NAME',
  
  // Application Configuration
  APP_NAME: 'NEXT_PUBLIC_APP_NAME',
  APP_VERSION: 'NEXT_PUBLIC_APP_VERSION',
  
  // Feature Flags
  ENABLE_DEBUG: 'NEXT_PUBLIC_ENABLE_DEBUG',
  ENABLE_ANALYTICS: 'NEXT_PUBLIC_ENABLE_ANALYTICS',
  
  // Security
  JWT_SECRET: 'JWT_SECRET',
  ENCRYPTION_KEY: 'ENCRYPTION_KEY',
} as const;

// Environment variable types
export interface EnvironmentConfig {
  // API Configuration
  apiUrl: string;
  apiGatewayUrl?: string;
  
  // Node Environment
  nodeEnv: 'development' | 'production' | 'test';
  
  // Aspire Configuration
  aspireEndpoint?: string;
  aspireServiceName?: string;
  
  // Application Configuration
  appName: string;
  appVersion: string;
  
  // Feature Flags
  enableDebug: boolean;
  enableAnalytics: boolean;
  
  // Security (server-side only)
  jwtSecret?: string;
  encryptionKey?: string;
}

// Default values
const DEFAULT_CONFIG: Partial<EnvironmentConfig> = {
  apiUrl: 'http://localhost:5000',
  nodeEnv: 'development',
  appName: 'E-Commerce Store',
  appVersion: '1.0.0',
  enableDebug: false,
  enableAnalytics: false,
};

// Required environment variables for different environments
const REQUIRED_VARS = {
  development: [ENV_VARS.API_URL, ENV_VARS.NODE_ENV],
  production: [
    ENV_VARS.API_URL,
    ENV_VARS.NODE_ENV,
    ENV_VARS.APP_NAME,
    ENV_VARS.APP_VERSION,
  ],
  test: [ENV_VARS.API_URL, ENV_VARS.NODE_ENV],
} as const;

/**
 * Get environment variable value with type safety
 */
function getEnvVar(name: string, defaultValue?: string): string | undefined {
  // Client-side: use process.env for NEXT_PUBLIC_ variables
  if (typeof window !== 'undefined') {
    return process.env[name] || defaultValue;
  }
  
  // Server-side: use process.env for all variables
  return process.env[name] || defaultValue;
}

/**
 * Parse boolean environment variable
 */
function parseBooleanEnv(value: string | undefined, defaultValue: boolean = false): boolean {
  if (!value) return defaultValue;
  return value.toLowerCase() === 'true' || value === '1';
}

/**
 * Validate required environment variables
 */
function validateEnvironment(env: string): string[] {
  const required = REQUIRED_VARS[env as keyof typeof REQUIRED_VARS] || REQUIRED_VARS.development;
  const missing: string[] = [];
  
  for (const varName of required) {
    const value = getEnvVar(varName);
    if (!value) {
      missing.push(varName);
    }
  }
  
  return missing;
}

/**
 * Load and validate environment configuration
 */
export function loadEnvironmentConfig(): EnvironmentConfig {
  const nodeEnv = (getEnvVar(ENV_VARS.NODE_ENV, 'development') as EnvironmentConfig['nodeEnv']);
  
  // Validate required variables
  const missingVars = validateEnvironment(nodeEnv);
  if (missingVars.length > 0) {
    const errorMessage = `Missing required environment variables: ${missingVars.join(', ')}`;
    console.error(errorMessage);
    
    // In development, warn but continue with defaults
    if (nodeEnv === 'development') {
      console.warn('Using default values for missing environment variables in development mode');
    } else {
      throw new Error(errorMessage);
    }
  }
  
  const config: EnvironmentConfig = {
    // API Configuration
    apiUrl: getEnvVar(ENV_VARS.API_URL) || DEFAULT_CONFIG.apiUrl!,
    apiGatewayUrl: getEnvVar(ENV_VARS.API_GATEWAY_URL),
    
    // Node Environment
    nodeEnv,
    
    // Aspire Configuration
    aspireEndpoint: getEnvVar(ENV_VARS.ASPIRE_ENDPOINT),
    aspireServiceName: getEnvVar(ENV_VARS.ASPIRE_SERVICE_NAME),
    
    // Application Configuration
    appName: getEnvVar(ENV_VARS.APP_NAME) || DEFAULT_CONFIG.appName!,
    appVersion: getEnvVar(ENV_VARS.APP_VERSION) || DEFAULT_CONFIG.appVersion!,
    
    // Feature Flags
    enableDebug: parseBooleanEnv(getEnvVar(ENV_VARS.ENABLE_DEBUG), DEFAULT_CONFIG.enableDebug!),
    enableAnalytics: parseBooleanEnv(getEnvVar(ENV_VARS.ENABLE_ANALYTICS), DEFAULT_CONFIG.enableAnalytics!),
    
    // Security (server-side only)
    jwtSecret: getEnvVar(ENV_VARS.JWT_SECRET),
    encryptionKey: getEnvVar(ENV_VARS.ENCRYPTION_KEY),
  };
  
  // Log configuration in development (excluding sensitive data)
  if (config.enableDebug && nodeEnv === 'development') {
    console.log('Environment Configuration:', {
      ...config,
      jwtSecret: config.jwtSecret ? '[REDACTED]' : undefined,
      encryptionKey: config.encryptionKey ? '[REDACTED]' : undefined,
    });
  }
  
  return config;
}

/**
 * Get current environment configuration
 */
export const env = loadEnvironmentConfig();

/**
 * Check if running in development mode
 */
export const isDevelopment = env.nodeEnv === 'development';

/**
 * Check if running in production mode
 */
export const isProduction = env.nodeEnv === 'production';

/**
 * Check if running in test mode
 */
export const isTest = env.nodeEnv === 'test';

/**
 * Check if running on client side
 */
export const isClient = typeof window !== 'undefined';

/**
 * Check if running on server side
 */
export const isServer = typeof window === 'undefined';

/**
 * Environment validation utility for runtime checks
 */
export const envUtils = {
  /**
   * Validate that all required environment variables are present
   */
  validate: () => {
    const missing = validateEnvironment(env.nodeEnv);
    return {
      isValid: missing.length === 0,
      missingVars: missing,
      config: env,
    };
  },
  
  /**
   * Get environment variable with fallback
   */
  get: (name: string, fallback?: string) => getEnvVar(name, fallback),
  
  /**
   * Check if environment variable exists
   */
  has: (name: string) => !!getEnvVar(name),
  
  /**
   * Get all environment variables (client-safe)
   */
  getAll: () => {
    const clientSafeEnv: Record<string, string> = {};
    
    // Only include NEXT_PUBLIC_ variables on client side
    Object.entries(process.env).forEach(([key, value]) => {
      if (isClient && key.startsWith('NEXT_PUBLIC_')) {
        clientSafeEnv[key] = value || '';
      } else if (isServer) {
        clientSafeEnv[key] = value || '';
      }
    });
    
    return clientSafeEnv;
  },
};