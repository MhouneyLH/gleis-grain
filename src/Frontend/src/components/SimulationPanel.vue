<script setup lang="ts">
import { computed, ref } from 'vue'
import { api, type Scenario } from '../api'
import { usePolling } from '../composables/usePolling'
import { HOT_STOP, PRESETS, SIM_HINT, isSimulatorDown, presetLabel, scenarioView } from '../live/simulation'

const props = defineProps<{ selectedId: string | null; stopName: (id: string) => string; open: boolean }>()
const emit = defineEmits<{ scenario: [Scenario | null] }>()

const scenario = ref<Scenario | null>(null)
const down = ref(false)
const error = ref<string | null>(null)
const busy = ref(false)

const view = computed(() => (scenario.value ? scenarioView(scenario.value, props.stopName) : null))
const selectedName = computed(() => (props.selectedId ? props.stopName(props.selectedId) : null))

function accept(next: Scenario | null): void {
  scenario.value = next
  emit('scenario', next)
}

async function load(): Promise<void> {
  try {
    accept(await api.scenario())
    down.value = false
    error.value = null
  } catch (e) {
    handle(e)
  }
}

function handle(e: unknown): void {
  if (isSimulatorDown(e)) {
    down.value = true
    error.value = null
    accept(null)
  } else {
    error.value = e instanceof Error ? e.message : 'The simulator rejected the request.'
  }
}

/** Runs a change, then reloads the scenario (the answer is the new scenario, the reload also refreshes `joined`). */
async function act(call: () => Promise<Scenario>): Promise<void> {
  busy.value = true
  try {
    accept(await call())
    down.value = false
    error.value = null
  } catch (e) {
    handle(e)
  } finally {
    busy.value = false
  }
  void load()
}

const pin = (people: number) => props.selectedId && act(() => api.pinPeople(props.selectedId as string, people))
const hot = () => act(() => api.pinPeople(HOT_STOP.id, HOT_STOP.people))
const releaseAll = () => act(() => api.releaseAll())

// Polls while the panel is open (and loads right away when it opens).
usePolling(load, 5000, () => props.open)
</script>

<template>
  <div class="sim panel-stack">
    <p v-if="down" class="hint" role="status">{{ SIM_HINT }}</p>
    <template v-else>
      <div v-if="view" class="stat-row">
        <div>
          <strong class="num">{{ view.people }}</strong>
          <span>people</span>
        </div>
        <div>
          <strong class="num">{{ view.joined }}</strong>
          <span>joined</span>
        </div>
      </div>
      <p v-else class="muted">Loading the scenario...</p>

      <ul v-if="view && view.rows.length > 0" class="groups panel-list">
        <li v-for="row in view.rows" :key="row.stopId" :class="{ sel: row.stopId === selectedId }">
          <span class="gname">{{ row.name }}</span>
          <span class="gpeople num">{{ row.people }}</span>
        </li>
      </ul>
      <p v-else-if="view" class="muted">Nobody pinned. The crowd spreads out by itself.</p>

      <p class="selected" :class="{ muted: !selectedName }">
        <template v-if="selectedName">Selected: <b>{{ selectedName }}</b></template>
        <template v-else>Click a stop on the map to select it</template>
      </p>
      <div class="presets">
        <button
          v-for="n in PRESETS"
          :key="n"
          class="btn btn-small btn-ghost"
          :disabled="busy || !selectedId"
          @click="pin(n)"
        >
          {{ presetLabel(n) }}
        </button>
      </div>
      <div class="wide">
        <button class="btn btn-small" :disabled="busy" @click="hot">Hot station: {{ HOT_STOP.name }} {{ HOT_STOP.people }}</button>
        <button class="btn btn-small btn-ghost" :disabled="busy" @click="releaseAll">Release all</button>
      </div>
      <p v-if="error" class="error-text" role="alert">{{ error }}</p>
    </template>
  </div>
</template>

<style scoped>
.hint {
  padding: 0.7em 0.9em;
  border-radius: 12px;
  background: var(--amber-soft);
  color: var(--amber);
  font-weight: 800;
  line-height: 1.3;
}

.groups {
  max-height: 9em;
  overflow-y: auto;
}

.groups li {
  display: flex;
  justify-content: space-between;
  gap: 1em;
}

.groups li.sel {
  color: var(--amber);
}

.gpeople {
  font-weight: 900;
}

.selected {
  font-weight: 800;
}

.selected b {
  color: var(--amber);
}

.presets {
  display: grid;
  grid-template-columns: repeat(4, 1fr) 1.9fr;
  gap: 6px;
}

.presets .btn {
  padding: 0 6px;
}

.wide {
  display: grid;
  gap: 6px;
}
</style>
