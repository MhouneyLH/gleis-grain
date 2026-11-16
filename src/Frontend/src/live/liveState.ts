// All state the projector derives from the event stream, as one plain object.
import type { LeaderboardRow, LiveEventMap, LiveEventType } from '../api'
import { applyGrainEvent, createGrainView, type GrainView } from './grainView'
import { normalizeLeaderboard } from './leaderboard'
import { describeEvent, pushTicker, tickerEntry, type TickerEntry } from './ticker'

export interface LiveState {
  grain: GrainView
  leaderboard: LeaderboardRow[]
  ticker: TickerEntry[]
}

export function createLiveState(stopIds: readonly string[] = []): LiveState {
  return { grain: createGrainView(stopIds), leaderboard: [], ticker: [] }
}

/** Applies one live event: grain view, leaderboard and ticker. Changes `state` in place. */
export function applyLiveEvent<T extends LiveEventType>(
  state: LiveState,
  type: T,
  payload: LiveEventMap[T],
  stopName: (stopId: string) => string,
  at: number = Date.now(),
): void {
  applyGrainEvent(state.grain, type, payload)
  if (type === 'leaderboard.changed') {
    state.leaderboard = normalizeLeaderboard((payload as LiveEventMap['leaderboard.changed']).top)
  }
  state.ticker = pushTicker(state.ticker, tickerEntry(type, describeEvent(type, payload, stopName), at))
}
