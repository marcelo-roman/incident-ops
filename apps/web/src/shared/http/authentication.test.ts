import { afterEach, describe, expect, it } from 'vitest';
import {
  anonymousAuthentication,
  getHttpAuthentication,
  setHttpAuthentication,
  type HttpAuthentication,
} from './authentication';

describe('http authentication registry', () => {
  const initial = getHttpAuthentication();

  afterEach(() => {
    setHttpAuthentication(initial);
  });

  it('returns the authentication that was set', () => {
    const authentication: HttpAuthentication = { accessToken: () => 'token', onUnauthorized: () => undefined };

    setHttpAuthentication(authentication);

    expect(getHttpAuthentication()).toBe(authentication);
  });

  it('can be reset to anonymous', () => {
    setHttpAuthentication(anonymousAuthentication);

    expect(getHttpAuthentication().accessToken()).toBeNull();
  });
});
