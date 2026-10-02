import { formatCountdown } from '../../../shared/format/duration';
import { slaStateLabels, slaTargetLabels } from './labels';
import type { SlaClock } from './sla';

const minimumVisibleBurn = 2;

export function describeClock(clock: SlaClock): string {
  const target = slaTargetLabels[clock.target];
  const state = slaStateLabels[clock.state];
  if (clock.remainingMs < 0) {
    return `${target} deadline passed ${formatCountdown(-clock.remainingMs)} ago, ${state}`;
  }
  return `${target} within ${formatCountdown(clock.remainingMs)}, ${state}`;
}

export function burnPercent(clock: SlaClock): number {
  if (clock.state === 'Breached') {
    return 100;
  }
  return Math.max(minimumVisibleBurn, Math.round(clock.fractionRemaining * 100));
}

export function clockReading(clock: SlaClock): { time: string; overdue: boolean } {
  return { time: formatCountdown(Math.abs(clock.remainingMs)), overdue: clock.remainingMs < 0 };
}
