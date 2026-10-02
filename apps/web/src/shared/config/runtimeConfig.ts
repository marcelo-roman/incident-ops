import { z } from 'zod';
import { withoutTrailingSlash, type AppConfig } from './appConfig';

export const runtimeConfigPath = '/config.json';

const runtimeConfigSchema = z.object({
  apiBaseUrl: z.url().optional(),
  insightsBaseUrl: z.url().optional(),
});

export function mergeRuntimeConfig(defaults: AppConfig, payload: unknown): AppConfig {
  const parsed = runtimeConfigSchema.safeParse(payload);
  if (!parsed.success) {
    return defaults;
  }
  const { apiBaseUrl, insightsBaseUrl } = parsed.data;
  return {
    ...defaults,
    apiBaseUrl: withoutTrailingSlash(apiBaseUrl ?? defaults.apiBaseUrl),
    insightsBaseUrl: withoutTrailingSlash(insightsBaseUrl ?? defaults.insightsBaseUrl),
  };
}

function isJsonResponse(response: Response): boolean {
  return response.ok && (response.headers.get('content-type') ?? '').includes('json');
}

export async function loadRuntimeConfig(
  defaults: AppConfig,
  fetchImpl: typeof fetch = (...args) => fetch(...args),
): Promise<AppConfig> {
  try {
    const response = await fetchImpl(runtimeConfigPath, { cache: 'no-store', headers: { accept: 'application/json' } });
    if (!isJsonResponse(response)) {
      return defaults;
    }
    return mergeRuntimeConfig(defaults, await response.json());
  } catch {
    return defaults;
  }
}
