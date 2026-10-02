export interface OnCallRoster {
  weekStart: string;
  primary: string;
  secondary: string;
  lead: string;
}

export interface EscalationTier {
  level: number;
  role: string;
  engineer: string;
}

export function escalationTiers(roster: OnCallRoster): EscalationTier[] {
  return [
    { level: 1, role: 'Primary', engineer: roster.primary },
    { level: 2, role: 'Secondary', engineer: roster.secondary },
    { level: 3, role: 'Engineering lead', engineer: roster.lead },
  ];
}
