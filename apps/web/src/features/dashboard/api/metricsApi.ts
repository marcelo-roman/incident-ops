import { incidentKeys } from '../../incidents';
import { incidentsHttpClient } from '../../../shared/http/clients';
import type { HttpClient } from '../../../shared/http/httpClient';
import type { MetricsSummary } from '../domain/metrics';

export interface MetricsApi {
  summary: () => Promise<MetricsSummary>;
}

export function createMetricsApi(http: HttpClient): MetricsApi {
  return {
    summary: () => http.get<MetricsSummary>('/api/metrics/summary'),
  };
}

export const metricsApi = createMetricsApi(incidentsHttpClient);

export const metricsKeys = {
  summary: [...incidentKeys.derived(), 'metrics-summary'] as const,
};
