// Pure logic of the simulation panel: presets, the scenario view model, error classification.
import type { Scenario } from '../api'

/** Preset buttons; 0 releases the people pinned to the selected stop. */
export const PRESETS: readonly number[] = [10, 25, 50, 100, 0]

/** Hauptbahnhof, the stop of the "hot station" button. */
export const HOT_STOP = { id: '60501720', name: 'Hauptbahnhof', people: 100 } as const

export const SIM_HINT = 'Simulator not running: click start on `simulator` in the Aspire dashboard'

export const presetLabel = (people: number): string => (people === 0 ? '0 (release)' : String(people))

export interface PinnedRow {
  stopId: string
  name: string
  people: number
}

export interface ScenarioView {
  people: number
  joined: number
  pinnedTotal: number
  rows: PinnedRow[]
}

/** Largest group first, then by name; groups with 0 people are dropped. */
export function scenarioView(scenario: Scenario, stopName: (id: string) => string): ScenarioView {
  const rows = scenario.pinned
    .filter((group) => group.people > 0)
    .map((group) => ({ stopId: group.stopId, name: stopName(group.stopId), people: group.people }))
    .sort((a, b) => b.people - a.people || a.name.localeCompare(b.name))
  return {
    people: scenario.people,
    joined: scenario.joined,
    pinnedTotal: rows.reduce((sum, row) => sum + row.people, 0),
    rows,
  }
}

/** Pinned people per stop id, for the badges on the map. */
export function pinnedCounts(scenario: Scenario | null): Record<string, number> {
  const counts: Record<string, number> = {}
  for (const group of scenario?.pinned ?? []) {
    if (group.people > 0) counts[group.stopId] = group.people
  }
  return counts
}

/** Clicking the selected stop again clears the selection. */
export const toggleSelection = (current: string | null, clicked: string): string | null =>
  current === clicked ? null : clicked

/** A network error (status 0) or a gateway answer of the proxy means: nothing listens. */
export function isSimulatorDown(error: unknown): boolean {
  const status = typeof error === 'object' && error !== null ? (error as { status?: unknown }).status : undefined
  return status === 0 || status === 502 || status === 503 || status === 504
}
