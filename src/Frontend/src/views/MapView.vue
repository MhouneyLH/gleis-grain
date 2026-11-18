<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { BETTING_ENABLED } from '../config'
import LeaderboardCard from '../components/LeaderboardCard.vue'
import StopMap from '../components/StopMap.vue'
import StopSheet from '../components/StopSheet.vue'
import { useEventStream } from '../composables/useEventStream'
import { usePlayer } from '../composables/usePlayer'
import { usePolling } from '../composables/usePolling'
import { useStops } from '../composables/useStops'
import { delayLabel, formatTime } from '../live/format'

const { stops, error: stopsError, stopName } = useStops()
const { nickname, player, leaderboard, ensureGuest, refreshPlayer, refreshLeaderboard, onLeaderboard, onBetResolved } =
  usePlayer()

const selectedId = ref<string | null>(null)

const watchedName = computed(() => (player.value?.watchedStopId ? stopName(player.value.watchedStopId) : null))
const bet = computed(() => player.value?.openBet ?? null)

onMounted(() => {
  // No prompt: a stored guest name is reused, otherwise one is claimed right away.
  void (nickname.value ? refreshPlayer() : ensureGuest())
  if (BETTING_ENABLED) void refreshLeaderboard()
})

if (BETTING_ENABLED) {
  useEventStream(
    { 'leaderboard.changed': onLeaderboard, 'bet.resolved': onBetResolved },
    () => {
      void refreshPlayer()
      void refreshLeaderboard()
    },
  )
}

// Keep the own state (watched stop) fresh; with betting on this also picks up resolved bets.
usePolling(refreshPlayer, 20_000, () => nickname.value !== null)
</script>

<template>
  <main class="audience">
    <StopMap
      class="full-map"
      :stops="stops"
      :selected-id="selectedId"
      :watched-id="player?.watchedStopId ?? null"
      @select="selectedId = $event"
    />

    <section class="status card" aria-label="You">
      <div class="who">
        <span class="muted">You are</span>
        <strong>{{ nickname ?? '...' }}</strong>
        <span v-if="BETTING_ENABLED" class="score num">{{ player?.score ?? 0 }} points</span>
      </div>
      <p class="line-info">
        <span class="muted">Watching</span>
        <strong>{{ watchedName ?? 'no stop yet, tap one' }}</strong>
      </p>
      <p v-if="BETTING_ENABLED" class="line-info">
        <span class="muted">Open bet</span>
        <strong v-if="bet">
          line {{ bet.line }} at {{ stopName(bet.stopId) }}, {{ formatTime(bet.plannedAt) }}: {{ delayLabel(bet.guessedDelayMinutes) }}
        </strong>
        <strong v-else>none, tap a stop and bet</strong>
      </p>
      <p v-if="stopsError" class="error-text" role="alert">{{ stopsError }}</p>
    </section>

    <LeaderboardCard v-if="BETTING_ENABLED" class="board" :rows="leaderboard" :limit="5" :mine="nickname ?? ''" />

    <StopSheet v-if="selectedId" :key="selectedId" :stop-id="selectedId" @close="selectedId = null" />
  </main>
</template>

<style scoped>
.audience {
  position: fixed;
  inset: 0;
  overflow: hidden;
}

.full-map {
  position: absolute;
  inset: 0;
}

.status {
  position: absolute;
  z-index: 1000;
  top: 52px;
  left: 10px;
  max-width: min(420px, calc(100vw - 20px));
  display: grid;
  gap: 2px;
  padding: 8px 14px;
  border-left: 6px solid var(--red);
  background: rgb(255 255 255 / 0.93);
}

.who {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 2px 8px;
}

.who strong {
  font-size: 1.15rem;
  font-weight: 900;
}

.score {
  font-weight: 900;
  color: var(--red);
}

.line-info {
  display: flex;
  flex-wrap: wrap;
  gap: 0 8px;
  font-size: 0.95rem;
}

.board {
  position: absolute;
  z-index: 1000;
  top: 52px;
  right: 10px;
  width: 260px;
  max-width: 40vw;
}
</style>
