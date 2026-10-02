import { useSyncExternalStore } from 'react';
import type { Session } from '../domain/session';
import { sessionStore } from '../session/activeSession';

export function useSession(): Session | null {
  return useSyncExternalStore(sessionStore.subscribe, sessionStore.read);
}
