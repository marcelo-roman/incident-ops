import { describe, expect, it } from 'vitest';
import { panelActionLabels, panelActions } from './panelActions';

describe('panelActions', () => {
  it('always allows notes in addition to the status transitions', () => {
    expect(panelActions('Acknowledged')).toEqual(['mitigate', 'resolve', 'note']);
    expect(panelActions('Resolved')).toEqual(['note']);
  });

  it('labels every action', () => {
    expect(panelActions('Triggered').map((action) => panelActionLabels[action])).toEqual([
      'Acknowledge',
      'Mitigate',
      'Resolve',
      'Add note',
    ]);
  });
});
