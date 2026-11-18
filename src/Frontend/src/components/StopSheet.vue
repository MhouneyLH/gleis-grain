<script setup lang="ts">
import { computed, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { api, type StopBoard } from '../api'
import { errorText, usePlayer } from '../composables/usePlayer'
import { usePolling } from '../composables/usePolling'
import { pushToast } from '../composables/useToast'
import { delayLabel, delayLevel, formatTime } from '../live/format'
import LineBadge from './LineBadge.vue'

const props = defineProps<{ stopId: string }>()
const emit = defineEmits<{ close: [] }>()

const { player, watchStop } = usePlayer()

const board = ref<StopBoard | null>(null)
const loadError = ref<string | null>(null)
const watchBusy = ref(false)

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
</script>

<template>
  <section class="sheet" aria-label="Stop board">
    <div class="grip" aria-hidden="true"></div>
    <header>
      <div>
        <h3>{{ board?.name ?? '...' }}</h3>
        <p v-if="board" class="muted">
          {{ board.watchers }} {{ board.watchers === 1 ? 'watcher' : 'watchers' }}
          <span v-if="board.grainVersion !== undefined && board.grainVersion !== null" class="chip-ver">grain v{{ String(board.grainVersion).replace(/^v/i, '') }}</span>
        </p>
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
        </div>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.chip-ver {
  margin-left: 8px;
  padding: 1px 9px;
  border-radius: 999px;
  background: var(--ink);
  color: var(--paper);
  font-size: 0.8rem;
  font-weight: 800;
}

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
  grid-template-columns: auto 1fr auto;
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
</style>
