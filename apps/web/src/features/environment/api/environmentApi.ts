import { getConfig } from '../../../shared/config/appConfig';
import { ApiError } from '../../../shared/http/problem';

export const probeTimeoutMs = 10_000;

export interface EnvironmentApi {
  probe: () => Promise<number>;
}

export function createEnvironmentApi(
  baseUrl: () => string,
  fetchImpl: typeof fetch = (...args) => fetch(...args),
): EnvironmentApi {
  return {
    async probe() {
      const response = await fetchImpl(`${baseUrl()}/health/ready`, {
        cache: 'no-store',
        signal: AbortSignal.timeout(probeTimeoutMs),
      });
      if (!response.ok) {
        throw new ApiError(response.status, null);
      }
      return response.status;
    },
  };
}

export const environmentApi = createEnvironmentApi(() => getConfig().apiBaseUrl);

export const environmentKeys = {
  probe: ['environment', 'probe'] as const,
};
