<script setup lang="ts">
import { computed } from 'vue'
import type { Silo } from '../api'
import { shapeSvg } from '../live/siloShapes'
import { isDeadStatus, type SiloInfo } from '../live/siloSlots'

const props = defineProps<{ silos: Silo[]; infoOf: (silo: string) => SiloInfo; hosted: Record<string, number> }>()

const sorted = computed(() =>
  props.silos
    .map((silo) => ({ silo, info: props.infoOf(silo.name), dead: isDeadStatus(silo.status) }))
    .sort((a, b) => a.info.version - b.info.version || a.silo.name.localeCompare(b.silo.name)),
)
</script>

<template>
  <section class="card">
    <h2>Silos</h2>
    <ul v-if="sorted.length > 0" class="panel-list">
      <li v-for="row in sorted" :key="row.silo.name" :class="{ dead: row.dead }">
        <span class="icon" v-html="shapeSvg(row.info.shape, row.dead ? 'transparent' : row.info.color, 22)"></span>
        <span class="ver" :style="row.dead ? undefined : { background: row.info.color }">v{{ row.info.version }}</span>
        <span class="name">{{ row.silo.name }}</span>
        <span v-if="!row.dead" class="hosted num">{{ hosted[row.silo.name] ?? 0 }} stops</span>
        <span class="status">{{ row.silo.status }}</span>
      </li>
    </ul>
    <p v-else class="muted">No silo information yet.</p>
  </section>
</template>

<style scoped>
li {
  display: grid;
  grid-template-columns: auto auto 1fr auto auto;
  align-items: center;
  gap: 10px;
}

.icon {
  display: inline-flex;
}

.ver {
  min-width: 2.2em;
  padding: 0 8px;
  border-radius: 8px;
  color: #0d1117;
  text-align: center;
  font-weight: 900;
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

li.dead .ver {
  background: none;
  color: var(--red);
  border: 1px solid var(--red);
  text-decoration: line-through;
}

li.dead .status {
  background: var(--red-soft);
  color: var(--red);
}
</style>
