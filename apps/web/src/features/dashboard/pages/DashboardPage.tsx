import { ButtonLink } from '../../../shared/ui/ButtonLink';
import { PageHeader } from '../../../shared/ui/PageHeader';
import styles from './Dashboard.module.css';
import { MetricsStrip } from '../components/MetricsStrip';
import { OnCallPanel } from '../../oncall';
import { OpenIncidentsBoard } from '../components/OpenIncidentsBoard';
import { SeverityMix } from '../components/SeverityMix';

export function DashboardPage() {
  return (
    <>
      <PageHeader
        title="Command overview"
        summary="Open incidents, ordered by the deadline that runs out first."
        actions={
          <ButtonLink to="/incidents/new" variant="primary">
            Declare incident
          </ButtonLink>
        }
      />
      <MetricsStrip />
      <div className={styles.layout}>
        <OpenIncidentsBoard />
        <div className={styles.side}>
          <OnCallPanel />
          <SeverityMix />
        </div>
      </div>
    </>
  );
}
