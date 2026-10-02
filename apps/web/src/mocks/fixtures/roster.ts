import type { OnCallRoster } from '../../features/oncall';

export function rosterFixture(now: number): OnCallRoster {
  const weekStart = new Date(now);
  weekStart.setUTCDate(weekStart.getUTCDate() - ((weekStart.getUTCDay() + 6) % 7));
  weekStart.setUTCHours(14, 30, 0, 0);
  return {
    weekStart: weekStart.toISOString(),
    primary: 'Ana Ribeiro',
    secondary: 'Daniel Okafor',
    lead: 'Priya Natarajan',
  };
}
