<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { api, ApiError, type Alert, type StopBoard } from '../api'
import AlertList from '../components/AlertList.vue'
import DepartureTable from '../components/DepartureTable.vue'
import { usePolling } from '../composables/usePolling'
import { useStops } from '../composables/useStops'
import { formatTime } from '../live/format'

const props = defineProps<{ id: string }>()

const router = useRouter()
const { stops } = useStops()
const sortedStops = computed(() => [...stops.value].sort((a, b) => a.name.localeCompare(b.name)))

const board = ref<StopBoard | null>(null)
const alerts = ref<Alert[]>([])
const error = ref<string | null>(null)
const notFound = ref(false)

async function load(): Promise<void> {
  const id = props.id
  const [boardResult, alertResult] = await Promise.allSettled([api.board(id), api.alerts({ stop: id })])
  if (id !== props.id) return // the route changed while loading
  if (boardResult.status === 'fulfilled') {
    board.value = boardResult.value
    error.value = null
    notFound.value = false
  } else {
    const reason: unknown = boardResult.reason
    notFound.value = reason instanceof ApiError && reason.status === 404
    error.value = reason instanceof ApiError ? reason.friendly : 'The board could not be loaded.'
  }
  if (alertResult.status === 'fulfilled') alerts.value = alertResult.value
}

const { refresh } = usePolling(load, 15_000)

watch(
  () => props.id,
  () => {
    board.value = null
    alerts.value = []
    error.value = null
    notFound.value = false
    void refresh()
  },
)
</script>

<template>
  <main class="page board-page">
    <header>
      <select v-if="stops.length > 0" class="field picker" :value="id" aria-label="Choose a stop" @change="router.push(`/stop/${($event.target as HTMLSelectElement).value}`)">
        <option v-for="stop in sortedStops" :key="stop.id" :value="stop.id">{{ stop.name }}</option>
      </select>
      <h1 class="page-title">{{ board?.name ?? (notFound ? 'Unknown stop' : 'Departures') }}</h1>
      <p v-if="board" class="muted num">
        {{ board.watchers }} {{ board.watchers === 1 ? 'watcher' : 'watchers' }} · updated {{ formatTime(board.refreshedAt) }} · refreshes every 15 s
      </p>
    </header>

    <p v-if="error" class="error-text" role="alert">{{ error }}</p>
    <p v-else-if="!board" class="muted">Loading the board...</p>

    <DepartureTable v-if="board" :departures="board.departures" />

    <section v-if="board || alerts.length > 0">
      <h2 class="section">Notices for this stop</h2>
      <AlertList :alerts="alerts" empty="No notices for this stop." />
    </section>
  </main>
</template>

<style scoped>
.board-page {
  max-width: 1100px;
}

.picker {
  max-width: 360px;
  margin-bottom: 10px;
}

.section {
  margin-bottom: 10px;
}
</style>
