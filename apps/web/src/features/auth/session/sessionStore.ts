import type { KeyValueStorage } from '../../../shared/lib/storage';
import { expiryDelayMs, restoreSession, serializeSession, type Session } from '../domain/session';

export interface SessionStore {
  read: () => Session | null;
  signIn: (session: Session) => void;
  signOut: () => void;
  subscribe: (listener: () => void) => () => void;
}

interface SessionStoreDependencies {
  key: string;
  storage: KeyValueStorage;
  now: () => number;
}

export function createSessionStore({ key, storage, now }: SessionStoreDependencies): SessionStore {
  const listeners = new Set<() => void>();
  let current: Session | null = null;
  let expiryTimer: ReturnType<typeof setTimeout> | undefined;

  function publish(next: Session | null): void {
    current = next;
    clearTimeout(expiryTimer);
    expiryTimer = undefined;
    if (next !== null) {
      expiryTimer = setTimeout(signOut, expiryDelayMs(next, now()));
    }
    listeners.forEach((listener) => {
      listener();
    });
  }

  function signOut(): void {
    storage.remove(key);
    if (current === null) {
      return;
    }
    publish(null);
  }

  function signIn(session: Session): void {
    storage.write(key, serializeSession(session));
    publish(session);
  }

  publish(restoreSession(storage.read(key), now()));

  return {
    read: () => current,
    signIn,
    signOut,
    subscribe: (listener) => {
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },
  };
}
