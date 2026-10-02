const dateTimeFormat = new Intl.DateTimeFormat(undefined, {
  month: 'short',
  day: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

const timeFormat = new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit', second: '2-digit' });

const dateFormat = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' });

const shortDateFormat = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' });

export function formatDateTime(iso: string | null): string {
  if (iso === null) {
    return '—';
  }
  return dateTimeFormat.format(new Date(iso));
}

export function formatTime(iso: string): string {
  return timeFormat.format(new Date(iso));
}

export function formatDate(iso: string): string {
  return dateFormat.format(new Date(iso));
}

export function formatShortDate(iso: string): string {
  return shortDateFormat.format(new Date(iso));
}
