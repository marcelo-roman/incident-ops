import { insightsHttpClient } from '../../../shared/http/clients';
import type { HttpClient } from '../../../shared/http/httpClient';
import type { AnomalyReport, KpiReport, RecurringReport } from '../domain/insights';

export interface InsightsApi {
  kpis: (days: number) => Promise<KpiReport>;
  recurring: (days: number) => Promise<RecurringReport>;
  anomalies: (days: number) => Promise<AnomalyReport>;
}

export function createInsightsApi(http: HttpClient): InsightsApi {
  return {
    kpis: (days) => http.get<KpiReport>('/api/kpis', { days }),
    recurring: (days) => http.get<RecurringReport>('/api/recurring', { days }),
    anomalies: (days) => http.get<AnomalyReport>('/api/anomalies', { days }),
  };
}

export const insightsApi = createInsightsApi(insightsHttpClient);

export const insightsKeys = {
  kpis: (days: number) => ['insights', 'kpis', days] as const,
  recurring: (days: number) => ['insights', 'recurring', days] as const,
  anomalies: (days: number) => ['insights', 'anomalies', days] as const,
};
