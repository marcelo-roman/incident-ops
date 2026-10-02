import { describe, expect, it } from 'vitest';
import { ApiError } from '../../../shared/http/problem';
import {
  describeLoginError,
  invalidCredentialsMessage,
  tooManyAttemptsMessage,
  UnusableTokenError,
  unusableTokenMessage,
} from './loginError';

describe('describeLoginError', () => {
  it('does not say which credential was wrong', () => {
    expect(describeLoginError(new ApiError(401, { detail: 'Wrong password.' }))).toBe(invalidCredentialsMessage);
  });

  it('asks to wait after too many attempts', () => {
    expect(describeLoginError(new ApiError(429, null))).toBe(tooManyAttemptsMessage);
  });

  it('explains an unusable token', () => {
    expect(describeLoginError(new UnusableTokenError())).toBe(unusableTokenMessage);
  });

  it('falls back to the shared descriptions', () => {
    expect(describeLoginError(new TypeError('Failed to fetch'))).toMatch(/could not be reached/);
    expect(describeLoginError(new ApiError(500, { detail: 'Boom.' }))).toBe('Boom.');
  });
});
