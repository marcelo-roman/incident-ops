import { AsyncContent } from '../../../shared/ui/AsyncContent';
import { KpiStrip } from '../../../shared/ui/KpiStrip';
import { useKpis } from '../api/useInsights';
import styles from './Insights.module.css';
import { KpiTrendPanel } from './KpiTrendPanel';
import { overallKpis } from '../domain/overallKpis';
import { ServiceKpiTable } from './ServiceKpiTable';

export function KpiOverview({ days }: Readonly<{ days: number }>) {
  const query = useKpis(days);
  return (
    <AsyncContent query={query} loadingLabel="Loading KPIs">
      {(report) => (
        <div className={styles.stack}>
          <KpiStrip label="Key indicators" items={overallKpis(report.overall)} />
          <KpiTrendPanel weeks={report.weekly} />
          <ServiceKpiTable groups={report.byService} />
        </div>
      )}
    </AsyncContent>
  );
}
