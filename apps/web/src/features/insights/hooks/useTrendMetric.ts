import { useState } from 'react';
import type { KpiTrendMetric, WeeklyKpi } from '../domain/insights';
import { trendMetricDefinitions, trendSeries } from '../domain/trendMetrics';

export function useTrendMetric(weeks: WeeklyKpi[]) {
  const [metric, setMetric] = useState<KpiTrendMetric>('incidents');
  return {
    metric,
    setMetric,
    definition: trendMetricDefinitions[metric],
    points: trendSeries(weeks, metric),
  };
}
