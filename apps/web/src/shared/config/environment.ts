export interface AppConfig {
  apiBaseUrl: string;
  insightsBaseUrl: string;
  useMocks: boolean;
}

export interface BuildEnvironment {
  VITE_API_BASE_URL?: string | undefined;
  VITE_INSIGHTS_BASE_URL?: string | undefined;
  VITE_USE_MOCKS?: string | undefined;
}

export const productionApiBaseUrl = 'https://incidents-api.marceloroman.com.br';
export const productionInsightsBaseUrl = 'https://incidents-insights.marceloroman.com.br';

export function withoutTrailingSlash(url: string): string {
  let trimmed = url.trim();
  while (trimmed.endsWith('/')) {
    trimmed = trimmed.slice(0, -1);
  }
  return trimmed;
}

function urlOrDefault(value: string | undefined, fallback: string): string {
  if (value === undefined || value.trim() === '') {
    return fallback;
  }
  return withoutTrailingSlash(value);
}

export function configFromEnvironment(environment: BuildEnvironment): AppConfig {
  return {
    apiBaseUrl: urlOrDefault(environment.VITE_API_BASE_URL, productionApiBaseUrl),
    insightsBaseUrl: urlOrDefault(environment.VITE_INSIGHTS_BASE_URL, productionInsightsBaseUrl),
    useMocks: environment.VITE_USE_MOCKS === 'true',
  };
}

export function runtimeConfigDocument(environment: BuildEnvironment): string {
  const { apiBaseUrl, insightsBaseUrl } = configFromEnvironment(environment);
  return `${JSON.stringify({ apiBaseUrl, insightsBaseUrl }, null, 2)}\n`;
}
