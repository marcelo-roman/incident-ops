import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll, beforeEach } from 'vitest';
import { resetMockStore, server } from '../mocks/node';

beforeAll(() => {
  server.listen({ onUnhandledRequest: 'error' });
});

beforeEach(() => {
  resetMockStore();
  window.localStorage.clear();
});

afterEach(() => {
  cleanup();
  server.resetHandlers();
});

afterAll(() => {
  server.close();
});

class ResizeObserverStub {
  observe(): void {
    return;
  }

  unobserve(): void {
    return;
  }

  disconnect(): void {
    return;
  }
}

if (!('ResizeObserver' in globalThis)) {
  Object.defineProperty(globalThis, 'ResizeObserver', { value: ResizeObserverStub, writable: true });
}
