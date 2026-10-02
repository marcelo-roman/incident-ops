import { describe, expect, it } from 'vitest';
import { anIncident } from '../testing/factories';
import { filterBySource, serverFilters } from './incidentFilters';

describe('incident filters', () => {
  it('keeps source out of the server query', () => {
    expect(serverFilters({ status: 'Triggered', source: 'Alertmanager', open: true })).toEqual({
      status: 'Triggered',
      open: true,
    });
  });

  it('filters by source on the client', () => {
    const manual = anIncident({ id: 'manual' });
    const alerted = anIncident({ id: 'alerted', source: 'AzureMonitor' });

    expect(filterBySource([manual, alerted], 'AzureMonitor')).toEqual([alerted]);
    expect(filterBySource([manual, alerted], undefined)).toEqual([manual, alerted]);
  });
});
