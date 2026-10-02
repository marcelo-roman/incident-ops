import type { Severity } from '../../incidents';

export interface AnalysisWindow {
  start: string;
  end: string;
  days: number;
}

export interface DurationStats {
  samples: number;
  medianMinutes: number | null;
  p90Minutes: number | null;
}

export interface SlaCompliance {
  evaluated: number;
  breached: number;
  acknowledgePct: number | null;
  resolvePct: number | null;
  overallPct: number | null;
}

export interface KpiSummary {
  incidents: number;
  mtta: DurationStats;
  mttr: DurationStats;
  sla: SlaCompliance;
}

export interface KpiGroup extends KpiSummary {
  serviceId?: string | null;
  severity?: Severity | null;
}

export interface WeeklyKpi {
  weekStart: string;
  incidents: number;
  mttaMedianMinutes: number | null;
  mttrMedianMinutes: number | null;
  slaCompliancePct: number | null;
}

export interface KpiReport {
  window: AnalysisWindow;
  overall: KpiSummary;
  bySeverity: KpiGroup[];
  byService: KpiGroup[];
  weekly: WeeklyKpi[];
}

export interface ServiceCount {
  serviceId: string;
  count: number;
}

export interface RecurringCluster {
  label: string;
  terms: string[];
  count: number;
  cohesion: number;
  services: ServiceCount[];
  sampleTitles: string[];
  firstSeen: string;
  lastSeen: string;
}

export interface RecurringReport {
  window: AnalysisWindow;
  incidentsAnalyzed: number;
  clusters: RecurringCluster[];
}

export interface VolumeAnomaly {
  serviceId: string;
  weekStart: string;
  count: number;
  baselineMean: number;
  baselineStd: number;
  zScore: number;
}

export interface AnomalyReport {
  window: AnalysisWindow;
  method: string;
  threshold: number;
  anomalies: VolumeAnomaly[];
}

export const kpiTrendMetrics = ['incidents', 'mttaMedianMinutes', 'mttrMedianMinutes', 'slaCompliancePct'] as const;
export type KpiTrendMetric = (typeof kpiTrendMetrics)[number];

export const insightWindows = [30, 90, 180] as const;
export type InsightWindow = (typeof insightWindows)[number];

export const defaultInsightWindow: InsightWindow = 90;

export function parseInsightWindow(value: string | null): InsightWindow {
  const days = Number(value);
  return insightWindows.find((window) => window === days) ?? defaultInsightWindow;
}
