import type { IncidentSource, IncidentStatus, SlaState, TimelineKind } from './incident';
import type { SlaTarget } from './sla';

export const slaStateLabels: Readonly<Record<SlaState, string>> = {
  OnTrack: 'On track',
  AtRisk: 'At risk',
  Breached: 'Breached',
  Met: 'Met',
};

export const statusLabels: Readonly<Record<IncidentStatus, string>> = {
  Triggered: 'Triggered',
  Acknowledged: 'Acknowledged',
  Mitigated: 'Mitigated',
  Resolved: 'Resolved',
};

export const slaTargetLabels: Readonly<Record<SlaTarget, string>> = {
  Acknowledge: 'Acknowledge',
  Resolve: 'Resolve',
};

export const timelineKindLabels: Readonly<Record<TimelineKind, string>> = {
  Triggered: 'Triggered',
  Acknowledged: 'Acknowledged',
  Escalated: 'Escalated',
  Mitigated: 'Mitigated',
  Resolved: 'Resolved',
  Note: 'Note',
  Alert: 'Alert',
};

export const sourceLabels: Readonly<Record<IncidentSource, string>> = {
  Manual: 'Declared',
  Alertmanager: 'Alertmanager',
  AzureMonitor: 'Azure Monitor',
};
