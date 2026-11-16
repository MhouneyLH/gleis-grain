<script setup lang="ts">
import { computed } from 'vue'
import type { ClusterStats } from '../api'
import { cacheHitRatio, shortTypeName } from '../live/format'

const props = defineProps<{ stats: ClusterStats | null }>()

const calls = computed(() => props.stats?.ingestion.calls ?? 0)
const hits = computed(() => props.stats?.ingestion.cacheHits ?? 0)
const ratio = computed(() => cacheHitRatio(calls.value, hits.value))
const percent = computed(() => Math.round(ratio.value * 100))
const grains = computed(() =>
  Object.entries(props.stats?.activeGrains ?? {})
    .map(([type, count]) => ({ type: shortTypeName(type), count }))
    .sort((a, b) => b.count - a.count || a.type.localeCompare(b.type)),
)
</script>

<template>
  <section class="card">
    <h2>Stats</h2>
    <template v-if="stats">
      <div class="numbers">
        <div>
          <strong class="num calls">{{ calls }}</strong>
          <span>calls to Linz AG</span>
        </div>
        <div>
          <strong class="num hits">{{ hits }}</strong>
          <span>cache hits</span>
        </div>
      </div>
      <div class="ratio" role="img" :aria-label="`${percent} percent served from the cache`">
        <span class="seg-calls" :style="{ flexGrow: calls }"></span>
        <span class="seg-hits" :style="{ flexGrow: hits }"></span>
      </div>
      <p class="muted ratio-text">{{ percent }}% served from the grains' cache</p>
      <ul v-if="grains.length > 0" class="grains">
        <li v-for="grain in grains" :key="grain.type">
          <span>{{ grain.type }}</span>
          <strong class="num">{{ grain.count }}</strong>
        </li>
      </ul>
    </template>
    <p v-else class="muted">Waiting for the first numbers...</p>
  </section>
</template>

<style scoped>
.numbers {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.numbers div {
  display: grid;
  line-height: 1.1;
}

.numbers strong {
  font-size: 2.2em;
  font-weight: 900;
}

.numbers span {
  color: var(--muted);
  font-weight: 700;
  font-size: 0.85em;
}

.calls {
  color: var(--red);
}

.hits {
  color: var(--green);
}

.ratio {
  display: flex;
  height: 14px;
  margin: 10px 0 4px;
  border-radius: 999px;
  overflow: hidden;
  background: var(--hairline);
}

.seg-calls {
  background: var(--red);
  transition: flex-grow 0.5s ease;
}

.seg-hits {
  background: var(--green);
  transition: flex-grow 0.5s ease;
}

.ratio-text {
  font-size: 0.85em;
  font-weight: 700;
}

.grains {
  list-style: none;
  margin: 12px 0 0;
  padding: 10px 0 0;
  border-top: 1px dashed var(--hairline);
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 4px 18px;
}

.grains li {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  font-weight: 700;
}
</style>
