import { describe, expect, it } from 'vitest';
import type { WeeklyKpi } from './insights';
import { trendMetricDefinitions, trendSeries } from './trendMetrics';

const weeks: WeeklyKpi[] = [
  { weekStart: '2026-09-14', incidents: 7, mttaMedianMinutes: 9, mttrMedianMinutes: 180, slaCompliancePct: 92.5 },
  { weekStart: '2026-09-21', incidents: 4, mttaMedianMinutes: null, mttrMedianMinutes: 95, slaCompliancePct: null },
];

describe('trend metrics', () => {
  it('projects the weekly report onto one metric', () => {
    expect(trendSeries(weeks, 'mttaMedianMinutes')).toEqual([
      { weekStart: '2026-09-14', value: 9 },
      { weekStart: '2026-09-21', value: null },
    ]);
  });

  it('formats each metric in its own unit', () => {
    expect(trendMetricDefinitions.incidents.format(7)).toBe('7');
    expect(trendMetricDefinitions.incidents.format(null)).toBe('0');
    expect(trendMetricDefinitions.mttrMedianMinutes.format(180)).toBe('3.0h');
    expect(trendMetricDefinitions.slaCompliancePct.format(92.5)).toBe('92.5%');
  });
});
