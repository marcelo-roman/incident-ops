import { setupWorker } from 'msw/browser';
import { createHandlers } from './handlers';
import { MockIncidentStore } from './store';

const store = new MockIncidentStore(Date.now());

export const worker = setupWorker(...createHandlers(() => store));
