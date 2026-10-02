import type { HttpClient } from '../../../shared/http/httpClient';
import type { Service } from '../domain/service';

export interface ServicesApi {
  list: () => Promise<Service[]>;
}

export function createServicesApi(http: HttpClient): ServicesApi {
  return {
    list: () => http.get<Service[]>('/api/services'),
  };
}
