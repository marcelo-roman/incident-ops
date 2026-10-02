import { useOperator } from '../../../shared/operator/useOperator';
import { useAcknowledgeIncident, useAddNote, useMitigateIncident, useResolveIncident } from '../api/useIncidentActions';
import { acknowledgeSchema, mitigateSchema, noteSchema, resolveSchema } from '../domain/actionSchemas';
import { useActionForm } from './useActionForm';

type OnDone = (message: string) => void;

export function useAcknowledgeForm(incidentId: string, onDone: OnDone) {
  const { operator } = useOperator();
  return useActionForm({
    schema: acknowledgeSchema,
    defaultValues: { actor: operator },
    mutation: useAcknowledgeIncident(incidentId),
    successMessage: 'Incident acknowledged. The acknowledge clock has stopped.',
    onDone,
  });
}

export function useMitigateForm(incidentId: string, onDone: OnDone) {
  const { operator } = useOperator();
  return useActionForm({
    schema: mitigateSchema,
    defaultValues: { actor: operator, note: '' },
    mutation: useMitigateIncident(incidentId),
    successMessage: 'Incident marked as mitigated.',
    onDone,
  });
}

export function useResolveForm(incidentId: string, onDone: OnDone) {
  const { operator } = useOperator();
  return useActionForm({
    schema: resolveSchema,
    defaultValues: { actor: operator, rootCause: '' },
    mutation: useResolveIncident(incidentId),
    successMessage: 'Incident resolved. The SLA outcome is now final.',
    onDone,
  });
}

export function useNoteForm(incidentId: string, onDone: OnDone) {
  const { operator } = useOperator();
  return useActionForm({
    schema: noteSchema,
    defaultValues: { actor: operator, message: '' },
    mutation: useAddNote(incidentId),
    successMessage: 'Note added to the timeline.',
    onDone,
  });
}
