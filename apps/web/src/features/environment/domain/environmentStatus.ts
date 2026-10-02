import { ApiError } from '../../../shared/http/problem';

export type EnvironmentStatus = 'checking' | 'available' | 'paused';

export interface ProbeState {
  isPending: boolean;
  error: unknown;
}

export const pausedRecheckMs = 30_000;
export const availableRecheckMs = 5 * 60_000;

const unreachableStatusCodes: ReadonlySet<number> = new Set([502, 503, 504]);

export function indicatesPause(error: unknown): boolean {
  if (error instanceof ApiError) {
    return unreachableStatusCodes.has(error.status);
  }
  return true;
}

export function environmentStatus({ isPending, error }: ProbeState): EnvironmentStatus {
  if (error !== null && error !== undefined) {
    return indicatesPause(error) ? 'paused' : 'available';
  }
  if (isPending) {
    return 'checking';
  }
  return 'available';
}

export function recheckInterval(status: EnvironmentStatus): number {
  if (status === 'paused') {
    return pausedRecheckMs;
  }
  return availableRecheckMs;
}
