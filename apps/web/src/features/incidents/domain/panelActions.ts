import type { IncidentStatus } from './incident';
import { availableActions, type IncidentAction } from './transitions';

export type PanelAction = IncidentAction | 'note';

export const panelActionLabels: Readonly<Record<PanelAction, string>> = {
  acknowledge: 'Acknowledge',
  mitigate: 'Mitigate',
  resolve: 'Resolve',
  note: 'Add note',
};

export function panelActions(status: IncidentStatus): PanelAction[] {
  return [...availableActions(status), 'note'];
}
