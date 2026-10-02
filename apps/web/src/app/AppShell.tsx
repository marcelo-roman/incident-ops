import { NavLink, Outlet } from 'react-router';
import { EnvironmentGate } from '../features/environment';
import { ConnectionIndicator, useRealtimeStatus } from '../features/realtime';
import { BoardIcon, ListIcon, SignalIcon, TrendIcon } from '../shared/ui/icons';
import { OperatorField } from '../shared/operator/OperatorField';
import { ThemeSwitcher } from './theme/ThemeSwitcher';
import { useTheme } from './theme/useTheme';
import { cx } from '../shared/lib/cx';
import styles from './AppShell.module.css';

const navigation = [
  { to: '/', label: 'Dashboard', icon: BoardIcon, end: true },
  { to: '/incidents', label: 'Incidents', icon: ListIcon, end: true },
  { to: '/incidents/new', label: 'Declare incident', icon: SignalIcon, end: true },
  { to: '/insights', label: 'Insights', icon: TrendIcon, end: false },
] as const;

function BrandMark() {
  return (
    <svg className={styles.mark} viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <rect width="32" height="32" rx="7" fill="var(--color-sunken)" />
      <path d="M7 22h18" stroke="var(--color-line-strong)" strokeWidth="3" strokeLinecap="round" />
      <path d="M7 22h11" stroke="var(--color-at-risk)" strokeWidth="3" strokeLinecap="round" />
      <circle cx="16" cy="12" r="4" fill="var(--color-accent)" />
    </svg>
  );
}

export function AppShell() {
  const status = useRealtimeStatus();
  const { preference, setPreference } = useTheme();
  return (
    <div className={styles.shell}>
      <a className={styles.skip} href="#main">
        Skip to content
      </a>
      <aside className={styles.rail}>
        <NavLink to="/" className={cx(styles.brand)} aria-label="Incident Ops home">
          <BrandMark />
          Incident Ops
        </NavLink>
        <nav aria-label="Primary">
          <ul className={styles.nav}>
            {navigation.map(({ to, label, icon: Icon, end }) => (
              <li key={to}>
                <NavLink to={to} end={end} className={cx(styles.link)}>
                  <Icon />
                  {label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
        <div className={styles.railFooter}>
          <ThemeSwitcher value={preference} onChange={setPreference} />
        </div>
      </aside>
      <div className={styles.workspace}>
        <header className={styles.topbar}>
          <OperatorField />
          <ConnectionIndicator status={status} />
        </header>
        <main id="main" className={styles.main} tabIndex={-1}>
          <EnvironmentGate>
            <Outlet />
          </EnvironmentGate>
        </main>
      </div>
    </div>
  );
}
