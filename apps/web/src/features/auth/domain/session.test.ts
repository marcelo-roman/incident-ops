import { describe, expect, it } from 'vitest';
import {
  expiryDelayMs,
  isActive,
  maxTimerDelayMs,
  restoreSession,
  serializeSession,
  sessionFromToken,
  type Session,
  type TokenResponse,
} from './session';

const now = Date.parse('2026-10-02T12:00:00Z');
const token: TokenResponse = { accessToken: 'jwt', tokenType: 'Bearer', expiresAt: '2026-10-02T20:00:00Z' };
const session: Session = { username: 'demo', accessToken: 'jwt', expiresAt: Date.parse('2026-10-02T20:00:00Z') };

describe('sessionFromToken', () => {
  it('builds a session that lasts until the token expires', () => {
    expect(sessionFromToken('demo', token, now)).toEqual(session);
  });

  it('accepts the token type in any case', () => {
    expect(sessionFromToken('demo', { ...token, tokenType: 'bearer' }, now)).toEqual(session);
  });

  it.each([
    ['another token type', { ...token, tokenType: 'MAC' }],
    ['an empty token', { ...token, accessToken: '' }],
    ['an unreadable expiry', { ...token, expiresAt: 'tomorrow' }],
    ['an expiry in the past', { ...token, expiresAt: '2026-10-02T11:59:59Z' }],
    ['an expiry right now', { ...token, expiresAt: '2026-10-02T12:00:00Z' }],
  ])('rejects %s', (_label, response) => {
    expect(sessionFromToken('demo', response, now)).toBeNull();
  });
});

describe('isActive', () => {
  it('is active only before the expiry', () => {
    expect(isActive(session, now)).toBe(true);
    expect(isActive(session, session.expiresAt)).toBe(false);
    expect(isActive(null, now)).toBe(false);
  });
});

describe('expiryDelayMs', () => {
  it('waits until the expiry, never less than zero', () => {
    expect(expiryDelayMs(session, now)).toBe(8 * 60 * 60 * 1000);
    expect(expiryDelayMs(session, session.expiresAt + 1000)).toBe(0);
  });

  it('caps the delay at what a timer can hold', () => {
    expect(expiryDelayMs({ ...session, expiresAt: now + maxTimerDelayMs * 2 }, now)).toBe(maxTimerDelayMs);
  });
});

describe('restoreSession', () => {
  it('restores a serialized session that is still active', () => {
    expect(restoreSession(serializeSession(session), now)).toEqual(session);
  });

  it.each([
    ['nothing stored', null],
    ['invalid JSON', '{'],
    ['an incomplete session', JSON.stringify({ username: 'demo' })],
    ['an expired session', serializeSession({ ...session, expiresAt: now - 1 })],
  ])('ignores %s', (_label, raw) => {
    expect(restoreSession(raw, now)).toBeNull();
  });
});
