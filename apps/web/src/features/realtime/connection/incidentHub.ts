import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr';
import { backoffRetryPolicy } from './retryPolicy';

export const hubMethods = {
  incidentChanged: 'IncidentChanged',
  timelineAppended: 'TimelineAppended',
} as const;

export function createIncidentHub(apiBaseUrl: string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${apiBaseUrl}/hubs/incidents`)
    .withAutomaticReconnect(backoffRetryPolicy)
    .configureLogging(LogLevel.Warning)
    .build();
}
