const levelTargets: Readonly<Record<number, string>> = {
  1: 'Primary on call',
  2: 'Secondary on call',
  3: 'Engineering lead',
};

export const maxEscalationLevel = 3;

export function describeEscalationLevel(level: number): string {
  const bounded = Math.min(Math.max(level, 1), maxEscalationLevel);
  return `Level ${String(bounded)}, ${levelTargets[bounded] ?? ''}`;
}
