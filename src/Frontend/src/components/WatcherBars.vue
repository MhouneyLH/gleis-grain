<script setup lang="ts">
import type { WatcherBar } from '../live/grainView'

defineProps<{ bars: WatcherBar[]; colorOf: (silo: string) => string }>()
</script>

<template>
  <section class="card">
    <h2>Watchers per stop</h2>
    <ul v-if="bars.length > 0">
      <li v-for="bar in bars" :key="bar.stopId">
        <span class="label">{{ bar.name }}</span>
        <span class="track">
          <span class="fill" :style="{ width: `${Math.max(bar.share * 100, 4)}%`, background: bar.silo ? colorOf(bar.silo) : '#6b7686' }"></span>
        </span>
        <span class="count num">{{ bar.watchers }}</span>
      </li>
    </ul>
    <p v-else class="muted">Nobody watches a stop yet.</p>
  </section>
</template>

<style scoped>
ul {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 6px;
}

li {
  display: grid;
  grid-template-columns: minmax(6em, 34%) 1fr 2.2em;
  align-items: center;
  gap: 10px;
  font-weight: 800;
}

.label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.track {
  height: 1.1em;
  border-radius: 999px;
  background: var(--cream);
  overflow: hidden;
}

.fill {
  display: block;
  height: 100%;
  border-radius: 999px;
  transition: width 0.5s ease;
}

.count {
  text-align: right;
  font-weight: 900;
}
</style>
