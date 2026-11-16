<script setup lang="ts">
import type { TickerEntry } from '../live/ticker'

defineProps<{ entries: TickerEntry[] }>()

const clock = new Intl.DateTimeFormat('de-AT', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
</script>

<template>
  <section class="card">
    <h2>Live ticker</h2>
    <ul v-if="entries.length > 0">
      <TransitionGroup name="tick">
        <li v-for="entry in entries" :key="entry.id" :class="entry.kind.replace('.', '-')">
          <time class="num muted">{{ clock.format(entry.at) }}</time>
          <span>{{ entry.text }}</span>
        </li>
      </TransitionGroup>
    </ul>
    <p v-else class="muted">Waiting for events...</p>
  </section>
</template>

<style scoped>
ul {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 4px;
}

li {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 10px;
  padding: 3px 0 3px 10px;
  border-left: 4px solid var(--hairline);
  font-weight: 700;
  font-size: 0.92em;
}

time {
  font-size: 0.85em;
}

li.stop-activated {
  border-color: var(--green);
}

li.stop-deactivated {
  border-color: #6b7686;
}

li.bet-resolved,
li.bet-placed,
li.leaderboard-changed {
  border-color: var(--amber);
}

li.chaos {
  border-color: var(--red);
  color: var(--red);
}

.tick-enter-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}

.tick-enter-from {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
