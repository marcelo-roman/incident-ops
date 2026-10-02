import { AsyncContent } from '../../../shared/ui/AsyncContent';
import { KpiStrip } from '../../../shared/ui/KpiStrip';
import { useMetricsSummary } from '../api/useMetricsSummary';
import { summaryKpis } from '../domain/summaryKpis';

export function MetricsStrip() {
  const query = useMetricsSummary();
  return (
    <AsyncContent query={query} loadingLabel="Loading service metrics">
      {(summary) => <KpiStrip label="Service metrics" items={summaryKpis(summary)} />}
    </AsyncContent>
  );
}
