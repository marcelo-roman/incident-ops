import { useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import {
  applyIncidentChanged,
  applyTimelineAppended,
  type Incident,
  refreshAfterReconnect,
  type TimelineEntry,
} from '../../incidents';
import type { ConnectionStatus } from '../connection/connectionStatus';
import { ConnectionSupervisor } from '../connection/connectionSupervisor';
import { createIncidentHub, hubMethods } from '../connection/incidentHub';

export function useIncidentHub(
  apiBaseUrl: string,
  enabled: boolean,
  accessTokenFactory: () => string,
): ConnectionStatus {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<ConnectionStatus>('connecting');

  useEffect(() => {
    if (!enabled) {
      return undefined;
    }
    const connection = createIncidentHub(apiBaseUrl, accessTokenFactory);
    connection.on(hubMethods.incidentChanged, (incident: Incident) => {
      applyIncidentChanged(queryClient, incident);
    });
    connection.on(hubMethods.timelineAppended, (entry: TimelineEntry) => {
      applyTimelineAppended(queryClient, entry);
    });
    const supervisor = new ConnectionSupervisor(connection, (next) => {
      setStatus(next);
      if (next === 'connected') {
        refreshAfterReconnect(queryClient);
      }
    });
    supervisor.start();
    return () => {
      void supervisor.stop();
    };
  }, [apiBaseUrl, enabled, accessTokenFactory, queryClient]);

  if (!enabled) {
    return 'disabled';
  }
  return status;
}
