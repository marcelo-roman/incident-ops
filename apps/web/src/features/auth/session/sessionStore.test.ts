import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { KeyValueStorage } from '../../../shared/lib/storage';
import { serializeSession, type Session } from '../domain/session';
import { createSessionStore } from './sessionStore';

const start = Date.parse('2026-10-02T12:00:00Z');
const session: Session = { username: 'demo', accessToken: 'jwt', expiresAt: start + 60_000 };

function memoryStorage(initial: Record<string, string> = {}): KeyValueStorage & { values: Map<string, string> } {
  const values = new Map(Object.entries(initial));
  return {
    values,
    read: (key) => values.get(key) ?? null,
    write: (key, value) => values.set(key, value),
    remove: (key) => values.delete(key),
  };
}

describe('createSessionStore', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(start);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  function storeWith(storage: KeyValueStorage) {
    return createSessionStore({ key: 'session', storage, now: () => Date.now() });
  }

  it('keeps the session in memory and in storage and notifies subscribers', () => {
    const storage = memoryStorage();
    const store = storeWith(storage);
    const listener = vi.fn();
    const unsubscribe = store.subscribe(listener);

    store.signIn(session);

    expect(store.read()).toEqual(session);
    expect(storage.values.get('session')).toBe(serializeSession(session));
    expect(listener).toHaveBeenCalledTimes(1);

    unsubscribe();
    store.signOut();

    expect(store.read()).toBeNull();
    expect(storage.values.has('session')).toBe(false);
    expect(listener).toHaveBeenCalledTimes(1);
  });

  it('restores an active session from storage', () => {
    expect(storeWith(memoryStorage({ session: serializeSession(session) })).read()).toEqual(session);
  });

  it('drops an expired session found in storage', () => {
    const storage = memoryStorage({ session: serializeSession({ ...session, expiresAt: start - 1 }) });

    expect(storeWith(storage).read()).toBeNull();
  });

  it('signs out when the session expires', () => {
    const store = storeWith(memoryStorage());
    const listener = vi.fn();
    store.subscribe(listener);
    store.signIn(session);

    vi.advanceTimersByTime(59_999);
    expect(store.read()).toEqual(session);

    vi.advanceTimersByTime(1);
    expect(store.read()).toBeNull();
    expect(listener).toHaveBeenCalledTimes(2);
  });

  it('does not notify when signing out without a session', () => {
    const store = storeWith(memoryStorage());
    const listener = vi.fn();
    store.subscribe(listener);

    store.signOut();

    expect(listener).not.toHaveBeenCalled();
  });
});
