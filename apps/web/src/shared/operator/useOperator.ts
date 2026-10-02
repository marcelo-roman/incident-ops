import { useSyncExternalStore } from 'react';
import { createStoredValue } from '../lib/storedValue';

const operatorStore = createStoredValue('incident-ops.operator', '');

export function useOperator() {
  const operator = useSyncExternalStore(operatorStore.subscribe, operatorStore.read);
  return { operator, setOperator: operatorStore.write };
}
