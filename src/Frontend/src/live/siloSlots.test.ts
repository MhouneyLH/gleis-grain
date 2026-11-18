import { describe, expect, it } from 'vitest'
import { parseVersion, shapeForSlot, SiloSlots, versionColor, type SiloLike } from './siloSlots'

const up = (name: string): SiloLike => ({ name, status: 'Active' })
const dead = (name: string): SiloLike => ({ name, status: 'Dead' })
const v1 = ['v1-3f9a1', 'v1-a0000', 'v1-c1111'].map(up)
const v2 = ['v2-b72c0', 'v2-d0000', 'v2-e1111'].map(up)

describe('parseVersion', () => {
  it('reads the number after the leading v', () => {
    expect(parseVersion('v1-3f9a1')).toBe(1)
    expect(parseVersion('v2-b72c0')).toBe(2)
    expect(parseVersion('v12-x')).toBe(12)
  })
  it('treats names that do not match as version 1', () => {
    expect(parseVersion('Silo_1')).toBe(1)
    expect(parseVersion('vx-1')).toBe(1)
    expect(parseVersion('')).toBe(1)
  })
  it('colours v1 and v2 differently', () => {
    expect(versionColor(1)).not.toBe(versionColor(2))
  })
})

describe('slot assignment', () => {
  it('ranks active silos of a version by name', () => {
    const slots = new SiloSlots()
    slots.update([...v1].reverse())
    expect(v1.map((s) => slots.slotOf(s.name))).toEqual([1, 2, 3])
    expect(v1.map((s) => slots.infoOf(s.name).shape)).toEqual(['square', 'circle', 'triangle'])
  })

  it('wraps around after three', () => {
    expect(shapeForSlot(4)).toBe('square')
    expect(shapeForSlot(5)).toBe('circle')
    expect(shapeForSlot(6)).toBe('triangle')
  })

  it('numbers every version on its own', () => {
    const slots = new SiloSlots()
    slots.update([...v1, ...v2])
    expect(slots.slotOf('v2-b72c0')).toBe(1)
    expect(slots.infoOf('v2-e1111').shape).toBe('triangle')
    expect(slots.infoOf('v2-e1111').version).toBe(2)
  })

  it('is stable when the silo list is unchanged or reordered', () => {
    const slots = new SiloSlots()
    slots.update(v1)
    expect(slots.update([...v1].reverse())).toBe(false)
    expect(v1.map((s) => slots.slotOf(s.name))).toEqual([1, 2, 3])
  })

  it('keeps the other slots when a silo dies and reuses the free slot for its replacement', () => {
    const slots = new SiloSlots()
    slots.update(v1)
    expect(slots.update([up('v1-3f9a1'), dead('v1-a0000'), up('v1-c1111')])).toBe(true)
    expect(slots.slotOf('v1-3f9a1')).toBe(1)
    expect(slots.slotOf('v1-c1111')).toBe(3)
    // The new name sorts first, but the existing slots stay and it takes the free slot 2.
    slots.update([up('v1-3f9a1'), dead('v1-a0000'), up('v1-c1111'), up('v1-0aaaa')])
    expect(slots.slotOf('v1-0aaaa')).toBe(2)
    expect(slots.slotOf('v1-3f9a1')).toBe(1)
    expect(slots.slotOf('v1-c1111')).toBe(3)
  })

  it('gives a fourth silo slot 4 (square again)', () => {
    const slots = new SiloSlots()
    slots.update([...v1, up('v1-zzzz')])
    expect(slots.slotOf('v1-zzzz')).toBe(4)
    expect(slots.infoOf('v1-zzzz').shape).toBe('square')
  })

  it('handles the rollout: three v1, then three v2, then v1 dies', () => {
    const slots = new SiloSlots()
    slots.update(v1)
    slots.update([...v1, ...v2])
    expect(v1.map((s) => slots.slotOf(s.name))).toEqual([1, 2, 3])
    expect(v2.map((s) => slots.slotOf(s.name))).toEqual([1, 2, 3])
    slots.update([dead('v1-3f9a1'), ...v1.slice(1), ...v2])
    expect(v2.map((s) => slots.slotOf(s.name))).toEqual([1, 2, 3])
    expect(slots.slotOf('v1-a0000')).toBe(2)
    slots.update([...v1.map((s) => dead(s.name)), ...v2])
    expect(v2.map((s) => slots.infoOf(s.name).color)).toEqual([versionColor(2), versionColor(2), versionColor(2)])
    expect(v2.map((s) => slots.slotOf(s.name))).toEqual([1, 2, 3])
  })

  it('assigns a silo that only shows up in an activation', () => {
    const slots = new SiloSlots()
    slots.update([up('v1-aaaa')])
    expect(slots.infoOf('v1-bbbb').slot).toBe(2)
    expect(slots.infoOf('Silo_9').version).toBe(1)
  })
})
