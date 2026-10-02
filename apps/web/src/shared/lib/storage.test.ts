import { describe, expect, it, vi } from 'vitest';
import { tabStorage } from './storage';

describe('tabStorage', () => {
  it('reads, writes and removes values in session storage', () => {
    tabStorage.write('test.tab', 'value');

    expect(tabStorage.read('test.tab')).toBe('value');
    expect(window.sessionStorage.getItem('test.tab')).toBe('value');

    tabStorage.remove('test.tab');

    expect(tabStorage.read('test.tab')).toBeNull();
  });

  it('degrades quietly when session storage is unavailable', () => {
    const failure = () => {
      throw new Error('blocked');
    };
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(failure);
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(failure);
    vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(failure);

    expect(tabStorage.read('test.tab')).toBeNull();
    expect(() => {
      tabStorage.write('test.tab', 'value');
      tabStorage.remove('test.tab');
    }).not.toThrow();
  });
});
