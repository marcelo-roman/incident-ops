import { getConfig } from '../config/appConfig';
import { createHttpClient } from './httpClient';

export const incidentsHttpClient = createHttpClient(() => getConfig().apiBaseUrl);
export const insightsHttpClient = createHttpClient(() => getConfig().insightsBaseUrl);
