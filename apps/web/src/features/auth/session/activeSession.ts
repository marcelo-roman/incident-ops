import type { HttpAuthentication } from '../../../shared/http/authentication';
import { tabStorage } from '../../../shared/lib/storage';
import { isActive } from '../domain/session';
import { createSessionStore } from './sessionStore';

export const sessionStorageKey = 'incident-ops.session';

export const sessionStore = createSessionStore({ key: sessionStorageKey, storage: tabStorage, now: () => Date.now() });

export function currentAccessToken(): string {
  const session = sessionStore.read();
  if (!isActive(session, Date.now())) {
    return '';
  }
  return session.accessToken;
}

export const sessionHttpAuthentication: HttpAuthentication = {
  accessToken: () => currentAccessToken(),
  onUnauthorized: () => {
    sessionStore.signOut();
  },
};
