import { act, renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { adoptOperator, useOperator } from './useOperator';

describe('adoptOperator', () => {
  it('fills an empty operator and keeps a chosen one', () => {
    const { result } = renderHook(() => useOperator());
    act(() => {
      result.current.setOperator('');
    });

    act(() => {
      adoptOperator('demo');
    });
    expect(result.current.operator).toBe('demo');

    act(() => {
      result.current.setOperator('Ana Ribeiro');
      adoptOperator('demo');
    });
    expect(result.current.operator).toBe('Ana Ribeiro');
  });
});
