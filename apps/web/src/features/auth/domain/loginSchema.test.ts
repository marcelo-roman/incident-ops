import { describe, expect, it } from 'vitest';
import { loginSchema } from './loginSchema';

describe('loginSchema', () => {
  it('trims the username and keeps the password as typed', () => {
    expect(loginSchema.parse({ username: '  demo ', password: ' secret ' })).toEqual({
      username: 'demo',
      password: ' secret ',
    });
  });

  it('requires both fields', () => {
    const result = loginSchema.safeParse({ username: ' ', password: '' });

    expect(result.success).toBe(false);
    expect(result.error?.issues.map((issue) => issue.message)).toEqual([
      'Enter your username.',
      'Enter your password.',
    ]);
  });
});
