import { describe, expect, it } from 'vitest';
import { parseIncidentFilters } from './useIncidentFilters';

describe('parseIncidentFilters', () => {
  it('reads known values from the address', () => {
    const params = new URLSearchParams(
      'status=Acknowledged&severity=Sev2&serviceId=search&source=Alertmanager&open=true',
    );

    expect(parseIncidentFilters(params)).toEqual({
      status: 'Acknowledged',
      severity: 'Sev2',
      serviceId: 'search',
      source: 'Alertmanager',
      open: true,
    });
  });

  it('ignores values outside the contract', () => {
    expect(parseIncidentFilters(new URLSearchParams('status=Closed&severity=P1&source=Pager'))).toEqual({});
  });
});
