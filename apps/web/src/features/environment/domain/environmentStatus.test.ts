import { describe, expect, it } from 'vitest';
import { ApiError } from '../../../shared/http/problem';
import {
  availableRecheckMs,
  environmentStatus,
  indicatesPause,
  pausedRecheckMs,
  recheckInterval,
} from './environmentStatus';

describe('indicatesPause', () => {
  it.each([502, 503, 504])('treats %i as an unreachable environment', (status) => {
    expect(indicatesPause(new ApiError(status, null))).toBe(true);
  });

  it.each([400, 404, 500])('treats %i as a reachable environment', (status) => {
    expect(indicatesPause(new ApiError(status, null))).toBe(false);
  });

  it('treats network failures and timeouts as an unreachable environment', () => {
    expect(indicatesPause(new TypeError('Failed to fetch'))).toBe(true);
    expect(indicatesPause(new DOMException('The operation timed out.', 'TimeoutError'))).toBe(true);
  });
});

describe('environmentStatus', () => {
  it('is checking until the first probe answers', () => {
    expect(environmentStatus({ isPending: true, error: null })).toBe('checking');
  });

  it('is available once the probe succeeds', () => {
    expect(environmentStatus({ isPending: false, error: null })).toBe('available');
    expect(environmentStatus({ isPending: false, error: undefined })).toBe('available');
  });

  it('is paused when the probe cannot reach the API', () => {
    expect(environmentStatus({ isPending: false, error: new TypeError('Failed to fetch') })).toBe('paused');
    expect(environmentStatus({ isPending: false, error: new ApiError(503, null) })).toBe('paused');
  });

  it('stays available when the API answers with an application error', () => {
    expect(environmentStatus({ isPending: false, error: new ApiError(500, null) })).toBe('available');
  });
});

describe('recheckInterval', () => {
  it('checks often while paused and rarely while available', () => {
    expect(recheckInterval('paused')).toBe(pausedRecheckMs);
    expect(recheckInterval('available')).toBe(availableRecheckMs);
    expect(recheckInterval('checking')).toBe(availableRecheckMs);
    expect(pausedRecheckMs).toBeLessThan(availableRecheckMs);
  });
});
