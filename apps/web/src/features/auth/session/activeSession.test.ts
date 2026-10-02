import { afterEach, describe, expect, it } from 'vitest';
import { currentAccessToken, sessionHttpAuthentication, sessionStorageKey, sessionStore } from './activeSession';

describe('active session', () => {
  afterEach(() => {
    sessionStore.signOut();
  });

  it('provides the token of the active session to HTTP and SignalR', () => {
    sessionStore.signIn({ username: 'demo', accessToken: 'jwt', expiresAt: Date.now() + 60_000 });

    expect(currentAccessToken()).toBe('jwt');
    expect(sessionHttpAuthentication.accessToken()).toBe('jwt');
    expect(window.sessionStorage.getItem(sessionStorageKey)).toContain('"jwt"');
  });

  it('provides no token once the session has expired', () => {
    sessionStore.signIn({ username: 'demo', accessToken: 'jwt', expiresAt: Date.now() - 1 });

    expect(currentAccessToken()).toBe('');
  });

  it('signs out when a request is unauthorized', () => {
    sessionStore.signIn({ username: 'demo', accessToken: 'jwt', expiresAt: Date.now() + 60_000 });

    sessionHttpAuthentication.onUnauthorized();

    expect(sessionStore.read()).toBeNull();
    expect(window.sessionStorage.getItem(sessionStorageKey)).toBeNull();
  });
});
