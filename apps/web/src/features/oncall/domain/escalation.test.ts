import { describe, expect, it } from 'vitest';
import { describeEscalationLevel } from './escalation';

describe('describeEscalationLevel', () => {
  it('names the target of each level', () => {
    expect(describeEscalationLevel(1)).toBe('Level 1, Primary on call');
    expect(describeEscalationLevel(3)).toBe('Level 3, Engineering lead');
  });

  it('clamps out-of-range levels to the policy', () => {
    expect(describeEscalationLevel(7)).toBe('Level 3, Engineering lead');
    expect(describeEscalationLevel(0)).toBe('Level 1, Primary on call');
  });
});
