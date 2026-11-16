import { describe, expect, it } from 'vitest'
import { normalizeLeaderboard } from './leaderboard'
import { applyLiveEvent, createLiveState } from './liveState'
import { describeEvent, pushTicker, tickerEntry } from './ticker'

const name = (id: string) => (id === 'a' ? 'Alpha' : id)

describe('leaderboard', () => {
  it('sorts by score, then nickname, and cuts the list', () => {
    const rows = [
      { nickname: 'zoe', score: 3 },
      { nickname: 'amy', score: 3 },
      { nickname: 'max', score: 9 },
    ]
    expect(normalizeLeaderboard(rows, 2).map((row) => row.nickname)).toEqual(['max', 'amy'])
  })

  it('does not change its input', () => {
    const rows = [{ nickname: 'b', score: 1 }, { nickname: 'a', score: 2 }]
    normalizeLeaderboard(rows)
    expect(rows[0]?.nickname).toBe('b')
  })
})

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
    expect(describeEvent('bet.placed', { nickname: 'Max', stopId: 'a', line: '1' }, name)).toBe(
      'Max bet on line 1 at Alpha',
    )
    expect(
      describeEvent('bet.resolved', { nickname: 'Max', tripId: 't', actualDelayMinutes: 2, points: 3 }, name),
    ).toBe('Max scored 3 points (delay was 2 min)')
    expect(describeEvent('leaderboard.changed', { top: [{ nickname: 'Max', score: 7 }] }, name)).toBe(
      'Leaderboard: Max leads with 7',
    )
    expect(describeEvent('leaderboard.changed', { top: [] }, name)).toBe('Leaderboard changed')
  })
})

describe('live state', () => {
  it('feeds grain view, leaderboard and ticker from one event', () => {
    const state = createLiveState(['a'])
    applyLiveEvent(state, 'stop.activated', { stopId: 'a', silo: 's1' }, name)
    applyLiveEvent(state, 'leaderboard.changed', { top: [{ nickname: 'Max', score: 2 }] }, name)
    expect(state.grain.dots['a']).toMatchObject({ active: true, silo: 's1' })
    expect(state.leaderboard).toEqual([{ nickname: 'Max', score: 2 }])
    expect(state.ticker.map((entry) => entry.kind)).toEqual(['leaderboard.changed', 'stop.activated'])
  })
})
