import { describe, expect, it } from 'vitest';
import { backoffDelay, backoffRetryPolicy, maxDelayMs } from './retryPolicy';

describe('backoffDelay', () => {
  it('doubles the ceiling on each attempt', () => {
    const highest = () => 1;

    expect([0, 1, 2, 3].map((attempt) => backoffDelay(attempt, highest))).toEqual([1_000, 2_000, 4_000, 8_000]);
  });

  it('applies jitter within the upper half of the ceiling', () => {
    expect(backoffDelay(2, () => 0)).toBe(2_000);
    expect(backoffDelay(2, () => 0.5)).toBe(3_000);
  });

  it('caps the delay', () => {
    expect(backoffDelay(20, () => 1)).toBe(maxDelayMs);
  });

  it('never gives up reconnecting', () => {
    const delay = backoffRetryPolicy.nextRetryDelayInMilliseconds({
      previousRetryCount: 500,
      elapsedMilliseconds: 86_400_000,
      retryReason: new Error('lost'),
    });

    expect(delay).toBeGreaterThan(0);
  });
});
