import type { IRetryPolicy, RetryContext } from '@microsoft/signalr';

export const baseDelayMs = 1_000;
export const maxDelayMs = 30_000;

export function backoffDelay(attempt: number, random: () => number = Math.random): number {
  const ceiling = Math.min(maxDelayMs, baseDelayMs * 2 ** attempt);
  return Math.round(ceiling / 2 + (random() * ceiling) / 2);
}

export const backoffRetryPolicy: IRetryPolicy = {
  nextRetryDelayInMilliseconds: (context: RetryContext) => backoffDelay(context.previousRetryCount),
};
