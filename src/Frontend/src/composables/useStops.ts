import { computed, ref } from 'vue'
import { api, type Stop } from '../api'
import { retryWithBackoff } from './retry'

// The catalogue is static, so it is loaded once and shared by all pages.
// While the backend is not reachable the load is retried with backoff (1 s, 2 s, 4 s ... 10 s) until it works.
const stops = ref<Stop[]>([])
/** Only set while the load is failing and being retried. */
const error = ref<string | null>(null)
let pending: Promise<void> | null = null

const byId = computed(() => new Map(stops.value.map((stop) => [stop.id, stop])))

function load(): Promise<void> {
  pending ??= retryWithBackoff(
    () => api.stops(),
    (e) => {
      error.value = `Could not load the stops (${e instanceof Error ? e.message : 'unknown error'}), trying again ...`
    },
  ).then((list) => {
    stops.value = list
    error.value = null
  })
  return pending
}

export function useStops() {
  void load()
  return {
    stops,
    error,
    stopById: (id: string) => byId.value.get(id),
    stopName: (id: string) => byId.value.get(id)?.name ?? id,
  }
}
