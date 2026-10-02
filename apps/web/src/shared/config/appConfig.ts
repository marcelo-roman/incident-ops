import { configFromEnvironment, type AppConfig } from './environment';

export type { AppConfig } from './environment';
export { withoutTrailingSlash } from './environment';

export const buildTimeConfig: AppConfig = configFromEnvironment(import.meta.env);

let activeConfig: AppConfig = buildTimeConfig;

export function getConfig(): AppConfig {
  return activeConfig;
}

export function setConfig(config: AppConfig): void {
  activeConfig = config;
}
