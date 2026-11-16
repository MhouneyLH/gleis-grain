<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'
import { api, ApiError, type Alert } from '../api'
import AlertList from '../components/AlertList.vue'
import { useStops } from '../composables/useStops'

const { stops } = useStops()

const line = ref('')
const stop = ref('')
const alerts = ref<Alert[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

let timer: ReturnType<typeof setTimeout> | undefined
let generation = 0

/** A stop name from the list is sent as its id; anything else is sent as typed. */
function stopFilter(): string {
  const typed = stop.value.trim()
  const match = stops.value.find((s) => s.name.toLowerCase() === typed.toLowerCase())
  return match?.id ?? typed
}

async function load(): Promise<void> {
  const mine = ++generation
  loading.value = true
  try {
    const result = await api.alerts({ line: line.value.trim(), stop: stopFilter() })
    if (mine !== generation) return
    alerts.value = result
    error.value = null
  } catch (e) {
    if (mine !== generation) return
    error.value = e instanceof ApiError ? e.friendly : 'The notices could not be loaded.'
  } finally {
    if (mine === generation) loading.value = false
  }
}

// Wait for a short pause in typing before asking the server.
watch([line, stop], () => {
  clearTimeout(timer)
  timer = setTimeout(() => void load(), 300)
})
onBeforeUnmount(() => clearTimeout(timer))

void load()
</script>

<template>
  <main class="page alerts-page">
    <h1 class="page-title">Service notices</h1>

    <form class="filters card" @submit.prevent="load">
      <label>
        <span class="muted">Line</span>
        <input v-model="line" class="field" inputmode="text" autocomplete="off" placeholder="e.g. 1" />
      </label>
      <label>
        <span class="muted">Stop</span>
        <input v-model="stop" class="field" list="stop-names" autocomplete="off" placeholder="name or id" />
        <datalist id="stop-names">
          <option v-for="s in stops" :key="s.id" :value="s.name" />
        </datalist>
      </label>
    </form>

    <p v-if="error" class="error-text" role="alert">{{ error }}</p>
    <p v-else-if="loading && alerts.length === 0" class="muted">Loading...</p>
    <AlertList v-else :alerts="alerts" :empty="line || stop ? 'No notice matches these filters.' : undefined" />
  </main>
</template>

<style scoped>
.alerts-page {
  max-width: 760px;
}

.filters {
  display: grid;
  grid-template-columns: 1fr 2fr;
  gap: 12px;
}

label {
  display: grid;
  gap: 4px;
  font-weight: 800;
  font-size: 0.9rem;
}

@media (max-width: 480px) {
  .filters {
    grid-template-columns: 1fr;
  }
}
</style>
