import { incidentsHttpClient } from '../../../shared/http/clients';
import type { HttpClient } from '../../../shared/http/httpClient';
import type { OnCallRoster } from '../domain/oncall';

export interface OnCallApi {
  current: () => Promise<OnCallRoster>;
}

export function createOnCallApi(http: HttpClient): OnCallApi {
  return {
    current: () => http.get<OnCallRoster>('/api/oncall/current'),
  };
}

export const onCallApi = createOnCallApi(incidentsHttpClient);

export const onCallKeys = {
  current: ['oncall', 'current'] as const,
};
