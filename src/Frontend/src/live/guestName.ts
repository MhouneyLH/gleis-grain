/** Guest nicknames: `Guest-` plus four random digits or lowercase letters (10 characters, valid for the 2-20 rule). */
const ALPHABET = 'abcdefghjkmnpqrstuvwxyz23456789'

export const GUEST_PREFIX = 'Guest-'
export const NICKNAME_MIN = 2
export const NICKNAME_MAX = 20

export function isValidNickname(name: string): boolean {
  const trimmed = name.trim()
  return trimmed.length >= NICKNAME_MIN && trimmed.length <= NICKNAME_MAX
}

/** `random` returns a number in [0, 1), like Math.random; injectable for tests. */
export function generateGuestName(random: () => number = Math.random): string {
  let suffix = ''
  for (let i = 0; i < 4; i += 1) {
    const index = Math.min(ALPHABET.length - 1, Math.floor(random() * ALPHABET.length))
    suffix += ALPHABET[index]
  }
  return GUEST_PREFIX + suffix
}
