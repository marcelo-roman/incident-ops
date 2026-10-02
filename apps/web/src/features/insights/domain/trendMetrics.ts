import { formatMinutes, formatPercent } from '../../../shared/format/duration';
import type { KpiTrendMetric, WeeklyKpi } from './insights';

export interface TrendMetricDefinition {
  label: string;
  format: (value: number | null) => string;
}

export const trendMetricDefinitions: Readonly<Record<KpiTrendMetric, TrendMetricDefinition>> = {
  incidents: { label: 'Incidents', format: (value) => String(value ?? 0) },
  mttaMedianMinutes: { label: 'Time to acknowledge', format: formatMinutes },
  mttrMedianMinutes: { label: 'Time to resolve', format: formatMinutes },
  slaCompliancePct: { label: 'SLA compliance', format: formatPercent },
};

export interface TrendPoint {
  weekStart: string;
  value: number | null;
}

export function trendSeries(weeks: WeeklyKpi[], metric: KpiTrendMetric): TrendPoint[] {
  return weeks.map((week) => ({ weekStart: week.weekStart, value: week[metric] }));
}
