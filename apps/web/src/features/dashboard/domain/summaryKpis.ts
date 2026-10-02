import type { Kpi } from '../../../shared/ui/KpiStrip';
import { formatMinutes, formatPercent } from '../../../shared/format/duration';
import { totalOpen, type MetricsSummary } from './metrics';

export function summaryKpis(summary: MetricsSummary): Kpi[] {
  return [
    { label: 'Open incidents', value: String(totalOpen(summary)) },
    { label: 'Open past SLA', value: String(summary.breachedOpen), alert: summary.breachedOpen > 0 },
    { label: 'SLA compliance, 30 days', value: formatPercent(summary.slaCompliance30d) },
    { label: 'Mean time to acknowledge', value: formatMinutes(summary.mtta30dMinutes) },
    { label: 'Mean time to resolve', value: formatMinutes(summary.mttr30dMinutes) },
  ];
}
