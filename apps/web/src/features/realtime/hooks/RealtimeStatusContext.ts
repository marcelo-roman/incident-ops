import { createContext, useContext } from 'react';
import type { ConnectionStatus } from '../connection/connectionStatus';

export const RealtimeStatusContext = createContext<ConnectionStatus>('disabled');

export function useRealtimeStatus(): ConnectionStatus {
  return useContext(RealtimeStatusContext);
}
