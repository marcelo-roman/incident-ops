import { TextAreaField, TextField } from '../../../../shared/ui/Field';
import { useNoteForm } from '../../hooks/useIncidentActionForms';
import { ActionForm } from './ActionForm';
import type { IncidentActionFormProps } from './actionProps';

export function NoteForm({ incidentId, onDone, onCancel }: Readonly<IncidentActionFormProps>) {
  const form = useNoteForm(incidentId, onDone);
  return (
    <ActionForm
      label="Add note"
      submitLabel="Add note"
      pendingLabel="Adding"
      isPending={form.isPending}
      error={form.error}
      onSubmit={form.onSubmit}
      onCancel={onCancel}
    >
      <TextField label="Author" autoComplete="name" error={form.errors.actor?.message} {...form.register('actor')} />
      <TextAreaField label="Note" error={form.errors.message?.message} {...form.register('message')} />
    </ActionForm>
  );
}
