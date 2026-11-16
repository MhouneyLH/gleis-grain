// The grain view: which stop grain is active, on which silo, how many watchers.
// Plain TypeScript, no Vue and no Leaflet, so it can be tested on its own.
// The functions change the given object in place; wrap it in `reactive` to render it.
import type { Activation, LiveEventMap, LiveEventType } from '../api'

export type FlashKind = 'woke' | 'moved'

export interface StopDot {
  id: string
  active: boolean
  /** Name of the silo that hosts the grain; null while it is not active. */
  silo: string | null
  watchers: number
  /** Grows with every flash, so a renderer can tell "new flash" from "same state". */
  flashSeq: number
  flashKind: FlashKind | null
}

export interface GrainView {
  dots: Record<string, StopDot>
}

export function createGrainView(stopIds: readonly string[] = []): GrainView {
  const view: GrainView = { dots: {} }
  for (const id of stopIds) ensureDot(view, id)
  return view
}

export function ensureDot(view: GrainView, stopId: string): StopDot {
  let dot = view.dots[stopId]
  if (!dot) {
    dot = { id: stopId, active: false, silo: null, watchers: 0, flashSeq: 0, flashKind: null }
    view.dots[stopId] = dot
  }
  return dot
}

function flash(dot: StopDot, kind: FlashKind): void {
  dot.flashSeq += 1
  dot.flashKind = kind
}

/**
 * A grain woke up on a silo. Flashes when it was dormant ('woke') or when it
 * is now hosted by another silo than before ('moved', for example after a silo died).
 * An activation that repeats the known state changes nothing.
 */
export function activate(view: GrainView, stopId: string, silo: string, options: { flash?: boolean } = {}): FlashKind | null {
  const dot = ensureDot(view, stopId)
  const kind: FlashKind | null = !dot.active ? 'woke' : dot.silo !== silo ? 'moved' : null
  dot.active = true
  dot.silo = silo
  if (kind && options.flash !== false) flash(dot, kind)
  return kind
}

export function deactivate(view: GrainView, stopId: string): void {
  const dot = ensureDot(view, stopId)
  dot.active = false
  dot.silo = null
}

export function setWatchers(view: GrainView, stopId: string, watchers: number): void {
  ensureDot(view, stopId).watchers = Math.max(0, Math.trunc(watchers))
}

/**
 * Replaces the activation state with the list from GET /api/cluster/activations.
 * Stops that are missing in the list are dormant. Used for the first load
 * (without flashing) and after a reconnect (with flashing, so moved dots show).
 */
export function applyActivations(view: GrainView, list: readonly Activation[], options: { flash?: boolean } = {}): void {
  const listed = new Set<string>()
  for (const item of list) {
    listed.add(item.stopId)
    activate(view, item.stopId, item.silo, options)
    if (item.watchers !== undefined) setWatchers(view, item.stopId, item.watchers)
  }
  for (const dot of Object.values(view.dots)) {
    if (dot.active && !listed.has(dot.id)) deactivate(view, dot.id)
  }
}

/** Applies the grain related live events. Other event types are ignored. */
export function applyGrainEvent<T extends LiveEventType>(view: GrainView, type: T, payload: LiveEventMap[T]): void {
  switch (type) {
    case 'stop.activated': {
      const p = payload as LiveEventMap['stop.activated']
      activate(view, p.stopId, p.silo)
      break
    }
    case 'stop.deactivated':
      deactivate(view, (payload as LiveEventMap['stop.deactivated']).stopId)
      break
    case 'stop.watchers': {
      const p = payload as LiveEventMap['stop.watchers']
      setWatchers(view, p.stopId, p.watchers)
      break
    }
    default:
      break
  }
}

export interface WatcherBar {
  stopId: string
  name: string
  watchers: number
  silo: string | null
  /** 0..1 relative to the biggest bar. */
  share: number
}

/** The stops with watchers, busiest first, for the bar chart. */
export function watcherBars(
  view: GrainView,
  nameOf: (stopId: string) => string,
  limit = 8,
): WatcherBar[] {
  const rows = Object.values(view.dots)
    .filter((dot) => dot.watchers > 0)
    .sort((a, b) => b.watchers - a.watchers || nameOf(a.id).localeCompare(nameOf(b.id)))
    .slice(0, limit)
  const max = rows[0]?.watchers ?? 1
  return rows.map((dot) => ({
    stopId: dot.id,
    name: nameOf(dot.id),
    watchers: dot.watchers,
    silo: dot.silo,
    share: dot.watchers / max,
  }))
}

export function activeCount(view: GrainView): number {
  return Object.values(view.dots).filter((dot) => dot.active).length
}
