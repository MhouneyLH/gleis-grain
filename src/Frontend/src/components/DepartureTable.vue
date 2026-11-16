<script setup lang="ts">
import type { Departure } from '../api'
import { delayLabel, delayLevel, formatTime } from '../live/format'
import LineBadge from './LineBadge.vue'

defineProps<{ departures: Departure[] }>()
</script>

<template>
  <div class="wrap">
    <table>
      <thead>
        <tr>
          <th>Line</th>
          <th>Direction</th>
          <th class="r">Planned</th>
          <th class="r">Delay</th>
          <th class="r">Platform</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="dep in departures" :key="dep.tripId">
          <td><LineBadge :line="dep.line" /></td>
          <td class="dir">{{ dep.direction }}</td>
          <td class="r num">{{ formatTime(dep.plannedAt) }}</td>
          <td class="r num delay" :class="`delay-${delayLevel(dep.delayMinutes)}`">{{ delayLabel(dep.delayMinutes) }}</td>
          <td class="r">{{ dep.platform ?? '-' }}</td>
        </tr>
        <tr v-if="departures.length === 0">
          <td colspan="5" class="empty muted">No departures right now.</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.wrap {
  overflow-x: auto;
  border-radius: var(--radius);
  background: var(--paper);
  box-shadow: var(--shadow);
}

table {
  width: 100%;
  border-collapse: collapse;
  font-size: clamp(1.05rem, 2.6vw, 1.5rem);
}

th {
  padding: 14px 16px;
  text-align: left;
  font-size: 0.7em;
  letter-spacing: 0.09em;
  text-transform: uppercase;
  color: var(--muted);
  background: var(--cream);
}

td {
  padding: 14px 16px;
  border-top: 1px solid var(--hairline);
  font-weight: 700;
}

.r {
  text-align: right;
}

.dir {
  font-weight: 800;
}

.line {
  font-size: 1.1em;
}

.delay {
  font-weight: 900;
}

@media (max-width: 520px) {
  table {
    font-size: 0.95rem;
  }

  th {
    font-size: 0.62em;
    letter-spacing: 0.04em;
  }

  th,
  td {
    padding: 10px 5px;
  }

  .line {
    font-size: 1em;
    padding: 2px 8px;
  }

  th:first-child,
  td:first-child {
    padding-left: 12px;
  }
}

.empty {
  padding: 28px 16px;
  text-align: center;
}
</style>
