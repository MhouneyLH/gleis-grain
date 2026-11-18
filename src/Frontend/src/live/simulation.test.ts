import { describe, expect, it } from 'vitest'
import { ApiError } from '../api'
import { HOT_STOP, PRESETS, isSimulatorDown, pinnedCounts, presetLabel, scenarioView, toggleSelection } from './simulation'

const names: Record<string, string> = { a: 'Alpha', b: 'Beta', c: 'Gamma' }
const nameOf = (id: string) => names[id] ?? id

describe('simulation logic', () => {
  it('has the presets and the hot stop', () => {
    expect(PRESETS).toEqual([10, 25, 50, 100, 0])
    expect(presetLabel(0)).toBe('0 (release)')
    expect(presetLabel(25)).toBe('25')
    expect(HOT_STOP).toMatchObject({ id: '60501720', people: 100 })
  })

  it('builds the view: sorted, names resolved, zero groups dropped', () => {
    const view = scenarioView(
      {
        people: 80,
        joined: 78,
        hotStopShare: 0.4,
        pinned: [
          { stopId: 'b', people: 10 },
          { stopId: 'a', people: 10 },
          { stopId: 'c', people: 50 },
          { stopId: 'zz', people: 0 },
          { stopId: 'x', people: 3 },
        ],
      },
      nameOf,
    )
    expect(view.rows.map((r) => [r.name, r.people])).toEqual([
      ['Gamma', 50],
      ['Alpha', 10],
      ['Beta', 10],
      ['x', 3],
    ])
    expect(view.pinnedTotal).toBe(73)
    expect(view.people).toBe(80)
    expect(view.joined).toBe(78)
  })

  it('counts pinned people per stop', () => {
    expect(pinnedCounts(null)).toEqual({})
    expect(
      pinnedCounts({ people: 1, joined: 1, hotStopShare: 0, pinned: [{ stopId: 'a', people: 5 }, { stopId: 'b', people: 0 }] }),
    ).toEqual({ a: 5 })
  })

  it('toggles the selection', () => {
    expect(toggleSelection(null, 'a')).toBe('a')
    expect(toggleSelection('a', 'a')).toBeNull()
    expect(toggleSelection('a', 'b')).toBe('b')
  })

  it('recognises a simulator that is not running', () => {
    expect(isSimulatorDown(new ApiError(0, 'Network error'))).toBe(true)
    expect(isSimulatorDown(new ApiError(502, 'Bad Gateway'))).toBe(true)
    expect(isSimulatorDown(new ApiError(504, 'Gateway Timeout'))).toBe(true)
    expect(isSimulatorDown(new ApiError(400, 'Bad Request'))).toBe(false)
    expect(isSimulatorDown(new Error('x'))).toBe(false)
  })
})
