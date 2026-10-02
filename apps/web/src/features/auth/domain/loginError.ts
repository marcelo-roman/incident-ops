import { ApiError, describeError } from '../../../shared/http/problem';

export class UnusableTokenError extends Error {
  constructor() {
    super('The sign-in response did not contain a usable token.');
    this.name = 'UnusableTokenError';
  }
}

export const invalidCredentialsMessage = 'Those credentials were not accepted. Check them and try again.';
export const tooManyAttemptsMessage = 'Too many sign-in attempts. Wait a minute and try again.';
export const unusableTokenMessage = 'The sign-in response could not be used. Try again.';

export function describeLoginError(error: unknown): string {
  if (error instanceof UnusableTokenError) {
    return unusableTokenMessage;
  }
  if (error instanceof ApiError && error.status === 401) {
    return invalidCredentialsMessage;
  }
  if (error instanceof ApiError && error.status === 429) {
    return tooManyAttemptsMessage;
  }
  return describeError(error);
}
