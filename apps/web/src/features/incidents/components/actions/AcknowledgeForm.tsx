import { TextField } from '../../../../shared/ui/Field';
import { useAcknowledgeForm } from '../../hooks/useIncidentActionForms';
import { ActionForm } from './ActionForm';
import type { IncidentActionFormProps } from './actionProps';

export function AcknowledgeForm({ incidentId, onDone, onCancel }: IncidentActionFormProps) {
  const form = useAcknowledgeForm(incidentId, onDone);
  return (
    <ActionForm
      label="Acknowledge incident"
      submitLabel="Acknowledge"
      pendingLabel="Acknowledging"
      isPending={form.isPending}
      error={form.error}
      onSubmit={form.onSubmit}
      onCancel={onCancel}
    >
      <TextField
        label="Acknowledged by"
        autoComplete="name"
        error={form.errors.actor?.message}
        {...form.register('actor')}
      />
    </ActionForm>
  );
}
