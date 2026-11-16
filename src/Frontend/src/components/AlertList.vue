<script setup lang="ts">
import { RouterLink } from 'vue-router'
import type { Alert } from '../api'
import { useStops } from '../composables/useStops'
import { formatDateTime } from '../live/format'
import LineBadge from './LineBadge.vue'

defineProps<{ alerts: Alert[]; empty?: string }>()

const { stopName } = useStops()
</script>

<template>
  <ul v-if="alerts.length > 0" class="alerts">
    <li v-for="alert in alerts" :key="alert.id" class="card">
      <h3>{{ alert.title }}</h3>
      <p v-if="alert.text" class="text">{{ alert.text }}</p>
      <div class="meta">
        <LineBadge v-for="line in alert.lines" :key="line" :line="line" />
        <RouterLink v-for="id in alert.stopIds" :key="id" :to="`/stop/${id}`" class="chip">{{ stopName(id) }}</RouterLink>
        <span v-if="alert.firstSeenAt" class="muted since">since {{ formatDateTime(alert.firstSeenAt) }}</span>
      </div>
    </li>
  </ul>
  <p v-else class="muted none">{{ empty ?? 'No notices. All quiet on the rails.' }}</p>
</template>

<style scoped>
.alerts {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 12px;
}

li {
  display: grid;
  gap: 8px;
  border-left: 8px solid var(--amber);
}

h3 {
  font-size: 1.15rem;
  font-weight: 900;
  line-height: 1.25;
}

.text {
  white-space: pre-line;
}

.meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px 8px;
}

.chip {
  padding: 4px 12px;
  border-radius: 999px;
  background: var(--red-soft);
  color: var(--red-dark);
  font-weight: 800;
  font-size: 0.9rem;
  text-decoration: none;
}

.since {
  font-size: 0.85rem;
  margin-left: auto;
}

.none {
  padding: 18px 4px;
}
</style>
