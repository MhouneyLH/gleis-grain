// The contract with the host, see docs/api.md. All types and calls live here.
import { request } from './http'

export { ApiError } from './http'

// ---- Stops ----------------------------------------------------------------

export interface Stop {
  id: string
  name: string
  latitude: number
  longitude: number
}

export interface Departure {
  tripId: string
  line: string
  direction: string
  plannedAt: string
  delayMinutes: number
  platform: string | null
}

export interface StopBoard extends Stop {
  watchers: number
  refreshedAt: string
  departures: Departure[]
}

// ---- Alerts ---------------------------------------------------------------

export interface Alert {
  id: string
  title: string
  text: string
  lines: string[]
  stopIds: string[]
  firstSeenAt: string
}

// ---- Game -----------------------------------------------------------------

export interface OpenBet {
  tripId: string
  stopId: string
  line: string
  plannedAt: string
  guessedDelayMinutes: number
}

export interface Player {
  nickname: string
  score: number
  watchedStopId: string | null
  openBet: OpenBet | null
}

export interface LeaderboardRow {
  nickname: string
  score: number
}

// ---- Cluster --------------------------------------------------------------

export interface Silo {
  name: string
  address: string
  status: string
}

export interface Activation {
  stopId: string
  silo: string
  /** Not in docs/api.md yet; used when the host sends it. */
  watchers?: number
}

export interface ClusterStats {
  ingestion: { calls: number; cacheHits: number }
  activeGrains: Record<string, number>
}

// ---- Live events ----------------------------------------------------------

export interface LiveEventMap {
  'stop.activated': { stopId: string; silo: string }
  'stop.deactivated': { stopId: string }
  'stop.watchers': { stopId: string; watchers: number }
  'bet.placed': { nickname: string; stopId: string; line: string }
  'bet.resolved': { nickname: string; tripId: string; actualDelayMinutes: number; points: number }
  'leaderboard.changed': { top: LeaderboardRow[] }
}

export type LiveEventType = keyof LiveEventMap

export const liveEventTypes: readonly LiveEventType[] = [
  'stop.activated',
  'stop.deactivated',
  'stop.watchers',
  'bet.placed',
  'bet.resolved',
  'leaderboard.changed',
]

export const EVENTS_URL = '/api/cluster/events'

// ---- Calls ----------------------------------------------------------------

const enc = encodeURIComponent

export const api = {
  stops: (signal?: AbortSignal) => request<Stop[]>('GET', '/api/stops', { signal }),
  board: (id: string, signal?: AbortSignal) => request<StopBoard>('GET', `/api/stops/${enc(id)}`, { signal }),

  alerts: (filter: { line?: string; stop?: string } = {}, signal?: AbortSignal) => {
    const query = new URLSearchParams()
    if (filter.line) query.set('line', filter.line)
    if (filter.stop) query.set('stop', filter.stop)
    const suffix = query.size > 0 ? `?${query}` : ''
    return request<Alert[]>('GET', `/api/alerts${suffix}`, { signal })
  },

  createPlayer: (nickname: string) => request<Player>('POST', '/api/game/players', { body: { nickname } }),
  player: (nickname: string) => request<Player>('GET', `/api/game/players/${enc(nickname)}`),
  watchStop: (nickname: string, stopId: string) =>
    request<void>('PUT', `/api/game/players/${enc(nickname)}/stop`, { body: { stopId } }),
  placeBet: (nickname: string, tripId: string, guessedDelayMinutes: number) =>
    request<Player>('POST', `/api/game/players/${enc(nickname)}/bet`, { body: { tripId, guessedDelayMinutes } }),
  leaderboard: () => request<LeaderboardRow[]>('GET', '/api/game/leaderboard'),

  silos: () => request<Silo[]>('GET', '/api/cluster/silos'),
  activations: () => request<Activation[]>('GET', '/api/cluster/activations'),
  stats: () => request<ClusterStats>('GET', '/api/cluster/stats'),
  killSilo: () => request<void>('POST', '/api/cluster/chaos/kill'),
}
