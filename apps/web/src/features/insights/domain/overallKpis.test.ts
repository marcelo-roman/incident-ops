import { describe, expect, it } from 'vitest';
import { overallKpis } from './overallKpis';

describe('overallKpis', () => {
  it('summarises the window', () => {
    const tiles = overallKpis({
      incidents: 120,
      mtta: { samples: 120, medianMinutes: 11, p90Minutes: 40 },
      mttr: { samples: 118, medianMinutes: 200, p90Minutes: null },
      sla: { evaluated: 118, breached: 0, acknowledgePct: 97, resolvePct: 92, overallPct: 91.2 },
    });

    expect(tiles.map((tile) => tile.value)).toEqual(['120', '11m', '3.3h', '—', '91.2%', '0']);
    expect(tiles.at(-1)?.alert).toBe(false);
  });
});
