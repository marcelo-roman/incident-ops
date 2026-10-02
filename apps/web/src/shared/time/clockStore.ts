const tickMs = 1_000;
const listeners = new Set<() => void>();
let current = Date.now();
let timer: ReturnType<typeof setInterval> | undefined;

function tick(): void {
  current = Date.now();
  listeners.forEach((listener) => {
    listener();
  });
}

export function subscribeToClock(listener: () => void): () => void {
  listeners.add(listener);
  if (listeners.size === 1) {
    current = Date.now();
    timer = setInterval(tick, tickMs);
  }
  return () => {
    listeners.delete(listener);
    if (listeners.size === 0) {
      clearInterval(timer);
    }
  };
}

export function readClock(): number {
  return current;
}
