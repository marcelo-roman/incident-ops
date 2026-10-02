import { describe, expect, it, vi } from 'vitest';
import {
  configFromEnvironment,
  productionApiBaseUrl,
  productionInsightsBaseUrl,
  runtimeConfigDocument,
  type AppConfig,
} from './environment';
import { loadRuntimeConfig, mergeRuntimeConfig } from './runtimeConfig';

const defaults: AppConfig = {
  apiBaseUrl: 'https://api.example.com',
  insightsBaseUrl: 'https://insights.example.com',
  useMocks: false,
};

function jsonResponse(body: unknown, contentType = 'application/json'): Response {
  return new Response(JSON.stringify(body), { status: 200, headers: { 'content-type': contentType } });
}

describe('configFromEnvironment', () => {
  it('falls back to the production endpoints', () => {
    expect(configFromEnvironment({})).toEqual({
      apiBaseUrl: productionApiBaseUrl,
      insightsBaseUrl: productionInsightsBaseUrl,
      useMocks: false,
    });
  });

  it('reads build-time variables and trims trailing slashes', () => {
    expect(
      configFromEnvironment({
        VITE_API_BASE_URL: 'http://localhost:5080/',
        VITE_INSIGHTS_BASE_URL: ' http://localhost:8000 ',
        VITE_USE_MOCKS: 'true',
      }),
    ).toEqual({ apiBaseUrl: 'http://localhost:5080', insightsBaseUrl: 'http://localhost:8000', useMocks: true });
  });
});

describe('runtimeConfigDocument', () => {
  it('serialises the endpoints the build was configured with', () => {
    expect(JSON.parse(runtimeConfigDocument({ VITE_API_BASE_URL: 'http://localhost:5080' }))).toEqual({
      apiBaseUrl: 'http://localhost:5080',
      insightsBaseUrl: productionInsightsBaseUrl,
    });
  });
});

describe('mergeRuntimeConfig', () => {
  it('overrides only the endpoints present in config.json', () => {
    expect(mergeRuntimeConfig(defaults, { apiBaseUrl: 'http://localhost:5080/' })).toEqual({
      ...defaults,
      apiBaseUrl: 'http://localhost:5080',
    });
  });

  it('ignores a payload that is not a valid configuration', () => {
    expect(mergeRuntimeConfig(defaults, { apiBaseUrl: 'not a url' })).toBe(defaults);
    expect(mergeRuntimeConfig(defaults, '<!doctype html>')).toBe(defaults);
  });
});

describe('loadRuntimeConfig', () => {
  it('applies config.json when it is served', async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(jsonResponse({ insightsBaseUrl: 'http://localhost:8000' })));

    await expect(loadRuntimeConfig(defaults, fetchImpl)).resolves.toMatchObject({
      insightsBaseUrl: 'http://localhost:8000',
    });
    expect(fetchImpl).toHaveBeenCalledWith('/config.json', expect.objectContaining({ cache: 'no-store' }));
  });

  it('keeps the defaults when the host answers with the SPA fallback page', async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(jsonResponse('<html>', 'text/html')));

    await expect(loadRuntimeConfig(defaults, fetchImpl)).resolves.toBe(defaults);
  });

  it('keeps the defaults when config.json is missing or unreachable', async () => {
    const missing = vi.fn(() => Promise.resolve(new Response(null, { status: 404 })));
    const offline = vi.fn(() => Promise.reject(new TypeError('Failed to fetch')));

    await expect(loadRuntimeConfig(defaults, missing)).resolves.toBe(defaults);
    await expect(loadRuntimeConfig(defaults, offline)).resolves.toBe(defaults);
  });
});
