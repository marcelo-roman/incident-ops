import { setupServer } from 'msw/node';
import { createHandlers } from './handlers';
import { MockIncidentStore } from './store';

let store = new MockIncidentStore(Date.now());

export function resetMockStore(now = Date.now()): MockIncidentStore {
  store = new MockIncidentStore(now);
  return store;
}

export const server = setupServer(...createHandlers(() => store));
