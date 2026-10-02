import {
  type IncidentDetail,
  type IncidentSource,
  type IncidentStatus,
  liveSlaState,
  type Severity,
  slaPolicy,
  type TimelineEntry,
  type TimelineKind,
} from '../../features/incidents';

const minute = 60_000;

export interface IncidentSpec {
  number: number;
  title: string;
  description: string;
  serviceId: string;
  severity: Severity;
  status: IncidentStatus;
  openedMinutesAgo: number;
  acknowledgedAfter?: number;
  mitigatedAfter?: number;
  resolvedAfter?: number;
  escalationLevel?: number;
  assignee?: string;
  rootCause?: string;
  source?: IncidentSource;
  alertFingerprint?: string;
  alerts?: { after: number; message: string }[];
  notes?: { after: number; actor: string; message: string }[];
}

function iso(ms: number): string {
  return new Date(ms).toISOString();
}

function offset(createdAt: number, after: number | undefined): string | null {
  if (after === undefined) {
    return null;
  }
  return iso(createdAt + after * minute);
}

function entry(incidentId: string, kind: TimelineKind, at: number, actor: string, message: string): TimelineEntry {
  return { id: `${incidentId}-${kind}-${String(at)}`, incidentId, at: iso(at), kind, actor, message };
}

function ackDueAt(spec: IncidentSpec, createdAt: number): number {
  const window = slaPolicy[spec.severity].acknowledgeMs;
  const escalations = (spec.escalationLevel ?? 1) - 1;
  return createdAt + window * (escalations + 1);
}

function lifecycleEntries(spec: IncidentSpec, id: string, createdAt: number, assignee: string): TimelineEntry[] {
  const at = (after: number) => createdAt + after * minute;
  const entries = [
    entry(id, 'Triggered', createdAt, sourceActor(spec.source), `Incident declared as ${spec.severity}.`),
  ];
  const ackWindowMinutes = slaPolicy[spec.severity].acknowledgeMs / minute;
  for (let level = 2; level <= (spec.escalationLevel ?? 1); level += 1) {
    entries.push(
      entry(
        id,
        'Escalated',
        at(ackWindowMinutes * (level - 1)),
        'sla-watchdog',
        `Not acknowledged in time, escalated to level ${String(level)}.`,
      ),
    );
  }
  if (spec.acknowledgedAfter !== undefined) {
    entries.push(entry(id, 'Acknowledged', at(spec.acknowledgedAfter), assignee, 'Acknowledged, investigating.'));
  }
  if (spec.mitigatedAfter !== undefined) {
    entries.push(entry(id, 'Mitigated', at(spec.mitigatedAfter), assignee, 'Impact contained, monitoring recovery.'));
  }
  if (spec.resolvedAfter !== undefined) {
    entries.push(entry(id, 'Resolved', at(spec.resolvedAfter), assignee, spec.rootCause ?? 'Resolved.'));
  }
  return entries;
}

function sourceActor(source: IncidentSource | undefined): string {
  if (source === 'Alertmanager') {
    return 'alertmanager';
  }
  if (source === 'AzureMonitor') {
    return 'azure-monitor';
  }
  return 'Ana Ribeiro';
}

function extraEntries(spec: IncidentSpec, id: string, createdAt: number): TimelineEntry[] {
  const at = (after: number) => createdAt + after * minute;
  const alerts = (spec.alerts ?? []).map((alert) =>
    entry(id, 'Alert', at(alert.after), sourceActor(spec.source), alert.message),
  );
  const notes = (spec.notes ?? []).map((note) => entry(id, 'Note', at(note.after), note.actor, note.message));
  return [...alerts, ...notes];
}

function acknowledgementBreached(spec: IncidentSpec): boolean {
  const ackWindowMinutes = slaPolicy[spec.severity].acknowledgeMs / minute;
  return (spec.escalationLevel ?? 1) > 1 || (spec.acknowledgedAfter ?? 0) > ackWindowMinutes;
}

export function buildIncident(spec: IncidentSpec, now: number): IncidentDetail {
  const id = `00000000-0000-4000-8000-${String(spec.number).padStart(12, '0')}`;
  const createdAt = now - spec.openedMinutesAgo * minute;
  const assignee = spec.assignee ?? 'Ana Ribeiro';
  const timeline = [...lifecycleEntries(spec, id, createdAt, assignee), ...extraEntries(spec, id, createdAt)].sort(
    (left, right) => Date.parse(left.at) - Date.parse(right.at),
  );
  const incident: IncidentDetail = {
    id,
    number: spec.number,
    title: spec.title,
    description: spec.description,
    serviceId: spec.serviceId,
    severity: spec.severity,
    status: spec.status,
    assignee,
    escalationLevel: spec.escalationLevel ?? 1,
    createdAt: iso(createdAt),
    acknowledgedAt: offset(createdAt, spec.acknowledgedAfter),
    mitigatedAt: offset(createdAt, spec.mitigatedAfter),
    resolvedAt: offset(createdAt, spec.resolvedAfter),
    ackDueAt: iso(ackDueAt(spec, createdAt)),
    acknowledgementBreached: acknowledgementBreached(spec),
    resolveDueAt: iso(createdAt + slaPolicy[spec.severity].resolveMs),
    slaState: 'OnTrack',
    rootCause: spec.rootCause ?? null,
    source: spec.source ?? 'Manual',
    alertFingerprint: spec.alertFingerprint ?? null,
    timeline,
  };
  return { ...incident, slaState: liveSlaState(incident, now) };
}
