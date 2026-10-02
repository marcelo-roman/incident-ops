import { QueryClientProvider } from '@tanstack/react-query';
import { act, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { incidentKeys, type IncidentDetail } from '../../incidents';
import { anIncident, anIncidentDetail } from '../../incidents/testing';
import { createTestQueryClient } from '../../../shared/test/render';
import { useRealtimeStatus } from '../hooks/RealtimeStatusContext';
import { RealtimeProvider } from './RealtimeProvider';

type Handler = (payload: unknown) => void;

const hub = vi.hoisted(() => ({
  handlers: new Map<string, Handler>(),
  start: vi.fn(() => Promise.resolve()),
}));

vi.mock('../connection/incidentHub', () => ({
  hubMethods: { incidentChanged: 'IncidentChanged', timelineAppended: 'TimelineAppended' },
  createIncidentHub: () => ({
    on: (method: string, handler: Handler) => hub.handlers.set(method, handler),
    start: hub.start,
    stop: () => Promise.resolve(),
    onreconnecting: () => undefined,
    onreconnected: () => undefined,
    onclose: () => undefined,
  }),
}));

function StatusProbe() {
  return <p>status: {useRealtimeStatus()}</p>;
}

function renderProvider(enabled: boolean) {
  const queryClient = createTestQueryClient();
  render(
    <QueryClientProvider client={queryClient}>
      <RealtimeProvider apiBaseUrl="https://api.test" enabled={enabled}>
        <StatusProbe />
      </RealtimeProvider>
    </QueryClientProvider>,
  );
  return queryClient;
}

describe('RealtimeProvider', () => {
  beforeEach(() => {
    hub.handlers.clear();
    hub.start.mockClear();
  });

  it('stays disabled without connecting when live updates are off', () => {
    renderProvider(false);

    expect(screen.getByText('status: disabled')).toBeInTheDocument();
    expect(hub.start).not.toHaveBeenCalled();
  });

  it('connects and applies hub events to the query cache', async () => {
    const queryClient = renderProvider(true);
    queryClient.setQueryData(incidentKeys.detail('incident-1'), anIncidentDetail());

    expect(await screen.findByText('status: connected')).toBeInTheDocument();
    act(() => {
      hub.handlers.get('IncidentChanged')?.(anIncident({ status: 'Acknowledged' }));
    });

    expect(queryClient.getQueryData<IncidentDetail>(incidentKeys.detail('incident-1'))?.status).toBe('Acknowledged');
  });
});
