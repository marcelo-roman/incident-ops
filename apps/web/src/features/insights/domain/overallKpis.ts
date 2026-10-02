import type { Kpi } from '../../../shared/ui/KpiStrip';
import { formatMinutes, formatPercent } from '../../../shared/format/duration';
import type { KpiSummary } from './insights';

export function overallKpis(summary: KpiSummary): Kpi[] {
  return [
    { label: 'Incidents', value: String(summary.incidents) },
    { label: 'Median time to acknowledge', value: formatMinutes(summary.mtta.medianMinutes) },
    { label: 'Median time to resolve', value: formatMinutes(summary.mttr.medianMinutes) },
    { label: 'P90 time to resolve', value: formatMinutes(summary.mttr.p90Minutes) },
    { label: 'SLA compliance', value: formatPercent(summary.sla.overallPct) },
    { label: 'SLA breaches', value: String(summary.sla.breached), alert: summary.sla.breached > 0 },
  ];
}
