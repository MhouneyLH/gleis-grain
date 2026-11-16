import { describe, expect, it } from 'vitest'
import {
  activate,
  activeCount,
  applyActivations,
  applyGrainEvent,
  createGrainView,
  deactivate,
  setWatchers,
  watcherBars,
} from './grainView'

const names: Record<string, string> = { a: 'Alpha', b: 'Beta', c: 'Gamma' }
const nameOf = (id: string) => names[id] ?? id

describe('grain view', () => {
  it('starts dormant', () => {
    const view = createGrainView(['a', 'b'])
    expect(view.dots['a']).toMatchObject({ active: false, silo: null, watchers: 0, flashSeq: 0 })
    expect(activeCount(view)).toBe(0)
  })

  it('flashes "woke" when a dormant grain activates', () => {
    const view = createGrainView(['a'])
    expect(activate(view, 'a', 's1')).toBe('woke')
    expect(view.dots['a']).toMatchObject({ active: true, silo: 's1', flashSeq: 1, flashKind: 'woke' })
  })

  it('does not flash when the same silo reports the same activation again', () => {
    const view = createGrainView(['a'])
    activate(view, 'a', 's1')
    expect(activate(view, 'a', 's1')).toBeNull()
    expect(view.dots['a']?.flashSeq).toBe(1)
  })

  it('flashes "moved" when the hosting silo changes', () => {
    const view = createGrainView(['a'])
    activate(view, 'a', 's1')
    expect(activate(view, 'a', 's2')).toBe('moved')
    expect(view.dots['a']).toMatchObject({ silo: 's2', flashSeq: 2, flashKind: 'moved' })
  })

  it('flashes "woke" again after a deactivation, even on another silo', () => {
    const view = createGrainView(['a'])
    activate(view, 'a', 's1')
    deactivate(view, 'a')
    expect(view.dots['a']).toMatchObject({ active: false, silo: null })
    expect(activate(view, 'a', 's2')).toBe('woke')
  })

  it('can activate without flashing for the first load', () => {
    const view = createGrainView(['a'])
    activate(view, 'a', 's1', { flash: false })
    expect(view.dots['a']).toMatchObject({ active: true, flashSeq: 0, flashKind: null })
  })

  it('creates dots for stops it has not seen', () => {
    const view = createGrainView()
    applyGrainEvent(view, 'stop.activated', { stopId: 'x', silo: 's1' })
    expect(view.dots['x']?.active).toBe(true)
  })

  it('applies the grain events', () => {
    const view = createGrainView(['a'])
    applyGrainEvent(view, 'stop.activated', { stopId: 'a', silo: 's1' })
    applyGrainEvent(view, 'stop.watchers', { stopId: 'a', watchers: 4 })
    expect(view.dots['a']).toMatchObject({ active: true, silo: 's1', watchers: 4 })
    applyGrainEvent(view, 'stop.deactivated', { stopId: 'a' })
    expect(view.dots['a']?.active).toBe(false)
    applyGrainEvent(view, 'bet.placed', { nickname: 'n', stopId: 'a', line: '1' })
    expect(view.dots['a']?.watchers).toBe(4)
  })

  it('never lets the watcher count go below zero', () => {
    const view = createGrainView(['a'])
    setWatchers(view, 'a', -3)
    expect(view.dots['a']?.watchers).toBe(0)
  })

  describe('applyActivations (first load and re-sync)', () => {
    it('lights the listed stops and darkens the others', () => {
      const view = createGrainView(['a', 'b', 'c'])
      activate(view, 'c', 's1', { flash: false })
      applyActivations(view, [{ stopId: 'a', silo: 's1' }, { stopId: 'b', silo: 's2' }], { flash: false })
      expect(view.dots['a']).toMatchObject({ active: true, silo: 's1' })
      expect(view.dots['b']).toMatchObject({ active: true, silo: 's2' })
      expect(view.dots['c']).toMatchObject({ active: false, silo: null })
      expect(view.dots['a']?.flashSeq).toBe(0)
    })

    it('flashes the stops that moved to another silo after a reconnect', () => {
      const view = createGrainView(['a', 'b'])
      applyActivations(view, [{ stopId: 'a', silo: 's1' }, { stopId: 'b', silo: 's1' }], { flash: false })
      applyActivations(view, [{ stopId: 'a', silo: 's2' }, { stopId: 'b', silo: 's1' }])
      expect(view.dots['a']).toMatchObject({ silo: 's2', flashKind: 'moved', flashSeq: 1 })
      expect(view.dots['b']?.flashSeq).toBe(0)
    })

    it('keeps the watchers unless the host sends them', () => {
      const view = createGrainView(['a', 'b'])
      setWatchers(view, 'a', 5)
      applyActivations(view, [{ stopId: 'a', silo: 's1' }, { stopId: 'b', silo: 's1', watchers: 2 }], { flash: false })
      expect(view.dots['a']?.watchers).toBe(5)
      expect(view.dots['b']?.watchers).toBe(2)
    })
  })

  describe('watcherBars', () => {
    it('sorts busiest first, relative to the biggest, and skips empty stops', () => {
      const view = createGrainView(['a', 'b', 'c'])
      activate(view, 'b', 's1')
      setWatchers(view, 'a', 2)
      setWatchers(view, 'b', 8)
      const bars = watcherBars(view, nameOf)
      expect(bars.map((bar) => bar.name)).toEqual(['Beta', 'Alpha'])
      expect(bars.map((bar) => bar.share)).toEqual([1, 0.25])
      expect(bars[0]?.silo).toBe('s1')
    })

    it('respects the limit and handles no watchers', () => {
      const view = createGrainView(['a', 'b', 'c'])
      expect(watcherBars(view, nameOf)).toEqual([])
      setWatchers(view, 'a', 1)
      setWatchers(view, 'b', 1)
      setWatchers(view, 'c', 1)
      expect(watcherBars(view, nameOf, 2).map((bar) => bar.name)).toEqual(['Alpha', 'Beta'])
    })
  })
})
