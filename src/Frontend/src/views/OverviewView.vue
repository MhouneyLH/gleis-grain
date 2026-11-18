<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { api, type ClusterStats, type Scenario, type LiveEventMap, type LiveEventType, type Silo } from '../api'
import ChaosButton from '../components/ChaosButton.vue'
import FloatingPanel from '../components/FloatingPanel.vue'
import SimulationPanel from '../components/SimulationPanel.vue'
import GrainMap from '../components/GrainMap.vue'
import SiloList from '../components/SiloList.vue'
import StatsPanel from '../components/StatsPanel.vue'
import TickerList from '../components/TickerList.vue'
import WatcherBars from '../components/WatcherBars.vue'
import { useEventStream, type LiveHandlers } from '../composables/useEventStream'
import { usePolling } from '../composables/usePolling'
import { retryWithBackoff } from '../composables/retry'
import { useStops } from '../composables/useStops'
import { applyActivations, watcherBars } from '../live/grainView'
import { applyLiveEvent, createLiveState } from '../live/liveState'
import { shapeSvg } from '../live/siloShapes'
import { SHAPES, SiloSlots, versionColor } from '../live/siloSlots'
import { pinnedCounts, toggleSelection } from '../live/simulation'
import { pushTicker, tickerEntry } from '../live/ticker'

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

const bars = computed(() => watcherBars(live.grain, stopName, 6))
// Counted over the loaded stops, so an event for an unknown stop cannot make it "26 of 25".
const lit = computed(() => stops.value.filter((stop) => live.grain.dots[stop.id]?.active).length)
const hostedBySilo = computed(() => {
  const counts: Record<string, number> = {}
  for (const dot of Object.values(live.grain.dots)) {
    if (dot.active && dot.silo) counts[dot.silo] = (counts[dot.silo] ?? 0) + 1
  }
  return counts
})

async function loadSilos(): Promise<void> {
  try {
    const list = await api.silos()
    if (slots.update(list)) layoutKey.value += 1
    silos.value = list
  } catch {
    // Keep the last known list.
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
}

/**
 * Loads silos, then the activations; retries until the host answers (it may still be starting).
 * Silos first, so the slots are ranked by name before activations ask for them.
 */
function syncState(first: boolean): void {
  void retryWithBackoff(async () => {
    await loadSilos()
    applyActivations(live.grain, await api.activations(), { flash: !first })
  })
}

// After a reconnect (for example because the silo behind the stream died) load the state again.
const { connected } = useEventStream(handlers, () => syncState(false))

onMounted(() => syncState(true))
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
  <div class="overview theme-dark">
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
      <FloatingPanel v-model:collapsed="collapsed.legend" title="Legend">
        <div class="key panel-stack" aria-label="Legend">
          <div class="key-row">
            <span class="conn" :class="{ off: !connected }"><i></i>{{ connected ? 'live' : 'reconnecting...' }}</span>
            <span class="lit num">{{ stops.length > 0 ? `Active grains: ${lit} of ${stops.length} stops` : 'loading stops ...' }}</span>
          </div>
          <div class="key-row">
            <span class="key-title label-caps">Silo</span>
            <span v-for="(shape, index) in SHAPES" :key="shape" class="key-item">
              <span v-html="shapeSvg(shape, '#9aa5b8', 24)"></span>{{ slotNumbers[index] }}
            </span>
          </div>
          <div class="key-row">
            <span class="key-title label-caps">Version</span>
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
.overview {
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

.key-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px 18px;
}

.key-title {
  min-width: 4.5em;
}

.key-item {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-size: 1.15em;
  font-weight: 800;
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
