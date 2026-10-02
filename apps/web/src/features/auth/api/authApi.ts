import { anonymousIncidentsHttpClient } from '../../../shared/http/clients';
import type { HttpClient } from '../../../shared/http/httpClient';
import type { LoginValues } from '../domain/loginSchema';
import type { TokenResponse } from '../domain/session';

export interface AuthApi {
  token: (credentials: LoginValues) => Promise<TokenResponse>;
}

export function createAuthApi(http: HttpClient): AuthApi {
  return {
    token: (credentials) => http.post<TokenResponse>('/api/auth/token', credentials),
  };
}

export const authApi = createAuthApi(anonymousIncidentsHttpClient);
