/** Fixed palette for the silos. None of them is red: red means "dead". */
export const SILO_PALETTE: readonly string[] = [
  '#ffc83d', // amber
  '#34d399', // mint
  '#4da3ff', // sky
  '#a78bfa', // violet
  '#f472b6', // pink
  '#a3e635', // lime
  '#fb923c', // orange
  '#22d3ee', // cyan
]

export const NO_SILO_COLOR = '#4b5563'

/** FNV-1a, so the same name starts at the same palette slot in every browser. */
export function hashString(value: string): number {
  let hash = 0x811c9dc5
  for (let i = 0; i < value.length; i++) {
    hash ^= value.charCodeAt(i)
    hash = Math.imul(hash, 0x01000193) >>> 0
  }
  return hash
}

/**
 * Gives every silo name its own palette colour and keeps it. The slot comes from
 * a hash of the name; when two silos meet on the same slot the later one takes the next free slot.
 */
export class SiloColors {
  private readonly slots = new Map<string, number>()

  colorFor(silo: string): string {
    let slot = this.slots.get(silo)
    if (slot === undefined) {
      const taken = new Set(this.slots.values())
      slot = hashString(silo) % SILO_PALETTE.length
      for (let i = 0; i < SILO_PALETTE.length && taken.has(slot); i++) {
        slot = (slot + 1) % SILO_PALETTE.length
      }
      this.slots.set(silo, slot)
    }
    return SILO_PALETTE[slot] ?? NO_SILO_COLOR
  }
}
