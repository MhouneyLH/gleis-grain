<script setup lang="ts">
// A translucent dark panel floating over the map. The head collapses it; the card inside loses its own frame and title.
defineProps<{ title: string }>()
const collapsed = defineModel<boolean>('collapsed', { default: false })
</script>

<template>
  <section class="fp" :class="{ collapsed }">
    <button class="head" type="button" :aria-expanded="!collapsed" @click="collapsed = !collapsed">
      <span class="title">{{ title }}</span>
      <slot name="meta"></slot>
      <svg class="chev" viewBox="0 0 24 24" width="1.1em" height="1.1em" aria-hidden="true">
        <path d="M6 9l6 6 6-6" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </button>
    <div v-show="!collapsed" class="body">
      <slot></slot>
    </div>
  </section>
</template>

<style scoped>
.fp {
  pointer-events: auto;
  border-radius: 16px;
  background: rgb(13 17 23 / 0.78);
  border: 1.5px solid rgb(255 255 255 / 0.14);
  box-shadow: 0 8px 28px rgb(0 0 0 / 0.45);
  backdrop-filter: blur(6px);
  overflow: hidden;
}

.head {
  display: flex;
  align-items: center;
  gap: 0.6em;
  width: 100%;
  padding: 0.55em 0.9em;
  border: 0;
  background: transparent;
  color: var(--muted);
  cursor: pointer;
  text-align: left;
}

.title {
  flex: 1;
  font-size: 0.8em;
  font-weight: 800;
  letter-spacing: 0.09em;
  text-transform: uppercase;
}

.chev {
  flex: none;
  transition: transform 0.15s ease;
}

.collapsed .chev {
  transform: rotate(-90deg);
}

.body {
  padding: 0 0.9em 0.8em;
}

/* The wrapped cards bring their own frame and title; the panel replaces both. */
.body :deep(.card) {
  padding: 0;
  background: transparent;
  box-shadow: none;
  border: 0;
  border-radius: 0;
}

.body :deep(.card > h2) {
  display: none;
}
</style>
