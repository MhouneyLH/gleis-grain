<script setup lang="ts">
import type { WatcherBar } from '../live/grainView'

defineProps<{ bars: WatcherBar[]; colorOf: (silo: string) => string }>()
</script>

<template>
  <section class="card">
    <h2>Watchers per stop</h2>
    <ul v-if="bars.length > 0" class="panel-list">
      <li v-for="bar in bars" :key="bar.stopId">
        <span class="label" :title="bar.silo ? `${bar.name} (${bar.silo})` : bar.name">
          <span class="stop">{{ bar.name }}</span>
          <span v-if="bar.silo" class="silo">({{ bar.silo }})</span>
        </span>
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
li {
  display: grid;
  grid-template-columns: minmax(9em, 50%) 1fr 2.2em;
  align-items: center;
  gap: 10px;
}

.label {
  display: flex;
  gap: 0.4em;
  min-width: 0;
}

/* A long stop name is cut with an ellipsis, the silo name stays whole. */
.stop {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.silo {
  flex: none;
  color: var(--muted);
  font-size: 0.85em;
  line-height: 1.6;
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
