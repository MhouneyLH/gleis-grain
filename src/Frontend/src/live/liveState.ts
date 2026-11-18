// All state the overview derives from the event stream, as one plain object.
import type { LiveEventMap, LiveEventType } from '../api'
import { applyGrainEvent, createGrainView, type GrainView } from './grainView'
import { describeEvent, pushTicker, tickerEntry, type TickerEntry } from './ticker'

export interface LiveState {
  grain: GrainView
  ticker: TickerEntry[]
}

export function createLiveState(stopIds: readonly string[] = []): LiveState {
  return { grain: createGrainView(stopIds), ticker: [] }
}

/** Applies one live event: grain view and ticker. Changes `state` in place. */
export function applyLiveEvent<T extends LiveEventType>(
  state: LiveState,
  type: T,
  payload: LiveEventMap[T],
  stopName: (stopId: string) => string,
  at: number = Date.now(),
): void {
  applyGrainEvent(state.grain, type, payload)
  state.ticker = pushTicker(state.ticker, tickerEntry(type, describeEvent(type, payload, stopName), at))
}
