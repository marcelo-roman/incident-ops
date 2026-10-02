import { insightsHttpClient } from '../../../shared/http/clients';
import type { HttpClient } from '../../../shared/http/httpClient';
import type { RcaDraft } from '../domain/rcaDraft';

export interface RcaApi {
  draft: (incidentId: string) => Promise<RcaDraft>;
}

export function createRcaApi(http: HttpClient): RcaApi {
  return {
    draft: (incidentId) => http.post<RcaDraft>('/api/rca/draft', { incidentId }),
  };
}

export const rcaApi = createRcaApi(insightsHttpClient);
