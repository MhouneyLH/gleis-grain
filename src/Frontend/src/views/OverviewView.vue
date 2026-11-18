<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { BETTING_ENABLED } from '../config'
import { api, type ClusterStats, type Scenario, type LiveEventMap, type LiveEventType, type Silo } from '../api'
import ChaosButton from '../components/ChaosButton.vue'
import FloatingPanel from '../components/FloatingPanel.vue'
import SimulationPanel from '../components/SimulationPanel.vue'
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
import { pinnedCounts, toggleSelection } from '../live/simulation'
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

// Floating panels: each collapses on its own, H hides or shows all of them.
const panelsHidden = ref(false)
const collapsed = reactive({ legend: false, sim: false, silos: false, watchers: false, stats: true, ticker: false })
const selectedId = ref<string | null>(null)
const scenario = ref<Scenario | null>(null)
const pinned = computed(() => pinnedCounts(scenario.value))
const simOpen = computed(() => !panelsHidden.value && !collapsed.sim)

function onSelect(id: string): void {
  if (simOpen.value) selectedId.value = toggleSelection(selectedId.value, id)
}

function onKey(event: KeyboardEvent): void {
  if (event.key.toLowerCase() !== 'h' || event.ctrlKey || event.metaKey || event.altKey) return
  const target = event.target as HTMLElement | null
  if (target && /^(input|textarea|select)$/i.test(target.tagName)) return
  panelsHidden.value = !panelsHidden.value
}

onMounted(() => window.addEventListener('keydown', onKey))
onBeforeUnmount(() => window.removeEventListener('keydown', onKey))

function onChaos(text: string): void {
  live.ticker = pushTicker(live.ticker, tickerEntry('chaos', text))
  void loadSilos()
}
</script>

<template>
  <div class="projector theme-dark">
    <GrainMap
      class="full-map"
      :stops="stops"
      :view="live.grain"
      :info-of="infoOf"
      :layout-key="layoutKey"
      :selected-id="simOpen ? selectedId : null"
      :pinned="pinned"
      @select="onSelect"
    />

    <button class="toggle-all" type="button" :aria-pressed="panelsHidden" @click="panelsHidden = !panelsHidden">
      {{ panelsHidden ? 'Show panels' : 'Hide panels' }} <kbd>H</kbd>
    </button>

    <p v-if="stopsError" class="map-error">{{ stopsError }}</p>

    <div v-show="!panelsHidden" class="col left">
      <div class="join">
        <span>Join on your phone</span>
        <strong>{{ joinAddress }}</strong>
      </div>

      <FloatingPanel v-model:collapsed="collapsed.legend" title="Legend">
        <div class="key" aria-label="Legend">
          <div class="key-row">
            <span class="conn" :class="{ off: !connected }"><i></i>{{ connected ? 'live' : 'reconnecting...' }}</span>
            <span class="lit num">{{ lit }} / {{ stops.length }} grains active</span>
          </div>
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
      </FloatingPanel>

      <FloatingPanel v-model:collapsed="collapsed.sim" title="Simulation">
        <SimulationPanel :selected-id="selectedId" :stop-name="stopName" :open="simOpen" @scenario="scenario = $event" />
      </FloatingPanel>
    </div>

    <div v-show="!panelsHidden" class="col right">
      <ChaosButton @message="onChaos" />
      <LeaderboardCard v-if="BETTING_ENABLED" :rows="live.leaderboard" :limit="5" big />
      <FloatingPanel v-model:collapsed="collapsed.silos" title="Silos">
        <SiloList :silos="silos" :info-of="infoOf" :hosted="hostedBySilo" />
      </FloatingPanel>
      <FloatingPanel v-model:collapsed="collapsed.watchers" title="Watchers per stop">
        <WatcherBars :bars="bars" :color-of="colorOf" />
      </FloatingPanel>
      <FloatingPanel v-model:collapsed="collapsed.stats" title="Stats">
        <StatsPanel :stats="stats" />
      </FloatingPanel>
      <FloatingPanel v-model:collapsed="collapsed.ticker" title="Live ticker">
        <TickerList :entries="live.ticker" />
      </FloatingPanel>
    </div>
  </div>
</template>

<style scoped>
.projector {
  position: fixed;
  inset: 0;
  overflow: hidden;
  background: var(--cream);
  color: var(--ink);
  font-size: clamp(14px, 0.75vw + 4px, 22px);
}

.full-map {
  position: absolute;
  inset: 0;
}

/* Two columns of floating panels at the sides; the map centre stays free. */
.col {
  position: absolute;
  z-index: 900;
  top: 16px;
  bottom: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  overflow-y: auto;
  scrollbar-width: none;
  pointer-events: none;
}

.col::-webkit-scrollbar {
  display: none;
}

.col > * {
  flex: none;
}

.col.left {
  left: 16px;
  width: min(23em, 30vw);
}

.col.right {
  right: 16px;
  width: min(25em, 32vw);
}

.col.right :deep(.chaos) {
  width: 100%;
  pointer-events: auto;
}

.join {
  pointer-events: auto;
  display: grid;
  gap: 2px;
  padding: 10px 22px 12px;
  border-radius: 18px;
  background: rgb(13 17 23 / 0.8);
  border: 2px solid var(--red);
  box-shadow: var(--shadow);
  backdrop-filter: blur(6px);
}

.join span {
  color: var(--muted);
  font-weight: 800;
  font-size: 0.85em;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.join strong {
  font-size: 2.1em;
  font-weight: 900;
  line-height: 1.1;
  color: var(--amber);
  overflow-wrap: anywhere;
}

.toggle-all {
  position: absolute;
  z-index: 950;
  left: 50%;
  bottom: 12px;
  transform: translateX(-50%);
  padding: 4px 14px;
  border: 1px solid rgb(255 255 255 / 0.2);
  border-radius: 999px;
  background: rgb(13 17 23 / 0.6);
  color: var(--muted);
  font-size: 0.8em;
  font-weight: 800;
  cursor: pointer;
  opacity: 0.7;
}

.toggle-all:hover {
  opacity: 1;
}

kbd {
  padding: 0 6px;
  border-radius: 4px;
  border: 1px solid var(--muted);
  font: inherit;
}

.conn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-weight: 800;
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

.lit {
  font-weight: 800;
}

.map-error {
  position: absolute;
  z-index: 900;
  bottom: 50px;
  left: 50%;
  transform: translateX(-50%);
  padding: 8px 14px;
  border-radius: 12px;
  background: var(--red-soft);
  color: var(--red);
  font-weight: 800;
}

.key {
  display: grid;
  gap: 8px;
}

.key-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px 18px;
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
  .col.left,
  .col.right {
    width: calc(50vw - 24px);
    font-size: 12px;
  }
}
</style>
