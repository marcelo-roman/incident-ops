export const severities = ['Sev1', 'Sev2', 'Sev3', 'Sev4'] as const;
export type Severity = (typeof severities)[number];

export const incidentStatuses = ['Triggered', 'Acknowledged', 'Mitigated', 'Resolved'] as const;
export type IncidentStatus = (typeof incidentStatuses)[number];

export const slaStates = ['OnTrack', 'AtRisk', 'Breached', 'Met'] as const;
export type SlaState = (typeof slaStates)[number];

export const incidentSources = ['Manual', 'Alertmanager', 'AzureMonitor'] as const;
export type IncidentSource = (typeof incidentSources)[number];

export const timelineKinds = [
  'Triggered',
  'Acknowledged',
  'Escalated',
  'Mitigated',
  'Resolved',
  'Note',
  'Alert',
] as const;
export type TimelineKind = (typeof timelineKinds)[number];

export interface Incident {
  id: string;
  number: number;
  title: string;
  description: string;
  serviceId: string;
  severity: Severity;
  status: IncidentStatus;
  assignee: string | null;
  escalationLevel: number;
  createdAt: string;
  acknowledgedAt: string | null;
  mitigatedAt: string | null;
  resolvedAt: string | null;
  ackDueAt: string;
  acknowledgementBreached: boolean;
  resolveDueAt: string;
  slaState: SlaState;
  rootCause: string | null;
  source: IncidentSource;
  alertFingerprint: string | null;
}

export interface TimelineEntry {
  id: string;
  incidentId: string;
  at: string;
  kind: TimelineKind;
  actor: string;
  message: string;
}

export interface IncidentDetail extends Incident {
  timeline: TimelineEntry[];
}

export function formatIncidentNumber(incidentNumber: number): string {
  return `INC-${String(incidentNumber)}`;
}

export function isOpen(incident: Pick<Incident, 'status'>): boolean {
  return incident.status !== 'Resolved';
}

export function isSeverity(value: string): value is Severity {
  return (severities as readonly string[]).includes(value);
}

export function isIncidentSource(value: string): value is IncidentSource {
  return (incidentSources as readonly string[]).includes(value);
}

export function isIncidentStatus(value: string): value is IncidentStatus {
  return (incidentStatuses as readonly string[]).includes(value);
}
