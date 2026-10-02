import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { sessionStore } from '../session/activeSession';

export function useSignOut(): () => void {
  const queryClient = useQueryClient();
  return useCallback(() => {
    sessionStore.signOut();
    queryClient.clear();
  }, [queryClient]);
}
