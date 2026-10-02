import { describe, expect, it } from 'vitest';
import { availableActions, canTransition } from './transitions';

describe('canTransition', () => {
  it.each([
    ['Triggered', 'Acknowledged'],
    ['Triggered', 'Mitigated'],
    ['Triggered', 'Resolved'],
    ['Acknowledged', 'Mitigated'],
    ['Acknowledged', 'Resolved'],
    ['Mitigated', 'Resolved'],
  ] as const)('allows %s to %s', (from, to) => {
    expect(canTransition(from, to)).toBe(true);
  });

  it.each([
    ['Acknowledged', 'Triggered'],
    ['Mitigated', 'Acknowledged'],
    ['Resolved', 'Triggered'],
    ['Resolved', 'Resolved'],
  ] as const)('rejects %s to %s', (from, to) => {
    expect(canTransition(from, to)).toBe(false);
  });
});

describe('availableActions', () => {
  it('offers only the transitions the contract allows', () => {
    expect(availableActions('Triggered')).toEqual(['acknowledge', 'mitigate', 'resolve']);
    expect(availableActions('Acknowledged')).toEqual(['mitigate', 'resolve']);
    expect(availableActions('Mitigated')).toEqual(['resolve']);
    expect(availableActions('Resolved')).toEqual([]);
  });
});
