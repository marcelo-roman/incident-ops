import { Button } from '../../../shared/ui/Button';
import { Panel } from '../../../shared/ui/Panel';
import { kpiTrendMetrics, type WeeklyKpi } from '../domain/insights';
import styles from './Insights.module.css';
import { KpiTrendChart } from './KpiTrendChart';
import { TrendDataTable } from './TrendDataTable';
import { trendMetricDefinitions } from '../domain/trendMetrics';
import { useTrendMetric } from '../hooks/useTrendMetric';

export function KpiTrendPanel({ weeks }: Readonly<{ weeks: WeeklyKpi[] }>) {
  const { metric, setMetric, definition, points } = useTrendMetric(weeks);
  return (
    <Panel
      title="Weekly trend"
      actions={
        <div className={styles.metricPicker} role="group" aria-label="Trend metric">
          {kpiTrendMetrics.map((option) => (
            <Button
              key={option}
              variant="quiet"
              aria-pressed={option === metric}
              onClick={() => {
                setMetric(option);
              }}
            >
              {trendMetricDefinitions[option].label}
            </Button>
          ))}
        </div>
      }
    >
      <KpiTrendChart points={points} metric={definition} />
      <TrendDataTable points={points} metric={definition} />
    </Panel>
  );
}
