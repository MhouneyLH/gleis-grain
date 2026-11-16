import { computed, ref } from 'vue'
import { api, type Stop } from '../api'

// The catalogue is static, so it is loaded once and shared by all pages.
const stops = ref<Stop[]>([])
const error = ref<string | null>(null)
let pending: Promise<void> | null = null

const byId = computed(() => new Map(stops.value.map((stop) => [stop.id, stop])))

function load(): Promise<void> {
  pending ??= api
    .stops()
    .then((list) => {
      stops.value = list
      error.value = null
    })
    .catch((e: unknown) => {
      error.value = e instanceof Error ? e.message : 'Could not load the stops.'
      pending = null
    })
  return pending
}

export function useStops() {
  void load()
  return {
    stops,
    error,
    reload: load,
    stopById: (id: string) => byId.value.get(id),
    stopName: (id: string) => byId.value.get(id)?.name ?? id,
  }
}
