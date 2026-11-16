<script setup lang="ts">
import { computed } from 'vue'
import type { LeaderboardRow } from '../api'

const props = withDefaults(
  defineProps<{ rows: LeaderboardRow[]; limit?: number; mine?: string | null; big?: boolean }>(),
  { limit: 5, mine: null, big: false },
)

const shown = computed(() => props.rows.slice(0, props.limit))
const isMine = (nickname: string) => !!props.mine && nickname.toLowerCase() === props.mine.toLowerCase()
</script>

<template>
  <section class="card board" :class="{ big }">
    <h2>Leaderboard</h2>
    <ol v-if="shown.length > 0">
      <TransitionGroup name="rank">
        <li v-for="(row, index) in shown" :key="row.nickname" :class="{ me: isMine(row.nickname) }">
          <span class="rank num" :class="`r${index + 1}`">{{ index + 1 }}</span>
          <span class="name">{{ row.nickname }}</span>
          <span class="score num">{{ row.score }}</span>
        </li>
      </TransitionGroup>
    </ol>
    <p v-else class="muted">Nobody has scored yet. Place the first bet!</p>
  </section>
</template>

<style scoped>
ol {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 6px;
}

li {
  display: grid;
  grid-template-columns: 2.1em 1fr auto;
  align-items: center;
  gap: 10px;
  padding: 6px 10px;
  border-radius: 12px;
  background: var(--cream);
}

li.me {
  outline: 2px solid var(--red);
}

.rank {
  display: grid;
  place-items: center;
  width: 2em;
  height: 2em;
  border-radius: 50%;
  background: var(--hairline);
  font-weight: 900;
  font-size: 0.9em;
}

.r1 {
  background: var(--amber);
  color: #3b2a00;
}

.name {
  font-weight: 800;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.score {
  font-weight: 900;
  font-size: 1.15em;
}

.big li {
  font-size: 1.25rem;
  padding: 8px 12px;
}

.rank-move,
.rank-enter-active,
.rank-leave-active {
  transition: transform 0.4s ease, opacity 0.4s ease;
}

.rank-enter-from,
.rank-leave-to {
  opacity: 0;
  transform: translateX(14px);
}
</style>
