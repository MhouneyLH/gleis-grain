import { ref } from 'vue'
import { api, ApiError, type LeaderboardRow, type LiveEventMap, type Player } from '../api'
import { generateGuestName } from '../live/guestName'
import { normalizeLeaderboard } from '../live/leaderboard'
import { pushToast } from './useToast'

const STORAGE_KEY = 'gleis-grain.nickname'

function readStoredNickname(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY)
  } catch {
    return null
  }
}

function storeNickname(nickname: string | null): void {
  try {
    if (nickname) localStorage.setItem(STORAGE_KEY, nickname)
    else localStorage.removeItem(STORAGE_KEY)
  } catch {
    // Private mode: the player has to type the nickname again next time.
  }
}

// One player per browser, shared by all components of the audience page.
const nickname = ref<string | null>(readStoredNickname())
const player = ref<Player | null>(null)
const leaderboard = ref<LeaderboardRow[]>([])

const GUEST_ATTEMPTS = 5

/** The text for a person for any failed call. */
export function errorText(error: unknown): string {
  if (error instanceof ApiError) return error.status === 0 ? error.detail ?? error.title : error.friendly
  return 'Something went wrong.'
}

export function usePlayer() {
  /**
   * Makes sure there is a player: keeps the stored nickname or claims a fresh guest name
   * (a few retries on 409). No prompt; other failures stay silent, the next call tries again.
   */
  async function ensureGuest(): Promise<void> {
    if (nickname.value) return
    for (let attempt = 0; attempt < GUEST_ATTEMPTS; attempt += 1) {
      try {
        player.value = await api.createPlayer(generateGuestName())
        nickname.value = player.value.nickname
        storeNickname(nickname.value)
        return
      } catch (error) {
        if (!(error instanceof ApiError && (error.status === 409 || error.status === 400))) return
      }
    }
  }

  /** Reloads the player; forgets a nickname the server does not know (any more). */
  async function refreshPlayer(): Promise<void> {
    if (!nickname.value) return
    try {
      player.value = await api.player(nickname.value)
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        nickname.value = null
        player.value = null
        storeNickname(null)
        await ensureGuest()
      }
    }
  }

  async function refreshLeaderboard(): Promise<void> {
    try {
      leaderboard.value = normalizeLeaderboard(await api.leaderboard(), 5)
    } catch {
      // Keep the old list.
    }
  }

  async function watchStop(stopId: string): Promise<void> {
    if (!nickname.value) return
    await api.watchStop(nickname.value, stopId)
    await refreshPlayer()
  }

  async function placeBet(tripId: string, guessedDelayMinutes: number): Promise<void> {
    if (!nickname.value) return
    player.value = await api.placeBet(nickname.value, tripId, guessedDelayMinutes)
  }

  function onLeaderboard(payload: LiveEventMap['leaderboard.changed']): void {
    leaderboard.value = normalizeLeaderboard(payload.top, 5)
  }

  function onBetResolved(payload: LiveEventMap['bet.resolved']): void {
    if (!nickname.value || payload.nickname.toLowerCase() !== nickname.value.toLowerCase()) return
    const delay = `The tram was ${payload.actualDelayMinutes === 0 ? 'on time' : `${payload.actualDelayMinutes} min late`}.`
    if (payload.points > 0) {
      pushToast('good', `+${payload.points} ${payload.points === 1 ? 'point' : 'points'}!`, delay)
    } else {
      pushToast('info', 'No points this time', delay)
    }
    void refreshPlayer()
  }

  return {
    nickname,
    player,
    leaderboard,
    ensureGuest,
    refreshPlayer,
    refreshLeaderboard,
    watchStop,
    placeBet,
    onLeaderboard,
    onBetResolved,
  }
}
