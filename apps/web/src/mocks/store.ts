import { maxEscalationLevel } from '../features/oncall';
import {
  type AcknowledgeInput,
  canTransition,
  type DeclareIncidentInput,
  type Incident,
  type IncidentDetail,
  type IncidentQuery,
  type IncidentStatus,
  isOpen,
  liveSlaState,
  type MitigateInput,
  type NoteInput,
  type ResolveInput,
  severities,
  slaPolicy,
  type TimelineEntry,
  type TimelineKind,
  isLateAcknowledgement,
} from '../features/incidents';
import type { MetricsSummary } from '../features/dashboard';
import { incidentFixtures } from './fixtures/incidents';

export class TransitionRejected extends Error {
  readonly from: IncidentStatus;
  readonly to: IncidentStatus;

  constructor(from: IncidentStatus, to: IncidentStatus) {
    super(`Cannot move an incident from ${from} to ${to}.`);
    this.from = from;
    this.to = to;
  }
}

export class EscalationRejected extends Error {
  constructor(level: number) {
    super(
      `Only triggered incidents below level ${String(maxEscalationLevel)} can escalate; this one is at level ${String(level)}.`,
    );
  }
}

function withoutTimeline({ timeline: _timeline, ...incident }: IncidentDetail): Incident {
  return incident;
}

function matches(incident: Incident, query: IncidentQuery): boolean {
  if (query.status !== undefined && incident.status !== query.status) {
    return false;
  }
  if (query.severity !== undefined && incident.severity !== query.severity) {
    return false;
  }
  if (query.serviceId !== undefined && incident.serviceId !== query.serviceId) {
    return false;
  }
  return !(query.open === true && !isOpen(incident));
}

export class MockIncidentStore {
  private readonly incidents = new Map<string, IncidentDetail>();
  private nextNumber: number;

  constructor(now: number) {
    const fixtures = incidentFixtures(now);
    fixtures.forEach((incident) => this.incidents.set(incident.id, incident));
    this.nextNumber = Math.max(...fixtures.map((incident) => incident.number)) + 1;
  }

  list(query: IncidentQuery): Incident[] {
    return [...this.incidents.values()]
      .filter((incident) => matches(incident, query))
      .sort((left, right) => Date.parse(right.createdAt) - Date.parse(left.createdAt))
      .map((incident) => withoutTimeline(this.refreshed(incident)));
  }

  get(id: string): IncidentDetail | undefined {
    const incident = this.incidents.get(id);
    if (incident === undefined) {
      return undefined;
    }
    return this.refreshed(incident);
  }

  declare(input: DeclareIncidentInput): Incident {
    const now = Date.now();
    const id = crypto.randomUUID();
    const incident: IncidentDetail = {
      id,
      number: this.nextNumber,
      title: input.title,
      description: input.description,
      serviceId: input.serviceId,
      severity: input.severity,
      status: 'Triggered',
      assignee: 'Ana Ribeiro',
      escalationLevel: 1,
      createdAt: new Date(now).toISOString(),
      acknowledgedAt: null,
      mitigatedAt: null,
      resolvedAt: null,
      ackDueAt: new Date(now + slaPolicy[input.severity].acknowledgeMs).toISOString(),
      resolveDueAt: new Date(now + slaPolicy[input.severity].resolveMs).toISOString(),
      acknowledgementBreached: false,
      slaState: 'OnTrack',
      rootCause: null,
      source: 'Manual',
      alertFingerprint: null,
      timeline: [],
    };
    this.nextNumber += 1;
    this.incidents.set(id, incident);
    this.append(incident, 'Triggered', 'console', `Incident declared as ${input.severity}.`);
    return withoutTimeline(incident);
  }

  acknowledge(id: string, input: AcknowledgeInput): Incident | undefined {
    const acknowledgedAt = new Date().toISOString();
    const current = this.incidents.get(id);
    const late = current !== undefined && isLateAcknowledgement(current.ackDueAt, acknowledgedAt);
    return this.transition(id, 'Acknowledged', input.actor, 'Acknowledged.', {
      acknowledgedAt,
      acknowledgementBreached: (current?.acknowledgementBreached ?? false) || late,
    });
  }

  mitigate(id: string, input: MitigateInput): Incident | undefined {
    return this.transition(id, 'Mitigated', input.actor, input.note, { mitigatedAt: new Date().toISOString() });
  }

  resolve(id: string, input: ResolveInput): Incident | undefined {
    return this.transition(id, 'Resolved', input.actor, input.rootCause, {
      resolvedAt: new Date().toISOString(),
      rootCause: input.rootCause,
    });
  }

  escalate(id: string, reason: string): Incident | undefined {
    const incident = this.incidents.get(id);
    if (incident === undefined) {
      return undefined;
    }
    if (incident.status !== 'Triggered' || incident.escalationLevel >= maxEscalationLevel) {
      throw new EscalationRejected(incident.escalationLevel);
    }
    const escalated = this.refreshed({
      ...incident,
      escalationLevel: incident.escalationLevel + 1,
      ackDueAt: new Date(Date.now() + slaPolicy[incident.severity].acknowledgeMs).toISOString(),
      acknowledgementBreached: true,
    });
    this.incidents.set(id, escalated);
    this.append(escalated, 'Escalated', 'sla-watchdog', reason);
    return withoutTimeline(escalated);
  }

  addNote(id: string, input: NoteInput): TimelineEntry | undefined {
    const incident = this.incidents.get(id);
    if (incident === undefined) {
      return undefined;
    }
    return this.append(incident, 'Note', input.actor, input.message);
  }

  metricsSummary(): MetricsSummary {
    const now = Date.now();
    const open = [...this.incidents.values()].filter(isOpen);
    const openBySeverity = Object.fromEntries(
      severities.map((severity) => [severity, open.filter((incident) => incident.severity === severity).length]),
    ) as MetricsSummary['openBySeverity'];
    return {
      openBySeverity,
      slaCompliance30d: 92.4,
      mtta30dMinutes: 11.6,
      mttr30dMinutes: 236,
      breachedOpen: open.filter((incident) => liveSlaState(incident, now) === 'Breached').length,
    };
  }

  private transition(
    id: string,
    to: IncidentStatus,
    actor: string,
    message: string,
    changes: Partial<Incident>,
  ): Incident | undefined {
    const incident = this.incidents.get(id);
    if (incident === undefined) {
      return undefined;
    }
    if (!canTransition(incident.status, to)) {
      throw new TransitionRejected(incident.status, to);
    }
    const updated = this.refreshed({ ...incident, ...changes, status: to, assignee: incident.assignee ?? actor });
    this.incidents.set(id, updated);
    this.append(updated, to, actor, message);
    return withoutTimeline(updated);
  }

  private append(incident: IncidentDetail, kind: TimelineKind, actor: string, message: string): TimelineEntry {
    const entry: TimelineEntry = {
      id: crypto.randomUUID(),
      incidentId: incident.id,
      at: new Date().toISOString(),
      kind,
      actor,
      message,
    };
    const current = this.incidents.get(incident.id) ?? incident;
    this.incidents.set(incident.id, { ...current, timeline: [...current.timeline, entry] });
    return entry;
  }

  private refreshed(incident: IncidentDetail): IncidentDetail {
    return { ...incident, slaState: liveSlaState(incident, Date.now()) };
  }
}
