// Silo encoding for the grain view: version -> colour, slot among the silos of that version -> shape.
// Plain TypeScript, no Vue and no Leaflet, so it can be tested on its own.

export type SiloShape = 'square' | 'circle' | 'triangle'

export const SHAPES: readonly SiloShape[] = ['square', 'circle', 'triangle']

/** v1 red, v2 blue; further versions get distinct fallbacks. */
export const VERSION_COLORS: readonly string[] = [
  '#ff4d4f', // v1 red
  '#3fa0ff', // v2 blue
  '#ffc83d', // v3 amber
  '#34d399', // v4 mint
]

export const NO_SILO_COLOR = '#4b5563'

export interface SiloLike {
  name: string
  status: string
}

export interface SiloInfo {
  version: number
  /** 1-based. Can be above 3; the shape wraps around. */
  slot: number
  shape: SiloShape
  color: string
}

/** `v2-b72c0` gives 2. A name that does not match gives 1. */
export function parseVersion(name: string): number {
  const match = /^v(\d+)(?:-|$)/i.exec(name)
  const version = match?.[1] ? Number.parseInt(match[1], 10) : 1
  return Number.isSafeInteger(version) && version > 0 ? version : 1
}

export function versionColor(version: number): string {
  return VERSION_COLORS[(Math.max(version, 1) - 1) % VERSION_COLORS.length] ?? NO_SILO_COLOR
}

export function shapeForSlot(slot: number): SiloShape {
  return SHAPES[(Math.max(slot, 1) - 1) % SHAPES.length] ?? 'square'
}

export const isDeadStatus = (status: string): boolean => status.toLowerCase() === 'dead'

/**
 * Keeps the slot of every silo for the session. A slot is the rank among the active silos of
 * the same version; it stays while the set of active silos does not change, a dead silo frees
 * its slot, and a new silo takes the lowest free one (so a replacement reuses the shape).
 */
export class SiloSlots {
  private readonly slots = new Map<string, number>()
  private readonly active = new Set<string>()

  /** Call with every silo list. Dead silos free their slot. Returns true when something changed. */
  update(silos: readonly SiloLike[]): boolean {
    const before = this.signature()
    const alive = silos.filter((silo) => !isDeadStatus(silo.status)).map((silo) => silo.name)
    for (const name of [...this.active]) if (!alive.includes(name)) this.active.delete(name)
    for (const name of [...alive].sort(compareNames)) this.ensure(name)
    return before !== this.signature()
  }

  slotOf(name: string): number {
    return this.ensure(name)
  }

  infoOf(name: string): SiloInfo {
    const version = parseVersion(name)
    const slot = this.slotOf(name)
    return { version, slot, shape: shapeForSlot(slot), color: versionColor(version) }
  }

  /** Makes the silo active and returns its slot. A known silo keeps its slot while that is free. */
  private ensure(name: string): number {
    if (this.active.has(name)) return this.slots.get(name) as number
    const version = parseVersion(name)
    const taken = new Set<number>()
    for (const other of this.active) if (parseVersion(other) === version) taken.add(this.slots.get(other) as number)
    let slot = this.slots.get(name)
    if (slot === undefined || taken.has(slot)) {
      slot = 1
      while (taken.has(slot)) slot += 1
    }
    this.slots.set(name, slot)
    this.active.add(name)
    return slot
  }

  private signature(): string {
    return [...this.active]
      .sort()
      .map((name) => `${name}=${this.slots.get(name)}`)
      .join(',')
  }
}

function compareNames(a: string, b: string): number {
  return a < b ? -1 : a > b ? 1 : 0
}
