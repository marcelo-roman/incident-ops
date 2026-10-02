import { useSyncExternalStore } from 'react';
import { readClock, subscribeToClock } from './clockStore';

export function useNow(): number {
  return useSyncExternalStore(subscribeToClock, readClock);
}
