<script setup lang="ts">
import { lineColor } from '../live/format'

defineProps<{ line: string }>()
</script>

<template>
  <span class="line" :style="{ background: lineColor(line) }">{{ line }}</span>
</template>
