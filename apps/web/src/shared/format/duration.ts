const second = 1000;
const minute = 60 * second;
const hour = 60 * minute;
const day = 24 * hour;

function pad(value: number): string {
  return String(value).padStart(2, '0');
}

export function formatCountdown(ms: number): string {
  if (ms < 0) {
    return `-${formatSpan(-ms)}`;
  }
  return formatSpan(ms);
}

function formatSpan(ms: number): string {
  const days = Math.floor(ms / day);
  const hours = Math.floor((ms % day) / hour);
  const minutes = Math.floor((ms % hour) / minute);
  const seconds = Math.floor((ms % minute) / second);
  if (days > 0) {
    return `${String(days)}d ${pad(hours)}h`;
  }
  if (hours > 0) {
    return `${String(hours)}h ${pad(minutes)}m`;
  }
  return `${String(minutes)}m ${pad(seconds)}s`;
}

export function formatMinutes(minutes: number | null): string {
  if (minutes === null) {
    return '—';
  }
  if (minutes < 60) {
    return `${String(Math.round(minutes))}m`;
  }
  if (minutes < 60 * 24) {
    return `${(minutes / 60).toFixed(1)}h`;
  }
  return `${(minutes / 60 / 24).toFixed(1)}d`;
}

export function formatPercent(percentage: number | null): string {
  if (percentage === null) {
    return '—';
  }
  return `${percentage.toFixed(1)}%`;
}

export function formatRelative(iso: string, now: number): string {
  const elapsed = now - Date.parse(iso);
  if (elapsed < minute) {
    return 'just now';
  }
  return `${formatAgo(elapsed)} ago`;
}

function formatAgo(ms: number): string {
  if (ms < hour) {
    return `${String(Math.floor(ms / minute))}m`;
  }
  if (ms < day) {
    return `${String(Math.floor(ms / hour))}h`;
  }
  return `${String(Math.floor(ms / day))}d`;
}
