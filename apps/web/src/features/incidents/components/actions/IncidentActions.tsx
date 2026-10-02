import type { ReactElement } from 'react';
import { Button } from '../../../../shared/ui/Button';
import { Panel } from '../../../../shared/ui/Panel';
import type { Incident } from '../../domain/incident';
import { panelActionLabels, type PanelAction } from '../../domain/panelActions';
import { useIncidentActionPanel } from '../../hooks/useIncidentActionPanel';
import { AcknowledgeForm } from './AcknowledgeForm';
import type { IncidentActionFormProps } from './actionProps';
import styles from './IncidentActions.module.css';
import { MitigateForm } from './MitigateForm';
import { NoteForm } from './NoteForm';
import { ResolveForm } from './ResolveForm';

const actionForms: Readonly<Record<PanelAction, (props: IncidentActionFormProps) => ReactElement>> = {
  acknowledge: (props) => <AcknowledgeForm {...props} />,
  mitigate: (props) => <MitigateForm {...props} />,
  resolve: (props) => <ResolveForm {...props} />,
  note: (props) => <NoteForm {...props} />,
};

export function IncidentActions({ incident }: Readonly<{ incident: Incident }>) {
  const panel = useIncidentActionPanel(incident.status);
  return (
    <Panel title="Response">
      <div className={styles.menu} role="group" aria-label="Incident actions">
        {panel.actions.map((action) => (
          <Button
            key={action}
            aria-pressed={action === panel.active}
            onClick={() => {
              panel.choose(action);
            }}
          >
            {panelActionLabels[action]}
          </Button>
        ))}
      </div>
      {panel.active && (
        <div key={panel.active} className={styles.form}>
          {actionForms[panel.active]({ incidentId: incident.id, onCancel: panel.cancel, onDone: panel.complete })}
        </div>
      )}
      <p className={styles.confirmation} role="status">
        {panel.confirmation}
      </p>
    </Panel>
  );
}
