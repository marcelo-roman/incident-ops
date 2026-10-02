import type { ReactNode } from 'react';
import { useIncidentHub } from '../hooks/useIncidentHub';
import { RealtimeStatusContext } from '../hooks/RealtimeStatusContext';

interface RealtimeProviderProps {
  apiBaseUrl: string;
  enabled: boolean;
  accessTokenFactory: () => string;
  children: ReactNode;
}

export function RealtimeProvider({
  apiBaseUrl,
  enabled,
  accessTokenFactory,
  children,
}: Readonly<RealtimeProviderProps>) {
  const status = useIncidentHub(apiBaseUrl, enabled, accessTokenFactory);
  return <RealtimeStatusContext value={status}>{children}</RealtimeStatusContext>;
}
