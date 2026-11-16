// Small formatting helpers, pure and shared by the pages.

const timeFormat = new Intl.DateTimeFormat('de-AT', {
  hour: '2-digit',
  minute: '2-digit',
  timeZone: 'Europe/Vienna',
})

const dateTimeFormat = new Intl.DateTimeFormat('de-AT', {
  day: '2-digit',
  month: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  timeZone: 'Europe/Vienna',
})

export function formatTime(iso: string): string {
  const date = new Date(iso)
  return Number.isNaN(date.getTime()) ? '--:--' : timeFormat.format(date)
}

export function formatDateTime(iso: string): string {
  const date = new Date(iso)
  return Number.isNaN(date.getTime()) ? '' : dateTimeFormat.format(date)
}

export type DelayLevel = 'ontime' | 'minor' | 'major'

export function delayLevel(minutes: number): DelayLevel {
  if (minutes <= 0) return 'ontime'
  return minutes <= 2 ? 'minor' : 'major'
}

export function delayLabel(minutes: number): string {
  return minutes <= 0 ? 'on time' : `+${minutes} min`
}

/** "Foo.Bar.StopGrain" becomes "StopGrain". */
export function shortTypeName(name: string): string {
  const last = name.slice(name.lastIndexOf('.') + 1)
  return last.length > 0 ? last : name
}

/** Share of cache hits among all lookups, 0..1. 0 when nothing happened yet. */
export function cacheHitRatio(calls: number, cacheHits: number): number {
  const total = calls + cacheHits
  return total <= 0 ? 0 : cacheHits / total
}

const LINE_COLORS = ['#d6232f', '#1f6fb2', '#2a8a5b', '#8a4fb3', '#c26a00', '#0f7f8a']

/** A stable colour per line number, tram red first for the low numbers. */
export function lineColor(line: string): string {
  const n = Number.parseInt(line, 10)
  if (Number.isFinite(n) && n >= 1 && n <= 4) return LINE_COLORS[0] ?? '#d6232f'
  let hash = 0
  for (const ch of line) hash = (hash * 31 + ch.charCodeAt(0)) >>> 0
  return LINE_COLORS[1 + (hash % (LINE_COLORS.length - 1))] ?? '#1f6fb2'
}
