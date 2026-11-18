import { describe, expect, it } from 'vitest'
import { generateGuestName, isValidNickname } from './guestName'

describe('guest names', () => {
  it('has the Guest- prefix and four characters', () => {
    expect(generateGuestName()).toMatch(/^Guest-[a-z0-9]{4}$/)
  })

  it('is always a valid nickname, even at the edges of the random range', () => {
    for (const value of [0, 0.5, 0.999999999, 1]) {
      expect(isValidNickname(generateGuestName(() => value))).toBe(true)
    }
    for (let i = 0; i < 200; i += 1) expect(isValidNickname(generateGuestName())).toBe(true)
  })

  it('differs between calls', () => {
    const names = new Set(Array.from({ length: 50 }, () => generateGuestName()))
    expect(names.size).toBeGreaterThan(40)
  })

  it('is deterministic for a fixed random source', () => {
    expect(generateGuestName(() => 0)).toBe('Guest-aaaa')
  })

  it('validates the 2-20 character rule', () => {
    expect(isValidNickname('a')).toBe(false)
    expect(isValidNickname('ab')).toBe(true)
    expect(isValidNickname('x'.repeat(20))).toBe(true)
    expect(isValidNickname('x'.repeat(21))).toBe(false)
  })
})
