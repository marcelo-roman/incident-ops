import { severities, type Severity } from '../../incidents';

export interface MetricsSummary {
  openBySeverity: Record<Severity, number>;
  slaCompliance30d: number;
  mtta30dMinutes: number | null;
  mttr30dMinutes: number | null;
  breachedOpen: number;
}

export interface SeverityShare {
  severity: Severity;
  count: number;
  share: number;
}

export function totalOpen(summary: Pick<MetricsSummary, 'openBySeverity'>): number {
  return severities.reduce((sum, severity) => sum + summary.openBySeverity[severity], 0);
}

export function severityShares(summary: Pick<MetricsSummary, 'openBySeverity'>): SeverityShare[] {
  const total = totalOpen(summary);
  return severities.map((severity) => {
    const count = summary.openBySeverity[severity];
    return { severity, count, share: shareOf(count, total) };
  });
}

function shareOf(count: number, total: number): number {
  if (total === 0) {
    return 0;
  }
  return count / total;
}
