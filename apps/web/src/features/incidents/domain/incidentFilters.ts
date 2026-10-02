import type { Incident, IncidentSource, IncidentStatus, Severity } from './incident';

export interface IncidentFilters {
  status?: IncidentStatus;
  severity?: Severity;
  serviceId?: string;
  source?: IncidentSource;
  open?: boolean;
}

export type ServerIncidentFilters = Omit<IncidentFilters, 'source'>;

export function serverFilters(filters: IncidentFilters): ServerIncidentFilters {
  const query: IncidentFilters = { ...filters };
  delete query.source;
  return query;
}

export function filterBySource(incidents: Incident[], source: IncidentSource | undefined): Incident[] {
  if (source === undefined) {
    return incidents;
  }
  return incidents.filter((incident) => incident.source === source);
}
