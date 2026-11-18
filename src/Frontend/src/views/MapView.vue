<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import StopMap from '../components/StopMap.vue'
import StopSheet from '../components/StopSheet.vue'
import { usePlayer } from '../composables/usePlayer'
import { usePolling } from '../composables/usePolling'
import { useStops } from '../composables/useStops'

const { stops, error: stopsError, stopName } = useStops()
const { nickname, player, ensureGuest, refreshPlayer } = usePlayer()

const selectedId = ref<string | null>(null)

const watchedName = computed(() => (player.value?.watchedStopId ? stopName(player.value.watchedStopId) : null))

onMounted(() => {
  // No prompt: a stored guest name is reused, otherwise one is claimed right away.
  void (nickname.value ? refreshPlayer() : ensureGuest())
})

// Keeps the own state (watched stop) fresh; also retries the guest name when the host was not reachable at first.
usePolling(() => (nickname.value ? refreshPlayer() : ensureGuest()), 20_000)
</script>

<template>
  <main class="map-page">
    <StopMap
      class="full-map"
      :stops="stops"
      :selected-id="selectedId"
      :watched-id="player?.watchedStopId ?? null"
      @select="selectedId = $event"
    />

    <section class="status card" aria-label="You">
      <div class="who">
        <span class="muted">You are</span>
        <strong>{{ nickname ?? '...' }}</strong>
      </div>
      <p class="line-info">
        <span class="muted">Watching</span>
        <strong>{{ watchedName ?? 'no stop yet, tap one' }}</strong>
      </p>
      <p v-if="stopsError" class="error-text" role="alert">{{ stopsError }}</p>
    </section>

    <StopSheet v-if="selectedId" :key="selectedId" :stop-id="selectedId" @close="selectedId = null" />
  </main>
</template>

<style scoped>
.map-page {
  position: fixed;
  inset: 0;
  overflow: hidden;
}

.full-map {
  position: absolute;
  inset: 0;
}

.status {
  position: absolute;
  z-index: 1000;
  top: 52px;
  left: 10px;
  max-width: min(420px, calc(100vw - 20px));
  display: grid;
  gap: 2px;
  padding: 8px 14px;
  border-left: 6px solid var(--red);
  background: rgb(255 255 255 / 0.93);
}

.who {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 2px 8px;
}

.who strong {
  font-size: 1.15rem;
  font-weight: 900;
}

.line-info {
  display: flex;
  flex-wrap: wrap;
  gap: 0 8px;
  font-size: 0.95rem;
}
</style>
