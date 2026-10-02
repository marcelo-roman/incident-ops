import { QueryClient } from '@tanstack/react-query';
import { beforeEach, describe, expect, it } from 'vitest';
import { incidentKeys } from './incidentKeys';
import type { Incident, IncidentDetail, TimelineEntry } from '../domain/incident';
import { anIncident, anIncidentDetail } from '../testing/factories';
import { applyIncidentChanged, applyTimelineAppended } from './incidentCache';

function entry(id: string, at: string): TimelineEntry {
  return { id, incidentId: 'incident-1', at, kind: 'Note', actor: 'Ana', message: id };
}

describe('cache sync', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient();
  });

  it('patches the cached detail and keeps its timeline', () => {
    const timeline = [entry('first', '2026-10-02T12:00:00Z')];
    queryClient.setQueryData(incidentKeys.detail('incident-1'), anIncidentDetail({ timeline }));

    applyIncidentChanged(queryClient, anIncident({ status: 'Acknowledged' }));

    const detail = queryClient.getQueryData<IncidentDetail>(incidentKeys.detail('incident-1'));
    expect(detail?.status).toBe('Acknowledged');
    expect(detail?.timeline).toEqual(timeline);
  });

  it('replaces the incident in every cached list and marks lists stale', () => {
    const other = anIncident({ id: 'other' });
    const listKey = incidentKeys.list({ open: true });
    queryClient.setQueryData(listKey, [anIncident(), other]);

    applyIncidentChanged(queryClient, anIncident({ status: 'Mitigated' }));

    const list = queryClient.getQueryData<Incident[]>(listKey);
    expect(list?.map((incident) => incident.status)).toEqual(['Mitigated', 'Triggered']);
    expect(queryClient.getQueryState(listKey)?.isInvalidated).toBe(true);
  });

  it('does not create a detail entry that was never fetched', () => {
    applyIncidentChanged(queryClient, anIncident());

    expect(queryClient.getQueryData(incidentKeys.detail('incident-1'))).toBeUndefined();
  });

  it('appends timeline entries in chronological order without duplicates', () => {
    queryClient.setQueryData(
      incidentKeys.detail('incident-1'),
      anIncidentDetail({ timeline: [entry('late', '2026-10-02T12:10:00Z')] }),
    );

    applyTimelineAppended(queryClient, entry('early', '2026-10-02T12:05:00Z'));
    applyTimelineAppended(queryClient, entry('early', '2026-10-02T12:05:00Z'));

    const detail = queryClient.getQueryData<IncidentDetail>(incidentKeys.detail('incident-1'));
    expect(detail?.timeline.map((item) => item.id)).toEqual(['early', 'late']);
  });
});
