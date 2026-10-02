import { describe, expect, it } from 'vitest';
import { acknowledgeSchema, mitigateSchema, noteSchema, resolveSchema } from './actionSchemas';

function firstError(result: { success: boolean; error?: { issues: { message: string }[] } }): string | undefined {
  return result.error?.issues[0]?.message;
}

describe('action schemas', () => {
  it('requires a named actor', () => {
    expect(firstError(acknowledgeSchema.safeParse({ actor: ' ' }))).toBe(
      'Enter the name of the person taking this action.',
    );
    expect(acknowledgeSchema.safeParse({ actor: 'Ana Ribeiro' }).success).toBe(true);
  });

  it('trims free text before validating it', () => {
    const result = resolveSchema.safeParse({ actor: 'Ana', rootCause: '   short   ' });

    expect(firstError(result)).toBe('Describe the root cause in at least 10 characters.');
  });

  it('accepts complete payloads and returns trimmed values', () => {
    expect(mitigateSchema.parse({ actor: ' Ana ', note: ' Rolled back ' })).toEqual({
      actor: 'Ana',
      note: 'Rolled back',
    });
    expect(noteSchema.safeParse({ actor: 'Ana', message: '' }).success).toBe(false);
  });
});
