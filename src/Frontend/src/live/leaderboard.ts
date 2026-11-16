import type { LeaderboardRow } from '../api'

/** Best score first, ties by nickname so the order does not jump around. */
export function normalizeLeaderboard(rows: readonly LeaderboardRow[], limit = 10): LeaderboardRow[] {
  return [...rows]
    .sort((a, b) => b.score - a.score || a.nickname.localeCompare(b.nickname))
    .slice(0, limit)
}
