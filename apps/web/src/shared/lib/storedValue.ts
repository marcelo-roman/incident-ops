import { readStored, writeStored } from './storage';

export interface StoredValue {
  read: () => string;
  write: (value: string) => void;
  subscribe: (listener: () => void) => () => void;
}

export function createStoredValue(key: string, fallback: string): StoredValue {
  const listeners = new Set<() => void>();
  let value = readStored(key) ?? fallback;
  return {
    read: () => value,
    write: (next) => {
      value = next;
      writeStored(key, next);
      listeners.forEach((listener) => {
        listener();
      });
    },
    subscribe: (listener) => {
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },
  };
}
