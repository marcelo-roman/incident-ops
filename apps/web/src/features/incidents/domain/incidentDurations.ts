import type { Incident } from './incident';

function elapsed(from: string, to: string | null): number | null {
  if (to === null) {
    return null;
  }
  return Date.parse(to) - Date.parse(from);
}

export function timeToAcknowledgeMs(incident: Pick<Incident, 'createdAt' | 'acknowledgedAt'>): number | null {
  return elapsed(incident.createdAt, incident.acknowledgedAt);
}

export function timeToResolveMs(incident: Pick<Incident, 'createdAt' | 'resolvedAt'>): number | null {
  return elapsed(incident.createdAt, incident.resolvedAt);
}
