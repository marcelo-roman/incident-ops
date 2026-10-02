import { getConfig } from '../config/appConfig';
import { getHttpAuthentication } from './authentication';
import { createHttpClient } from './httpClient';

export const incidentsHttpClient = createHttpClient(() => getConfig().apiBaseUrl, getHttpAuthentication);
export const insightsHttpClient = createHttpClient(() => getConfig().insightsBaseUrl, getHttpAuthentication);
export const anonymousIncidentsHttpClient = createHttpClient(() => getConfig().apiBaseUrl);
