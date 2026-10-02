import type { ReactNode } from 'react';
import { useIncidentHub } from '../hooks/useIncidentHub';
import { RealtimeStatusContext } from '../hooks/RealtimeStatusContext';

interface RealtimeProviderProps {
  apiBaseUrl: string;
  enabled: boolean;
  children: ReactNode;
}

export function RealtimeProvider({ apiBaseUrl, enabled, children }: RealtimeProviderProps) {
  const status = useIncidentHub(apiBaseUrl, enabled);
  return <RealtimeStatusContext value={status}>{children}</RealtimeStatusContext>;
}
