import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ConnectionStatus } from './connectionStatus';
import { ConnectionSupervisor, type SupervisedConnection } from './connectionSupervisor';

class FakeConnection implements SupervisedConnection {
  readonly start = vi.fn<() => Promise<void>>();
  readonly stop = vi.fn(() => Promise.resolve());
  reconnecting: () => void = () => undefined;
  reconnected: () => void = () => undefined;
  closed: () => void = () => undefined;

  onreconnecting(callback: () => void): void {
    this.reconnecting = callback;
  }

  onreconnected(callback: () => void): void {
    this.reconnected = callback;
  }

  onclose(callback: () => void): void {
    this.closed = callback;
  }
}

describe('ConnectionSupervisor', () => {
  let connection: FakeConnection;
  let statuses: ConnectionStatus[];
  let supervisor: ConnectionSupervisor;

  beforeEach(() => {
    vi.useFakeTimers();
    connection = new FakeConnection();
    statuses = [];
    supervisor = new ConnectionSupervisor(
      connection,
      (status) => statuses.push(status),
      (attempt) => 1_000 * (attempt + 1),
    );
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('reports connected after a successful start', async () => {
    connection.start.mockResolvedValue();

    supervisor.start();
    await vi.runOnlyPendingTimersAsync();

    expect(statuses).toEqual(['connecting', 'connected']);
  });

  it('retries a failed initial start with backoff', async () => {
    connection.start
      .mockRejectedValueOnce(new Error('down'))
      .mockRejectedValueOnce(new Error('down'))
      .mockResolvedValue();

    supervisor.start();
    await vi.advanceTimersByTimeAsync(0);
    expect(statuses).toEqual(['connecting', 'offline']);

    await vi.advanceTimersByTimeAsync(1_000);
    expect(statuses.at(-1)).toBe('offline');

    await vi.advanceTimersByTimeAsync(2_000);
    expect(statuses.at(-1)).toBe('connected');
    expect(connection.start).toHaveBeenCalledTimes(3);
  });

  it('mirrors the automatic reconnect lifecycle', async () => {
    connection.start.mockResolvedValue();
    supervisor.start();
    await vi.advanceTimersByTimeAsync(0);

    connection.reconnecting();
    connection.reconnected();

    expect(statuses.slice(-2)).toEqual(['reconnecting', 'connected']);
  });

  it('restarts when the connection closes unexpectedly', async () => {
    connection.start.mockResolvedValue();
    supervisor.start();
    await vi.advanceTimersByTimeAsync(0);

    connection.closed();
    await vi.advanceTimersByTimeAsync(1_000);

    expect(connection.start).toHaveBeenCalledTimes(2);
    expect(statuses.at(-1)).toBe('connected');
  });

  it('stops retrying after stop', async () => {
    connection.start.mockRejectedValue(new Error('down'));
    supervisor.start();
    await vi.advanceTimersByTimeAsync(0);

    await supervisor.stop();
    await vi.advanceTimersByTimeAsync(60_000);

    expect(connection.start).toHaveBeenCalledTimes(1);
    expect(connection.stop).toHaveBeenCalled();
  });
});
