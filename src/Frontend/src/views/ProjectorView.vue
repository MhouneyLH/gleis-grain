<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { BETTING_ENABLED } from '../config'
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
import { shapeSvg } from '../live/siloShapes'
import { SHAPES, SiloSlots, versionColor } from '../live/siloSlots'
import { pushTicker, tickerEntry } from '../live/ticker'

const route = useRoute()
const { stops, error: stopsError, stopName } = useStops()

const slots = new SiloSlots()
// Slots are assigned lazily too (an activation can name a silo the list does not know yet), so ask the class.
const layoutKey = ref(0)
const infoOf = (silo: string) => slots.infoOf(silo)
const colorOf = (silo: string) => slots.infoOf(silo).color
const slotNumbers = [1, 2, 3]

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
    if (slots.update(list)) layoutKey.value += 1
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
  ...(BETTING_ENABLED
    ? {
        'bet.placed': (p) => apply('bet.placed', p),
        'bet.resolved': (p) => apply('bet.resolved', p),
        'leaderboard.changed': (p) => apply('leaderboard.changed', p),
      }
    : {}),
}

// After a reconnect (for example because the silo behind the stream died) load the state again.
const { connected } = useEventStream(handlers, () => {
  // Silos first, so the slots are ranked by name before activations ask for them.
  void loadSilos().then(() => loadActivations(false))
  if (BETTING_ENABLED) void loadLeaderboard()
})

onMounted(() => {
  void loadSilos().then(() => loadActivations(true))
  if (BETTING_ENABLED) void loadLeaderboard()
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
      <GrainMap :stops="stops" :view="live.grain" :info-of="infoOf" :layout-key="layoutKey" />

      <div class="join">
        <span>Join on your phone</span>
        <strong>{{ joinAddress }}</strong>
      </div>

      <div class="legend">
        <span class="conn" :class="{ off: !connected }">
          <i></i>{{ connected ? 'live' : 'reconnecting...' }}
        </span>
        <span class="chip lit num">{{ lit }} / {{ stops.length }} grains active</span>
      </div>

      <div class="key" aria-label="Legend">
        <div class="key-row">
          <span class="key-title">Silo</span>
          <span v-for="(shape, index) in SHAPES" :key="shape" class="key-item">
            <span v-html="shapeSvg(shape, '#9aa5b8', 34)"></span>{{ slotNumbers[index] }}
          </span>
        </div>
        <div class="key-row">
          <span class="key-title">Version</span>
          <span class="key-item"><i :style="{ background: versionColor(1) }"></i>v1</span>
          <span class="key-item"><i :style="{ background: versionColor(2) }"></i>v2</span>
        </div>
      </div>

      <div class="chaos-slot"><ChaosButton @message="onChaos" /></div>

      <p v-if="stopsError" class="map-error">{{ stopsError }}</p>
    </section>

    <aside class="panel">
      <LeaderboardCard v-if="BETTING_ENABLED" :rows="live.leaderboard" :limit="5" big />
      <SiloList :silos="silos" :info-of="infoOf" :hosted="hostedBySilo" />
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

.key {
  position: absolute;
  z-index: 900;
  right: 16px;
  bottom: 60px;
  display: grid;
  gap: 6px;
  padding: 10px 18px;
  border-radius: 16px;
  background: rgb(13 17 23 / 0.9);
  border: 2px solid var(--hairline);
}

.key-row {
  display: flex;
  align-items: center;
  gap: 18px;
}

.key-title {
  min-width: 4.5em;
  color: var(--muted);
  font-weight: 800;
  font-size: 0.9em;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.key-item {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 1.7em;
  font-weight: 900;
  line-height: 1;
}

.key-item i {
  width: 1em;
  height: 1em;
  border-radius: 4px;
  border: 2.5px solid #fff;
}

@media (max-width: 900px) {
  .projector {
    grid-template-columns: 1fr;
    grid-template-rows: 55dvh 1fr;
  }
}
</style>
