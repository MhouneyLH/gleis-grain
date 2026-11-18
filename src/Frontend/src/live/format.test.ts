import { describe, expect, it } from 'vitest'
import { cacheHitRatio, delayLabel, delayLevel, formatTime, shortTypeName } from './format'
import { SILO_PALETTE, SiloColors } from './siloColors'

describe('silo colours', () => {
  it('returns the same colour for the same silo', () => {
    const colors = new SiloColors()
    expect(colors.colorFor('Silo_1')).toBe(colors.colorFor('Silo_1'))
  })

  it('starts at the same slot in every instance for a name', () => {
    expect(new SiloColors().colorFor('Silo_1')).toBe(new SiloColors().colorFor('Silo_1'))
  })

  it('gives the first palette-size silos distinct colours, even on hash collisions', () => {
    const colors = new SiloColors()
    const used = new Set<string>()
    for (let i = 0; i < SILO_PALETTE.length; i++) used.add(colors.colorFor(`silo-${i}`))
    expect(used.size).toBe(SILO_PALETTE.length)
  })

  it('keeps assigned colours when more silos arrive', () => {
    const colors = new SiloColors()
    const first = colors.colorFor('a')
    for (let i = 0; i < 20; i++) colors.colorFor(`x${i}`)
    expect(colors.colorFor('a')).toBe(first)
  })
})

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
