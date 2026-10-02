import type { HttpAuthentication } from '../shared/http/authentication';

export const mockCredentials = { username: 'demo', password: 'local-demo-password' } as const;
export const mockAccessToken = 'mock-access-token';
export const mockTokenLifetimeMs = 8 * 60 * 60 * 1000;

export const mockAuthentication: HttpAuthentication = {
  accessToken: () => mockAccessToken,
  onUnauthorized: () => undefined,
};

export function hasMockBearer(request: Request): boolean {
  return request.headers.get('authorization') === `Bearer ${mockAccessToken}`;
}

export function acceptsMockCredentials(credentials: { username?: unknown; password?: unknown }): boolean {
  return credentials.username === mockCredentials.username && credentials.password === mockCredentials.password;
}
