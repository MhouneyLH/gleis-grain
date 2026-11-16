import type { LiveEventMap, LiveEventType } from '../api'

export const TICKER_SIZE = 8

export type TickerKind = LiveEventType | 'chaos'

export interface TickerEntry {
  id: number
  at: number
  kind: TickerKind
  text: string
}

let nextId = 1

export function tickerEntry(kind: TickerKind, text: string, at: number = Date.now()): TickerEntry {
  return { id: nextId++, at, kind, text }
}

/** Adds the entry on top and keeps only the newest `limit` entries. */
export function pushTicker(entries: readonly TickerEntry[], entry: TickerEntry, limit = TICKER_SIZE): TickerEntry[] {
  return [entry, ...entries].slice(0, limit)
}

const plural = (n: number, one: string, many: string) => `${n} ${n === 1 ? one : many}`

/** One line of text for a live event. */
export function describeEvent<T extends LiveEventType>(
  type: T,
  payload: LiveEventMap[T],
  stopName: (stopId: string) => string,
): string {
  switch (type) {
    case 'stop.activated': {
      const p = payload as LiveEventMap['stop.activated']
      return `${stopName(p.stopId)} woke up on ${p.silo}`
    }
    case 'stop.deactivated':
      return `${stopName((payload as LiveEventMap['stop.deactivated']).stopId)} went to sleep`
    case 'stop.watchers': {
      const p = payload as LiveEventMap['stop.watchers']
      return `${stopName(p.stopId)}: ${plural(p.watchers, 'watcher', 'watchers')}`
    }
    case 'bet.placed': {
      const p = payload as LiveEventMap['bet.placed']
      return `${p.nickname} bet on line ${p.line} at ${stopName(p.stopId)}`
    }
    case 'bet.resolved': {
      const p = payload as LiveEventMap['bet.resolved']
      return `${p.nickname} scored ${plural(p.points, 'point', 'points')} (delay was ${p.actualDelayMinutes} min)`
    }
    case 'leaderboard.changed': {
      const top = (payload as LiveEventMap['leaderboard.changed']).top[0]
      return top ? `Leaderboard: ${top.nickname} leads with ${top.score}` : 'Leaderboard changed'
    }
    default:
      return String(type)
  }
}
