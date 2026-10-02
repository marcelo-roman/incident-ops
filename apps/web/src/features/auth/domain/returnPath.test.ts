import { describe, expect, it } from 'vitest';
import { loginPathFor, safeReturnPath } from './returnPath';

describe('safeReturnPath', () => {
  it('keeps a path inside the console', () => {
    expect(safeReturnPath('/incidents?status=Triggered')).toBe('/incidents?status=Triggered');
  });

  it.each([null, '', 'incidents', 'https://evil.test', '//evil.test', '/\\evil.test', '/login', '/login?returnTo=/'])(
    'falls back to the dashboard for %s',
    (candidate) => {
      expect(safeReturnPath(candidate)).toBe('/');
    },
  );
});

describe('loginPathFor', () => {
  it('carries the return path', () => {
    expect(loginPathFor('/incidents/abc?tab=timeline')).toBe('/login?returnTo=%2Fincidents%2Fabc%3Ftab%3Dtimeline');
  });

  it('omits the return path for the dashboard and unsafe paths', () => {
    expect(loginPathFor('/')).toBe('/login');
    expect(loginPathFor('//evil.test')).toBe('/login');
  });
});
