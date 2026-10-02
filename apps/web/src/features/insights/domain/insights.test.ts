import { describe, expect, it } from 'vitest';
import { defaultInsightWindow, parseInsightWindow } from './insights';

describe('parseInsightWindow', () => {
  it('accepts the supported windows', () => {
    expect(parseInsightWindow('30')).toBe(30);
    expect(parseInsightWindow('180')).toBe(180);
  });

  it('falls back to the default window', () => {
    expect(parseInsightWindow(null)).toBe(defaultInsightWindow);
    expect(parseInsightWindow('45')).toBe(defaultInsightWindow);
  });
});
