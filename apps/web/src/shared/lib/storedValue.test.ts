import { describe, expect, it, vi } from 'vitest';
import { createStoredValue } from './storedValue';

describe('createStoredValue', () => {
  it('persists writes and notifies subscribers', () => {
    const value = createStoredValue('test.key', 'fallback');
    const listener = vi.fn();
    const unsubscribe = value.subscribe(listener);

    value.write('Ana');
    unsubscribe();
    value.write('Dan');

    expect(value.read()).toBe('Dan');
    expect(window.localStorage.getItem('test.key')).toBe('Dan');
    expect(listener).toHaveBeenCalledTimes(1);
  });

  it('starts from the fallback when nothing is stored', () => {
    expect(createStoredValue('missing.key', 'fallback').read()).toBe('fallback');
  });
});
