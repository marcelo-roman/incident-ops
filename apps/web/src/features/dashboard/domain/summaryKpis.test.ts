import { describe, expect, it } from 'vitest';
import { summaryKpis } from './summaryKpis';

describe('summaryKpis', () => {
  it('turns the metrics summary into tiles and flags breaches', () => {
    const tiles = summaryKpis({
      openBySeverity: { Sev1: 1, Sev2: 2, Sev3: 0, Sev4: 1 },
      breachedOpen: 1,
      slaCompliance30d: 95.25,
      mtta30dMinutes: 8,
      mttr30dMinutes: 125,
    });

    expect(tiles).toEqual([
      { label: 'Open incidents', value: '4' },
      { label: 'Open past SLA', value: '1', alert: true },
      { label: 'SLA compliance, 30 days', value: '95.3%' },
      { label: 'Mean time to acknowledge', value: '8m' },
      { label: 'Mean time to resolve', value: '2.1h' },
    ]);
  });
});
