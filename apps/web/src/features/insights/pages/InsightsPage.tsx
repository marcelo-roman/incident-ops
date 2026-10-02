import { PageHeader } from '../../../shared/ui/PageHeader';
import { useInsightWindow } from '../hooks/useInsightWindow';
import { AnomaliesPanel } from '../components/AnomaliesPanel';
import styles from '../components/Insights.module.css';
import { KpiOverview } from '../components/KpiOverview';
import { RcaQueuePanel } from '../components/RcaQueuePanel';
import { RecurringClustersPanel } from '../components/RecurringClustersPanel';
import { WindowPicker } from '../components/WindowPicker';

export function InsightsPage() {
  const { days, setDays } = useInsightWindow();
  return (
    <>
      <PageHeader
        title="Insights"
        summary="How response is trending, which incidents keep coming back, and where volume is unusual."
        actions={<WindowPicker value={days} onChange={setDays} />}
      />
      <div className={styles.stack}>
        <KpiOverview days={days} />
        <div className={styles.pair}>
          <RecurringClustersPanel days={days} />
          <AnomaliesPanel days={days} />
        </div>
        <RcaQueuePanel />
      </div>
    </>
  );
}
