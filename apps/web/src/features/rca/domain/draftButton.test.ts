import { describe, expect, it } from 'vitest';
import { draftButtonLabel } from './draftButton';

describe('draftButtonLabel', () => {
  it('follows the request lifecycle', () => {
    expect(draftButtonLabel({ isPending: false, hasDraft: false })).toBe('Draft RCA with AI');
    expect(draftButtonLabel({ isPending: true, hasDraft: false })).toBe('Drafting RCA');
    expect(draftButtonLabel({ isPending: false, hasDraft: true })).toBe('Draft again');
  });
});
