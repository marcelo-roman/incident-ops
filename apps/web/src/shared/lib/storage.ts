export function readStored(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

export function writeStored(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    return;
  }
}

export interface KeyValueStorage {
  read: (key: string) => string | null;
  write: (key: string, value: string) => void;
  remove: (key: string) => void;
}

export const tabStorage: KeyValueStorage = {
  read: (key) => {
    try {
      return window.sessionStorage.getItem(key);
    } catch {
      return null;
    }
  },
  write: (key, value) => {
    try {
      window.sessionStorage.setItem(key, value);
    } catch {
      return;
    }
  },
  remove: (key) => {
    try {
      window.sessionStorage.removeItem(key);
    } catch {
      return;
    }
  },
};
