import { incidentsHttpClient } from '../../../shared/http/clients';
import { createIncidentsApi } from './incidentsApi';
import { createServicesApi } from './servicesApi';

export const incidentsApi = createIncidentsApi(incidentsHttpClient);
export const servicesApi = createServicesApi(incidentsHttpClient);
