import type { IncidentStatus } from './incident';

export type IncidentAction = 'acknowledge' | 'mitigate' | 'resolve';

const allowedTransitions: Readonly<Record<IncidentStatus, readonly IncidentStatus[]>> = {
  Triggered: ['Acknowledged', 'Mitigated', 'Resolved'],
  Acknowledged: ['Mitigated', 'Resolved'],
  Mitigated: ['Resolved'],
  Resolved: [],
};

const actionTargets: Readonly<Record<IncidentAction, IncidentStatus>> = {
  acknowledge: 'Acknowledged',
  mitigate: 'Mitigated',
  resolve: 'Resolved',
};

export function canTransition(from: IncidentStatus, to: IncidentStatus): boolean {
  return allowedTransitions[from].includes(to);
}

export function availableActions(status: IncidentStatus): IncidentAction[] {
  return (Object.keys(actionTargets) as IncidentAction[]).filter((action) =>
    canTransition(status, actionTargets[action]),
  );
}

export function targetStatus(action: IncidentAction): IncidentStatus {
  return actionTargets[action];
}
