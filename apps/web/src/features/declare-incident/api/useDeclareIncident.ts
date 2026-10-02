import { useMutation, useQueryClient } from '@tanstack/react-query';
import { incidentsApi, invalidateIncidentViews, type DeclareIncidentInput } from '../../incidents';

export function useDeclareIncident() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: DeclareIncidentInput) => incidentsApi.declare(input),
    onSuccess: () => {
      invalidateIncidentViews(queryClient);
    },
  });
}
