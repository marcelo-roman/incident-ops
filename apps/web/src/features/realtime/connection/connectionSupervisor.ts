import type { ConnectionStatus } from './connectionStatus';
import { backoffDelay } from './retryPolicy';

export interface SupervisedConnection {
  start(): Promise<void>;
  stop(): Promise<void>;
  onreconnecting(callback: () => void): void;
  onreconnected(callback: () => void): void;
  onclose(callback: () => void): void;
}

export type StatusListener = (status: ConnectionStatus) => void;

function statusForAttempt(attempt: number): ConnectionStatus {
  if (attempt === 0) {
    return 'connecting';
  }
  return 'reconnecting';
}

export class ConnectionSupervisor {
  private readonly connection: SupervisedConnection;
  private readonly onStatus: StatusListener;
  private readonly delay: (attempt: number) => number;
  private attempt = 0;
  private stopped = false;
  private retryTimer: ReturnType<typeof setTimeout> | undefined;

  constructor(connection: SupervisedConnection, onStatus: StatusListener, delay = backoffDelay) {
    this.connection = connection;
    this.onStatus = onStatus;
    this.delay = delay;
    connection.onreconnecting(() => {
      this.onStatus('reconnecting');
    });
    connection.onreconnected(() => {
      this.markConnected();
    });
    connection.onclose(() => {
      this.scheduleRetry();
    });
  }

  start(): void {
    this.stopped = false;
    void this.connect();
  }

  async stop(): Promise<void> {
    this.stopped = true;
    clearTimeout(this.retryTimer);
    await this.connection.stop();
  }

  private async connect(): Promise<void> {
    this.onStatus(statusForAttempt(this.attempt));
    try {
      await this.connection.start();
    } catch {
      this.scheduleRetry();
      return;
    }
    if (this.stopped) {
      return;
    }
    this.markConnected();
  }

  private scheduleRetry(): void {
    if (this.stopped) {
      return;
    }
    this.onStatus('offline');
    const wait = this.delay(this.attempt);
    this.attempt += 1;
    this.retryTimer = setTimeout(() => {
      void this.connect();
    }, wait);
  }

  private markConnected(): void {
    this.attempt = 0;
    this.onStatus('connected');
  }
}
