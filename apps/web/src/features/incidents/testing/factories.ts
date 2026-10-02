import type { Incident, IncidentDetail } from '../domain/incident';

export const baseTime = Date.parse('2026-10-02T12:00:00.000Z');

export function anIncident(overrides: Partial<Incident> = {}): Incident {
  return {
    id: 'incident-1',
    number: 1042,
    title: 'Checkout returns 502',
    description: 'Card payments fail.',
    serviceId: 'checkout',
    severity: 'Sev1',
    status: 'Triggered',
    assignee: 'Ana Ribeiro',
    escalationLevel: 1,
    createdAt: new Date(baseTime).toISOString(),
    acknowledgedAt: null,
    mitigatedAt: null,
    resolvedAt: null,
    ackDueAt: new Date(baseTime + 15 * 60_000).toISOString(),
    acknowledgementBreached: false,
    resolveDueAt: new Date(baseTime + 4 * 3_600_000).toISOString(),
    slaState: 'OnTrack',
    rootCause: null,
    source: 'Manual',
    alertFingerprint: null,
    ...overrides,
  };
}

export function anIncidentDetail(overrides: Partial<IncidentDetail> = {}): IncidentDetail {
  return { ...anIncident(overrides), timeline: [], ...overrides };
}
