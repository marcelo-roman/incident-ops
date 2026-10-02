import type { Incident, SlaState } from './incident';
import { atRiskThreshold, slaPolicy } from './slaPolicy';

export type SlaTarget = 'Acknowledge' | 'Resolve';
export type LiveSlaState = Exclude<SlaState, 'Met'>;

export interface SlaClock {
  target: SlaTarget;
  dueAt: number;
  remainingMs: number;
  fractionRemaining: number;
  state: LiveSlaState;
}

type SlaFields = Pick<
  Incident,
  'severity' | 'status' | 'ackDueAt' | 'resolveDueAt' | 'resolvedAt' | 'acknowledgedAt' | 'acknowledgementBreached'
>;

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function stateFor(remainingMs: number, fractionRemaining: number): LiveSlaState {
  if (remainingMs <= 0) {
    return 'Breached';
  }
  if (fractionRemaining < atRiskThreshold) {
    return 'AtRisk';
  }
  return 'OnTrack';
}

export function buildClock(target: SlaTarget, dueAtIso: string, windowMs: number, now: number): SlaClock {
  const dueAt = Date.parse(dueAtIso);
  const remainingMs = dueAt - now;
  const fractionRemaining = clamp(remainingMs / windowMs, 0, 1);
  return { target, dueAt, remainingMs, fractionRemaining, state: stateFor(remainingMs, fractionRemaining) };
}

export function acknowledgeClock(incident: SlaFields, now: number): SlaClock | null {
  if (incident.status !== 'Triggered') {
    return null;
  }
  return buildClock('Acknowledge', incident.ackDueAt, slaPolicy[incident.severity].acknowledgeMs, now);
}

export function resolveClock(incident: SlaFields, now: number): SlaClock | null {
  if (incident.status === 'Resolved') {
    return null;
  }
  return buildClock('Resolve', incident.resolveDueAt, slaPolicy[incident.severity].resolveMs, now);
}

export function activeClock(incident: SlaFields, now: number): SlaClock | null {
  return acknowledgeClock(incident, now) ?? resolveClock(incident, now);
}

export function isLateAcknowledgement(ackDueAt: string, acknowledgedAt: string): boolean {
  return Date.parse(acknowledgedAt) > Date.parse(ackDueAt);
}

export function compliesWithSla(incident: SlaFields): boolean {
  if (incident.acknowledgedAt === null || incident.acknowledgementBreached || incident.resolvedAt === null) {
    return false;
  }
  return Date.parse(incident.resolvedAt) <= Date.parse(incident.resolveDueAt);
}

export function resolvedSlaState(incident: SlaFields): SlaState {
  if (compliesWithSla(incident)) {
    return 'Met';
  }
  return 'Breached';
}

export function liveSlaState(incident: SlaFields, now: number): SlaState {
  if (incident.status === 'Resolved') {
    return resolvedSlaState(incident);
  }
  const clocks = [acknowledgeClock(incident, now), resolveClock(incident, now)].filter(
    (clock): clock is SlaClock => clock !== null,
  );
  if (clocks.some((clock) => clock.state === 'Breached')) {
    return 'Breached';
  }
  return clocks[0]?.state ?? 'OnTrack';
}

export function mostUrgentFirst(left: SlaFields, right: SlaFields, now: number): number {
  return urgencyKey(left, now) - urgencyKey(right, now);
}

function urgencyKey(incident: SlaFields, now: number): number {
  return activeClock(incident, now)?.remainingMs ?? Number.POSITIVE_INFINITY;
}
