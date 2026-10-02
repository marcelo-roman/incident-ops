import type { Incident, SlaState } from '../domain/incident';
import { acknowledgeClock, liveSlaState, resolveClock, type SlaClock } from '../domain/sla';
import { useNow } from '../../../shared/time/useNow';

export interface SlaClocks {
  acknowledge: SlaClock | null;
  resolve: SlaClock | null;
  state: SlaState;
}

export function useSlaClocks(incident: Incident): SlaClocks {
  const now = useNow();
  return {
    acknowledge: acknowledgeClock(incident, now),
    resolve: resolveClock(incident, now),
    state: liveSlaState(incident, now),
  };
}
