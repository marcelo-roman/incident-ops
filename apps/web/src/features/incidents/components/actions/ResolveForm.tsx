import { TextAreaField, TextField } from '../../../../shared/ui/Field';
import { useResolveForm } from '../../hooks/useIncidentActionForms';
import { ActionForm } from './ActionForm';
import type { IncidentActionFormProps } from './actionProps';

export function ResolveForm({ incidentId, onDone, onCancel }: IncidentActionFormProps) {
  const form = useResolveForm(incidentId, onDone);
  return (
    <ActionForm
      label="Resolve incident"
      submitLabel="Resolve"
      pendingLabel="Resolving"
      isPending={form.isPending}
      error={form.error}
      onSubmit={form.onSubmit}
      onCancel={onCancel}
    >
      <TextField
        label="Resolved by"
        autoComplete="name"
        error={form.errors.actor?.message}
        {...form.register('actor')}
      />
      <TextAreaField
        label="Root cause"
        hint="What failed and why. This feeds the RCA and recurring-incident analysis."
        error={form.errors.rootCause?.message}
        {...form.register('rootCause')}
      />
    </ActionForm>
  );
}
