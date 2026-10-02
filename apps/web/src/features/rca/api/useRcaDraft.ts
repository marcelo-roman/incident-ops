import { useMutation } from '@tanstack/react-query';
import { rcaApi } from './rcaApi';

export function useRcaDraft() {
  return useMutation({
    mutationFn: (incidentId: string) => rcaApi.draft(incidentId),
  });
}
