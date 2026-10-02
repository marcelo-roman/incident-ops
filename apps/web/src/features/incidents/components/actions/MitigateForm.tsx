import { TextAreaField, TextField } from '../../../../shared/ui/Field';
import { useMitigateForm } from '../../hooks/useIncidentActionForms';
import { ActionForm } from './ActionForm';
import type { IncidentActionFormProps } from './actionProps';

export function MitigateForm({ incidentId, onDone, onCancel }: IncidentActionFormProps) {
  const form = useMitigateForm(incidentId, onDone);
  return (
    <ActionForm
      label="Mitigate incident"
      submitLabel="Mark mitigated"
      pendingLabel="Saving"
      isPending={form.isPending}
      error={form.error}
      onSubmit={form.onSubmit}
      onCancel={onCancel}
    >
      <TextField
        label="Mitigated by"
        autoComplete="name"
        error={form.errors.actor?.message}
        {...form.register('actor')}
      />
      <TextAreaField
        label="What was done"
        hint="Customer impact should be contained at this point."
        error={form.errors.note?.message}
        {...form.register('note')}
      />
    </ActionForm>
  );
}
