<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { api, type ClusterStats, type LiveEventMap, type LiveEventType, type Silo } from '../api'
import ChaosButton from '../components/ChaosButton.vue'
import GrainMap from '../components/GrainMap.vue'
import LeaderboardCard from '../components/LeaderboardCard.vue'
import SiloList from '../components/SiloList.vue'
import StatsPanel from '../components/StatsPanel.vue'
import TickerList from '../components/TickerList.vue'
import WatcherBars from '../components/WatcherBars.vue'
import { useEventStream, type LiveHandlers } from '../composables/useEventStream'
import { usePolling } from '../composables/usePolling'
import { useStops } from '../composables/useStops'
import { activeCount, applyActivations, watcherBars } from '../live/grainView'
import { normalizeLeaderboard } from '../live/leaderboard'
import { applyLiveEvent, createLiveState } from '../live/liveState'
import { SiloColors } from '../live/siloColors'
import { pushTicker, tickerEntry } from '../live/ticker'

const route = useRoute()
const { stops, error: stopsError, stopName } = useStops()

const colors = new SiloColors()
const colorOf = (silo: string) => colors.colorFor(silo)

const live = reactive(createLiveState())
const silos = ref<Silo[]>([])
const stats = ref<ClusterStats | null>(null)

// The address the audience types in. Behind a laptop "localhost" is useless, so ?join=... overrides it.
const joinAddress = computed(() => {
  const given = route.query['join']
  const url = typeof given === 'string' && given.length > 0 ? given : window.location.origin
  return url.replace(/^https?:\/\//, '').replace(/\/$/, '')
})

const bars = computed(() => watcherBars(live.grain, stopName, 6))
const lit = computed(() => activeCount(live.grain))
const hostedBySilo = computed(() => {
  const counts: Record<string, number> = {}
  for (const dot of Object.values(live.grain.dots)) {
    if (dot.active && dot.silo) counts[dot.silo] = (counts[dot.silo] ?? 0) + 1
  }
  return counts
})
const liveSilos = computed(() => silos.value.filter((silo) => silo.status.toLowerCase() !== 'dead'))

async function loadActivations(first: boolean): Promise<void> {
  try {
    applyActivations(live.grain, await api.activations(), { flash: !first })
  } catch {
    // The next re-sync or event repairs it.
  }
}

async function loadSilos(): Promise<void> {
  try {
    const list = await api.silos()
    // Assign colours in name order so a fresh projector page picks the same colours most of the time.
    for (const silo of [...list].sort((a, b) => a.name.localeCompare(b.name))) colors.colorFor(silo.name)
    silos.value = list
  } catch {
    // Keep the last known list.
  }
}

async function loadLeaderboard(): Promise<void> {
  try {
    live.leaderboard = normalizeLeaderboard(await api.leaderboard())
  } catch {
    // Events will fill it.
  }
}

async function loadStats(): Promise<void> {
  try {
    stats.value = await api.stats()
  } catch {
    // Keep the last numbers.
  }
}

function apply<T extends LiveEventType>(type: T, payload: LiveEventMap[T]): void {
  applyLiveEvent(live, type, payload, stopName)
}

const handlers: LiveHandlers = {
  'stop.activated': (p) => apply('stop.activated', p),
  'stop.deactivated': (p) => apply('stop.deactivated', p),
  'stop.watchers': (p) => apply('stop.watchers', p),
  'bet.placed': (p) => apply('bet.placed', p),
  'bet.resolved': (p) => apply('bet.resolved', p),
  'leaderboard.changed': (p) => apply('leaderboard.changed', p),
}

// After a reconnect (for example because the silo behind the stream died) load the state again.
const { connected } = useEventStream(handlers, () => {
  void loadActivations(false)
  void loadSilos()
  void loadLeaderboard()
})

onMounted(() => {
  void loadActivations(true)
  void loadLeaderboard()
})
usePolling(loadSilos, 3000)
usePolling(loadStats, 5000)

function onChaos(text: string): void {
  live.ticker = pushTicker(live.ticker, tickerEntry('chaos', text))
  void loadSilos()
}
</script>

<template>
  <div class="projector theme-dark">
    <section class="stage">
      <GrainMap :stops="stops" :view="live.grain" :color-of="colorOf" />

      <div class="join">
        <span>Join on your phone</span>
        <strong>{{ joinAddress }}</strong>
      </div>

      <div class="legend">
        <span class="conn" :class="{ off: !connected }">
          <i></i>{{ connected ? 'live' : 'reconnecting...' }}
        </span>
        <span class="chip lit num">{{ lit }} / {{ stops.length }} grains active</span>
        <span v-for="silo in liveSilos" :key="silo.name" class="chip">
          <i :style="{ background: colorOf(silo.name) }"></i>{{ silo.name }}
        </span>
      </div>

      <div class="chaos-slot"><ChaosButton @message="onChaos" /></div>

      <p v-if="stopsError" class="map-error">{{ stopsError }}</p>
    </section>

    <aside class="panel">
      <LeaderboardCard :rows="live.leaderboard" :limit="5" big />
      <SiloList :silos="silos" :color-of="colorOf" :hosted="hostedBySilo" />
      <WatcherBars :bars="bars" :color-of="colorOf" />
      <StatsPanel :stats="stats" />
      <TickerList :entries="live.ticker" />
    </aside>
  </div>
</template>

<style scoped>
.projector {
  display: grid;
  grid-template-columns: minmax(0, 1fr) clamp(380px, 32vw, 640px);
  height: 100dvh;
  overflow: hidden;
  background: var(--cream);
  color: var(--ink);
  font-size: clamp(14px, 0.75vw + 4px, 22px);
}

.stage {
  position: relative;
  min-width: 0;
}

.join {
  position: absolute;
  z-index: 900;
  top: 16px;
  left: 16px;
  display: grid;
  gap: 2px;
  padding: 10px 22px 12px;
  border-radius: 18px;
  background: rgb(13 17 23 / 0.86);
  border: 2px solid var(--red);
  box-shadow: var(--shadow);
}

.join span {
  color: var(--muted);
  font-weight: 800;
  font-size: 0.85em;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.join strong {
  font-size: 2.4em;
  font-weight: 900;
  line-height: 1.1;
  color: var(--amber);
}

.chaos-slot {
  position: absolute;
  z-index: 900;
  top: 16px;
  right: 16px;
}

.legend {
  position: absolute;
  z-index: 900;
  left: 16px;
  bottom: 16px;
  right: 16px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  pointer-events: none;
}

.chip,
.conn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 6px 14px;
  border-radius: 999px;
  background: rgb(13 17 23 / 0.86);
  font-weight: 800;
}

.chip i {
  width: 0.9em;
  height: 0.9em;
  border-radius: 50%;
}

.conn i {
  width: 0.7em;
  height: 0.7em;
  border-radius: 50%;
  background: var(--green);
  box-shadow: 0 0 10px var(--green);
}

.conn.off {
  color: var(--amber);
}

.conn.off i {
  background: var(--amber);
  box-shadow: 0 0 10px var(--amber);
}

.map-error {
  position: absolute;
  z-index: 900;
  bottom: 70px;
  left: 16px;
  padding: 8px 14px;
  border-radius: 12px;
  background: var(--red-soft);
  color: var(--red);
  font-weight: 800;
}

.panel {
  display: grid;
  align-content: start;
  gap: 12px;
  padding: 14px;
  overflow-y: auto;
  background: #090c12;
  border-left: 2px solid var(--hairline);
}

.panel :deep(.card) {
  padding: 0.85em 1em;
}

.panel :deep(h2) {
  font-size: 0.8em;
}

@media (max-width: 900px) {
  .projector {
    grid-template-columns: 1fr;
    grid-template-rows: 55dvh 1fr;
  }
}
</style>
