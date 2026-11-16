<script setup lang="ts">
import { computed } from 'vue'
import type { Silo } from '../api'

const props = defineProps<{ silos: Silo[]; colorOf: (silo: string) => string; hosted: Record<string, number> }>()

const isDead = (silo: Silo) => silo.status.toLowerCase() === 'dead'
const sorted = computed(() => [...props.silos].sort((a, b) => a.name.localeCompare(b.name)))
</script>

<template>
  <section class="card">
    <h2>Silos</h2>
    <ul v-if="sorted.length > 0">
      <li v-for="silo in sorted" :key="silo.name" :class="{ dead: isDead(silo) }">
        <i class="swatch" :style="{ background: isDead(silo) ? 'transparent' : colorOf(silo.name) }"></i>
        <span class="name">{{ silo.name }}</span>
        <span v-if="!isDead(silo)" class="hosted num">{{ hosted[silo.name] ?? 0 }} stops</span>
        <span class="status">{{ silo.status }}</span>
      </li>
    </ul>
    <p v-else class="muted">No silo information yet.</p>
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
  grid-template-columns: auto 1fr auto auto;
  align-items: center;
  gap: 10px;
  font-weight: 800;
}

.swatch {
  width: 1em;
  height: 1em;
  border-radius: 50%;
  border: 2px solid transparent;
}

.name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.hosted {
  color: var(--muted);
  font-size: 0.85em;
}

.status {
  padding: 1px 10px;
  border-radius: 999px;
  background: var(--green-soft);
  color: var(--green);
  font-size: 0.8em;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

li.dead {
  color: var(--red);
}

li.dead .name {
  text-decoration: line-through;
}

li.dead .swatch {
  border-color: var(--red);
}

li.dead .status {
  background: var(--red-soft);
  color: var(--red);
}
</style>
