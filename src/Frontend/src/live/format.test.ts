import { describe, expect, it } from 'vitest'
import { cacheHitRatio, delayLabel, delayLevel, formatTime, shortTypeName } from './format'

describe('format helpers', () => {
  it('classifies and labels delays', () => {
    expect(delayLevel(0)).toBe('ontime')
    expect(delayLevel(2)).toBe('minor')
    expect(delayLevel(3)).toBe('major')
    expect(delayLabel(0)).toBe('on time')
    expect(delayLabel(4)).toBe('+4 min')
  })

  it('shows times in Linz local time', () => {
    expect(formatTime('2026-11-17T10:05:00+00:00')).toBe('11:05')
    expect(formatTime('nonsense')).toBe('--:--')
  })

  it('shortens grain type names', () => {
    expect(shortTypeName('GleisGrain.Stops.StopGrain')).toBe('StopGrain')
    expect(shortTypeName('stop')).toBe('stop')
  })

  it('computes the cache hit ratio', () => {
    expect(cacheHitRatio(0, 0)).toBe(0)
    expect(cacheHitRatio(1, 3)).toBe(0.75)
  })
})
