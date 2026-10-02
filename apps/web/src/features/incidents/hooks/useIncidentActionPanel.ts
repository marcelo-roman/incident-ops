import { useState } from 'react';
import type { IncidentStatus } from '../domain/incident';
import { panelActions, type PanelAction } from '../domain/panelActions';

export function useIncidentActionPanel(status: IncidentStatus) {
  const [selected, setSelected] = useState<PanelAction | null>(null);
  const [confirmation, setConfirmation] = useState('');
  const actions = panelActions(status);
  const active = actions.find((action) => action === selected);

  return {
    actions,
    active,
    confirmation,
    choose: (action: PanelAction) => {
      setConfirmation('');
      setSelected(action);
    },
    cancel: () => {
      setSelected(null);
    },
    complete: (message: string) => {
      setSelected(null);
      setConfirmation(message);
    },
  };
}
