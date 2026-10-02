import { describeEscalationLevel } from '../../oncall';
import { formatDateTime } from '../../../shared/format/dates';
import type { Incident } from './incident';

export interface IncidentFact {
  term: string;
  detail: string;
}

function alertFacts(incident: Incident): IncidentFact[] {
  if (incident.alertFingerprint === null) {
    return [];
  }
  return [{ term: 'Alert fingerprint', detail: incident.alertFingerprint }];
}

export function incidentFacts(incident: Incident, serviceName: string): IncidentFact[] {
  return [
    { term: 'Service', detail: serviceName },
    { term: 'Assignee', detail: incident.assignee ?? 'Unassigned' },
    { term: 'Escalation', detail: describeEscalationLevel(incident.escalationLevel) },
    { term: 'Opened', detail: formatDateTime(incident.createdAt) },
    { term: 'Acknowledged', detail: formatDateTime(incident.acknowledgedAt) },
    { term: 'Mitigated', detail: formatDateTime(incident.mitigatedAt) },
    { term: 'Resolved', detail: formatDateTime(incident.resolvedAt) },
    ...alertFacts(incident),
  ];
}
