import { describe, expect, it } from 'vitest';
import { anIncident, baseTime } from '../testing/factories';
import {
  acknowledgeClock,
  activeClock,
  compliesWithSla,
  isLateAcknowledgement,
  liveSlaState,
  mostUrgentFirst,
  resolveClock,
} from './sla';

const minute = 60_000;
const hour = 60 * minute;

describe('acknowledgeClock', () => {
  it('counts down to ackDueAt while the incident is triggered', () => {
    const clock = acknowledgeClock(anIncident(), baseTime + 5 * minute);

    expect(clock).toMatchObject({ target: 'Acknowledge', remainingMs: 10 * minute, state: 'OnTrack' });
    expect(clock?.fractionRemaining).toBeCloseTo(10 / 15);
  });

  it('is at risk when less than a quarter of the window remains', () => {
    const clock = acknowledgeClock(anIncident(), baseTime + 12 * minute);

    expect(clock?.state).toBe('AtRisk');
  });

  it('stays on track at exactly a quarter of the window', () => {
    const atQuarter = baseTime + 15 * minute - 15 * minute * 0.25;

    expect(acknowledgeClock(anIncident(), atQuarter)?.state).toBe('OnTrack');
  });

  it('is breached once the deadline passes, with negative remaining time', () => {
    const clock = acknowledgeClock(anIncident(), baseTime + 20 * minute);

    expect(clock).toMatchObject({ state: 'Breached', remainingMs: -5 * minute, fractionRemaining: 0 });
  });

  it('measures the window from the latest ackDueAt after an escalation', () => {
    const escalated = anIncident({
      escalationLevel: 2,
      ackDueAt: new Date(baseTime + 30 * minute).toISOString(),
    });

    const clock = acknowledgeClock(escalated, baseTime + 20 * minute);

    expect(clock?.state).toBe('OnTrack');
    expect(clock?.fractionRemaining).toBeCloseTo(10 / 15);
  });

  it('does not apply once the incident is acknowledged', () => {
    expect(acknowledgeClock(anIncident({ status: 'Acknowledged' }), baseTime)).toBeNull();
  });
});

describe('resolveClock', () => {
  it('uses the resolve window of the severity', () => {
    const incident = anIncident({ status: 'Acknowledged', severity: 'Sev1' });

    const clock = resolveClock(incident, baseTime + 3.5 * hour);

    expect(clock).toMatchObject({ target: 'Resolve', remainingMs: 0.5 * hour, state: 'AtRisk' });
  });

  it('does not apply to resolved incidents', () => {
    expect(resolveClock(anIncident({ status: 'Resolved' }), baseTime)).toBeNull();
  });
});

describe('activeClock', () => {
  it('prefers the acknowledge clock while triggered', () => {
    expect(activeClock(anIncident(), baseTime)?.target).toBe('Acknowledge');
  });

  it('falls back to the resolve clock after acknowledgement', () => {
    expect(activeClock(anIncident({ status: 'Mitigated' }), baseTime)?.target).toBe('Resolve');
  });
});

describe('liveSlaState', () => {
  it('is met when acknowledged in time and resolved before resolveDueAt', () => {
    const incident = anIncident({
      status: 'Resolved',
      acknowledgedAt: new Date(baseTime + 5 * minute).toISOString(),
      resolvedAt: new Date(baseTime + hour).toISOString(),
    });

    expect(liveSlaState(incident, baseTime + 10 * hour)).toBe('Met');
  });

  it('is breached when resolved after resolveDueAt', () => {
    const incident = anIncident({ status: 'Resolved', resolvedAt: new Date(baseTime + 5 * hour).toISOString() });

    expect(liveSlaState(incident, baseTime + 10 * hour)).toBe('Breached');
  });

  it('is breached when the resolve deadline passed even though the ack window is fresh', () => {
    const incident = anIncident({
      escalationLevel: 3,
      ackDueAt: new Date(baseTime + 5 * hour).toISOString(),
    });

    expect(liveSlaState(incident, baseTime + 4.5 * hour)).toBe('Breached');
  });

  it('follows the active clock otherwise', () => {
    expect(liveSlaState(anIncident(), baseTime + 13 * minute)).toBe('AtRisk');
    expect(liveSlaState(anIncident(), baseTime + minute)).toBe('OnTrack');
  });
});

describe('SLA compliance', () => {
  const resolvedOnTime = anIncident({
    status: 'Resolved',
    acknowledgedAt: new Date(baseTime + 5 * minute).toISOString(),
    resolvedAt: new Date(baseTime + hour).toISOString(),
  });

  it('complies when acknowledged without a breach and resolved on time', () => {
    expect(compliesWithSla(resolvedOnTime)).toBe(true);
  });

  it('does not comply once the acknowledgement was breached, even if resolved on time', () => {
    const escalated = { ...resolvedOnTime, acknowledgementBreached: true };

    expect(compliesWithSla(escalated)).toBe(false);
    expect(liveSlaState(escalated, baseTime + 2 * hour)).toBe('Breached');
  });

  it('does not comply when resolved without ever being acknowledged', () => {
    const neverAcknowledged = { ...resolvedOnTime, acknowledgedAt: null };

    expect(liveSlaState(neverAcknowledged, baseTime + 2 * hour)).toBe('Breached');
  });

  it('does not comply when still unresolved', () => {
    expect(compliesWithSla({ ...resolvedOnTime, status: 'Mitigated', resolvedAt: null })).toBe(false);
  });

  it('detects late acknowledgements against ackDueAt', () => {
    expect(isLateAcknowledgement('2026-10-02T12:15:00Z', '2026-10-02T12:15:01Z')).toBe(true);
    expect(isLateAcknowledgement('2026-10-02T12:15:00Z', '2026-10-02T12:15:00Z')).toBe(false);
  });
});

describe('mostUrgentFirst', () => {
  it('orders by the remaining time on the active clock', () => {
    const relaxed = anIncident({
      id: 'relaxed',
      severity: 'Sev4',
      ackDueAt: new Date(baseTime + 24 * hour).toISOString(),
    });
    const urgent = anIncident({ id: 'urgent' });

    const ordered = [relaxed, urgent].sort((left, right) => mostUrgentFirst(left, right, baseTime));

    expect(ordered.map((incident) => incident.id)).toEqual(['urgent', 'relaxed']);
  });
});
