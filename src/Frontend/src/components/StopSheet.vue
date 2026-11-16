<script setup lang="ts">
import { computed, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { api, type Departure, type StopBoard } from '../api'
import { errorText, usePlayer } from '../composables/usePlayer'
import { usePolling } from '../composables/usePolling'
import { pushToast } from '../composables/useToast'
import { delayLabel, delayLevel, formatTime } from '../live/format'
import LineBadge from './LineBadge.vue'

const props = defineProps<{ stopId: string }>()
const emit = defineEmits<{ close: [] }>()

const { player, watchStop, placeBet } = usePlayer()

const board = ref<StopBoard | null>(null)
const loadError = ref<string | null>(null)
const watchBusy = ref(false)
const betTripId = ref<string | null>(null)
const guess = ref(1)
const betBusy = ref(false)
const betError = ref<string | null>(null)

const MAX_GUESS = 15

async function load(): Promise<void> {
  try {
    board.value = await api.board(props.stopId)
    loadError.value = null
  } catch (error) {
    loadError.value = errorText(error)
  }
}

// Refreshes every 15 seconds for as long as the sheet is open (the parent keys it by stop).
usePolling(load, 15_000)

const departures = computed(() => board.value?.departures.slice(0, 8) ?? [])
const watching = computed(() => player.value?.watchedStopId === props.stopId)
const openBetTripId = computed(() => player.value?.openBet?.tripId ?? null)

async function watch(): Promise<void> {
  watchBusy.value = true
  try {
    await watchStop(props.stopId)
    pushToast('info', `Watching ${board.value?.name ?? 'this stop'}`)
    void load()
  } catch (error) {
    pushToast('bad', 'Could not watch this stop', errorText(error))
  } finally {
    watchBusy.value = false
  }
}

function startBet(departure: Departure): void {
  betError.value = null
  guess.value = Math.min(Math.max(departure.delayMinutes, 0), MAX_GUESS)
  betTripId.value = betTripId.value === departure.tripId ? null : departure.tripId
}

const step = (by: number) => {
  guess.value = Math.min(MAX_GUESS, Math.max(0, guess.value + by))
}

async function submitBet(): Promise<void> {
  if (!betTripId.value) return
  betBusy.value = true
  betError.value = null
  try {
    await placeBet(betTripId.value, guess.value)
    pushToast('good', 'Bet placed', `You guessed ${guess.value} min.`)
    betTripId.value = null
  } catch (error) {
    betError.value = errorText(error)
  } finally {
    betBusy.value = false
  }
}
</script>

<template>
  <section class="sheet" aria-label="Stop board">
    <div class="grip" aria-hidden="true"></div>
    <header>
      <div>
        <h3>{{ board?.name ?? '...' }}</h3>
        <p v-if="board" class="muted">{{ board.watchers }} {{ board.watchers === 1 ? 'watcher' : 'watchers' }}</p>
      </div>
      <button class="close" aria-label="Close" @click="emit('close')">
        <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
          <path d="M5 5l14 14M19 5L5 19" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" />
        </svg>
      </button>
    </header>

    <div class="actions">
      <button v-if="!watching" class="btn" :disabled="watchBusy || !player" @click="watch">Watch this stop</button>
      <span v-else class="watching">You are watching this stop</span>
      <RouterLink class="btn btn-ghost" :to="`/stop/${stopId}`">Full board</RouterLink>
    </div>

    <p v-if="loadError" class="error-text">{{ loadError }}</p>
    <p v-else-if="!board" class="muted">Loading the board...</p>
    <p v-else-if="departures.length === 0" class="muted">No departures right now.</p>

    <ul v-if="departures.length > 0" class="deps">
      <li v-for="dep in departures" :key="dep.tripId">
        <div class="row">
          <LineBadge :line="dep.line" />
          <div class="where">
            <strong>{{ dep.direction }}</strong>
            <span class="muted num">{{ formatTime(dep.plannedAt) }}<template v-if="dep.platform"> · platform {{ dep.platform }}</template></span>
          </div>
          <span class="delay num" :class="`delay-${delayLevel(dep.delayMinutes)}`">{{ delayLabel(dep.delayMinutes) }}</span>
          <button
            v-if="openBetTripId !== dep.tripId"
            class="btn btn-small"
            :class="{ 'btn-ghost': betTripId === dep.tripId }"
            :disabled="!player"
            @click="startBet(dep)"
          >
            Bet
          </button>
          <span v-else class="mine">Your bet: {{ player?.openBet?.guessedDelayMinutes }} min</span>
        </div>

        <div v-if="betTripId === dep.tripId" class="bet">
          <span class="bet-label">How late will it be?</span>
          <div class="stepper">
            <button aria-label="One minute less" :disabled="guess <= 0" @click="step(-1)">-</button>
            <output class="num">{{ guess }} min</output>
            <button aria-label="One minute more" :disabled="guess >= MAX_GUESS" @click="step(1)">+</button>
          </div>
          <button class="btn" :disabled="betBusy" @click="submitBet">Place bet</button>
          <p v-if="betError" class="error-text bet-error">{{ betError }}</p>
        </div>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.sheet {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 1100;
  max-width: 640px;
  max-height: 78dvh;
  margin: 0 auto;
  overflow-y: auto;
  overscroll-behavior: contain;
  padding: 8px 16px calc(20px + env(safe-area-inset-bottom));
  background: var(--paper);
  border-radius: 24px 24px 0 0;
  box-shadow: 0 -10px 40px rgb(60 20 20 / 0.25);
  animation: rise 0.22s ease-out;
}

@keyframes rise {
  from {
    transform: translateY(40px);
    opacity: 0;
  }
}

.grip {
  width: 44px;
  height: 5px;
  margin: 0 auto 8px;
  border-radius: 3px;
  background: var(--hairline);
}

header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

h3 {
  font-size: 1.4rem;
  font-weight: 900;
  line-height: 1.2;
}

.close {
  flex: none;
  display: grid;
  place-items: center;
  width: 48px;
  height: 48px;
  border: 0;
  border-radius: 50%;
  background: var(--cream);
  cursor: pointer;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  margin: 8px 0 14px;
}

.actions .btn {
  text-decoration: none;
}

.watching {
  display: inline-flex;
  align-items: center;
  min-height: 48px;
  padding: 0 18px;
  border-radius: 999px;
  background: var(--green-soft);
  color: var(--green);
  font-weight: 800;
}

.deps {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 8px;
}

.deps > li {
  padding: 10px;
  border-radius: 14px;
  background: var(--cream);
}

.row {
  display: grid;
  grid-template-columns: auto 1fr auto auto;
  align-items: center;
  gap: 10px;
}

.where {
  display: grid;
  min-width: 0;
}

.where strong {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.where span {
  font-size: 0.85rem;
}

.mine {
  padding: 6px 10px;
  border-radius: 10px;
  background: var(--amber-soft);
  font-weight: 800;
  font-size: 0.85rem;
}

.bet {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px 14px;
  margin-top: 10px;
  padding-top: 10px;
  border-top: 2px dashed var(--hairline);
}

.bet-label {
  flex-basis: 100%;
  font-weight: 800;
}

.stepper {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px;
  border-radius: 999px;
  background: var(--paper);
  box-shadow: inset 0 0 0 2px var(--hairline);
}

.stepper button {
  width: 48px;
  height: 48px;
  border: 0;
  border-radius: 50%;
  background: var(--red-soft);
  color: var(--red-dark);
  font-size: 1.6rem;
  font-weight: 900;
  cursor: pointer;
}

.stepper button:disabled {
  opacity: 0.35;
}

.stepper output {
  min-width: 5.2ch;
  text-align: center;
  font-size: 1.3rem;
  font-weight: 900;
}

.bet-error {
  flex-basis: 100%;
}
</style>
