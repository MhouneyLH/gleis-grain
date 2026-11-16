<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import LeaderboardCard from '../components/LeaderboardCard.vue'
import StopMap from '../components/StopMap.vue'
import StopSheet from '../components/StopSheet.vue'
import { useEventStream } from '../composables/useEventStream'
import { NICKNAME_RULES, usePlayer } from '../composables/usePlayer'
import { usePolling } from '../composables/usePolling'
import { useStops } from '../composables/useStops'
import { delayLabel, formatTime } from '../live/format'

const { stops, error: stopsError, stopName } = useStops()
const { nickname, player, leaderboard, register, refreshPlayer, refreshLeaderboard, forget, onLeaderboard, onBetResolved } =
  usePlayer()

const selectedId = ref<string | null>(null)
const wanted = ref('')
const joinError = ref<string | null>(null)
const joining = ref(false)

const watchedName = computed(() => (player.value?.watchedStopId ? stopName(player.value.watchedStopId) : null))
const bet = computed(() => player.value?.openBet ?? null)

async function join(): Promise<void> {
  joinError.value = null
  joining.value = true
  try {
    await register(wanted.value)
  } catch (error) {
    joinError.value = error instanceof Error ? error.message : NICKNAME_RULES
  } finally {
    joining.value = false
  }
}

onMounted(() => {
  void refreshPlayer()
  void refreshLeaderboard()
})

useEventStream(
  { 'leaderboard.changed': onLeaderboard, 'bet.resolved': onBetResolved },
  () => {
    void refreshPlayer()
    void refreshLeaderboard()
  },
)

// Bets are resolved by the server; keep the own state fresh in case an event was missed.
usePolling(refreshPlayer, 20_000, () => nickname.value !== null)
</script>

<template>
  <main class="page audience">
    <!-- Step 1: pick a nickname -->
    <section v-if="!nickname" class="card hello">
      <h1 class="page-title">Hop on!</h1>
      <p>
        Pick a nickname, choose a stop on the map and guess how late the next tram will be. No account, no personal data.
      </p>
      <form @submit.prevent="join">
        <label for="nick" class="muted">Your nickname</label>
        <input
          id="nick"
          v-model="wanted"
          class="field"
          maxlength="20"
          autocomplete="off"
          autocapitalize="off"
          spellcheck="false"
          placeholder="e.g. TramFan42"
        />
        <p v-if="joinError" class="error-text" role="alert">{{ joinError }}</p>
        <p v-else class="muted hint">{{ NICKNAME_RULES }}</p>
        <button class="btn" type="submit" :disabled="joining || wanted.trim().length === 0">Let's go</button>
      </form>
    </section>

    <!-- Step 2: play -->
    <template v-else>
      <section class="status card">
        <div class="who">
          <span class="muted">Playing as</span>
          <strong>{{ nickname }}</strong>
          <button class="link" @click="forget">not you?</button>
        </div>
        <div class="score">
          <span class="num">{{ player?.score ?? 0 }}</span>
          <span class="muted">points</span>
        </div>
        <p class="line-info">
          <span class="muted">Watching</span>
          <strong>{{ watchedName ?? 'no stop yet' }}</strong>
        </p>
        <p class="line-info">
          <span class="muted">Open bet</span>
          <strong v-if="bet">
            line {{ bet.line }} at {{ stopName(bet.stopId) }}, {{ formatTime(bet.plannedAt) }}: {{ delayLabel(bet.guessedDelayMinutes) }}
          </strong>
          <strong v-else>none, tap a stop and bet</strong>
        </p>
      </section>

      <section class="map-card">
        <p v-if="stopsError" class="error-text maperr">{{ stopsError }}</p>
        <StopMap :stops="stops" :selected-id="selectedId" :watched-id="player?.watchedStopId ?? null" @select="selectedId = $event" />
      </section>

      <LeaderboardCard :rows="leaderboard" :limit="5" :mine="nickname" />

      <StopSheet v-if="selectedId" :key="selectedId" :stop-id="selectedId" @close="selectedId = null" />
      <div v-if="selectedId" class="sheet-space" aria-hidden="true"></div>
    </template>
  </main>
</template>

<style scoped>
.audience {
  max-width: 640px;
}

.hello {
  display: grid;
  gap: 14px;
  margin-top: 8vh;
  padding: 22px;
  border-top: 8px solid var(--red);
}

.hello form {
  display: grid;
  gap: 10px;
}

.hint {
  font-size: 0.9rem;
}

.status {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 6px 12px;
  align-items: center;
  border-left: 8px solid var(--red);
}

.who {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 4px 8px;
}

.who strong {
  font-size: 1.35rem;
  font-weight: 900;
}

.link {
  border: 0;
  background: none;
  padding: 4px 0;
  color: var(--muted);
  text-decoration: underline;
  cursor: pointer;
  font-size: 0.85rem;
}

.score {
  display: grid;
  justify-items: center;
  line-height: 1;
}

.score .num {
  font-size: 2.6rem;
  font-weight: 900;
  color: var(--red);
}

.line-info {
  grid-column: 1 / -1;
  display: flex;
  flex-wrap: wrap;
  gap: 2px 8px;
}

.map-card {
  position: relative;
  height: min(58dvh, 520px);
  min-height: 300px;
  border-radius: var(--radius);
  overflow: hidden;
  box-shadow: var(--shadow);
  background: var(--paper);
}

.maperr {
  position: absolute;
  z-index: 500;
  top: 8px;
  left: 8px;
  right: 8px;
  padding: 8px 12px;
  border-radius: 10px;
  background: var(--paper);
}

.sheet-space {
  height: 40dvh;
}
</style>
