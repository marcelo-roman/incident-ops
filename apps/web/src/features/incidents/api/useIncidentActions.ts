import { useMutation, useQueryClient } from '@tanstack/react-query';
import { incidentsApi } from './client';
import type { AcknowledgeInput, MitigateInput, NoteInput, ResolveInput } from './incidentsApi';
import { incidentKeys } from './incidentKeys';
import type { Incident } from '../domain/incident';
import { applyIncidentChanged } from './incidentCache';

function useIncidentTransition<TInput>(id: string, send: (id: string, input: TInput) => Promise<Incident>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: TInput) => send(id, input),
    onSuccess: (incident) => {
      applyIncidentChanged(queryClient, incident);
      void queryClient.invalidateQueries({ queryKey: incidentKeys.detail(id) });
    },
  });
}

export function useAcknowledgeIncident(id: string) {
  return useIncidentTransition<AcknowledgeInput>(id, incidentsApi.acknowledge);
}

export function useMitigateIncident(id: string) {
  return useIncidentTransition<MitigateInput>(id, incidentsApi.mitigate);
}

export function useResolveIncident(id: string) {
  return useIncidentTransition<ResolveInput>(id, incidentsApi.resolve);
}

export function useAddNote(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: NoteInput) => incidentsApi.addNote(id, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: incidentKeys.detail(id) });
    },
  });
}
