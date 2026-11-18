import { describe, expect, it } from 'vitest'
import { applyLiveEvent, createLiveState } from './liveState'
import { describeEvent, pushTicker, tickerEntry } from './ticker'

const name = (id: string) => (id === 'a' ? 'Alpha' : id)

describe('ticker', () => {
  it('keeps only the newest 8 entries, newest first', () => {
    let entries = pushTicker([], tickerEntry('stop.activated', 'first'))
    for (let i = 1; i <= 9; i++) entries = pushTicker(entries, tickerEntry('stop.activated', `e${i}`))
    expect(entries).toHaveLength(8)
    expect(entries[0]?.text).toBe('e9')
    expect(entries.at(-1)?.text).toBe('e2')
  })

  it('describes every event type', () => {
    expect(describeEvent('stop.activated', { stopId: 'a', silo: 's1' }, name)).toBe('Alpha woke up on s1')
    expect(describeEvent('stop.deactivated', { stopId: 'a' }, name)).toBe('Alpha went to sleep')
    expect(describeEvent('stop.watchers', { stopId: 'a', watchers: 1 }, name)).toBe('Alpha: 1 watcher')
    expect(describeEvent('stop.watchers', { stopId: 'a', watchers: 3 }, name)).toBe('Alpha: 3 watchers')
  })
})

describe('live state', () => {
  it('feeds grain view and ticker from one event', () => {
    const state = createLiveState(['a'])
    applyLiveEvent(state, 'stop.activated', { stopId: 'a', silo: 's1' }, name)
    applyLiveEvent(state, 'stop.watchers', { stopId: 'a', watchers: 2 }, name)
    expect(state.grain.dots['a']).toMatchObject({ active: true, silo: 's1' })
    expect(state.grain.dots['a']?.watchers).toBe(2)
    expect(state.ticker.map((entry) => entry.kind)).toEqual(['stop.watchers', 'stop.activated'])
  })
})
