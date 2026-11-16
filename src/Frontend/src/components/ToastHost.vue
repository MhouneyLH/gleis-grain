<script setup lang="ts">
import { useToasts } from '../composables/useToast'

const { toasts, dismissToast } = useToasts()
</script>

<template>
  <div class="toasts" aria-live="polite">
    <TransitionGroup name="toast">
      <button v-for="toast in toasts" :key="toast.id" class="toast" :class="toast.kind" @click="dismissToast(toast.id)">
        <strong>{{ toast.title }}</strong>
        <span v-if="toast.text">{{ toast.text }}</span>
      </button>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toasts {
  position: fixed;
  top: 64px;
  left: 0;
  right: 0;
  z-index: 3000;
  display: grid;
  justify-items: center;
  gap: 8px;
  padding: 0 12px;
  pointer-events: none;
}

.toast {
  pointer-events: auto;
  display: grid;
  gap: 2px;
  width: min(100%, 420px);
  padding: 14px 18px;
  border: 0;
  border-radius: 16px;
  text-align: left;
  color: #fff;
  background: #3a3336;
  box-shadow: 0 10px 30px rgb(0 0 0 / 0.3);
  cursor: pointer;
}

.toast strong {
  font-size: 1.15rem;
  font-weight: 900;
}

.toast.good {
  background: var(--green);
}

.toast.bad {
  background: var(--red-dark);
}

.toast-enter-active,
.toast-leave-active {
  transition: transform 0.25s ease, opacity 0.25s ease;
}

.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateY(-16px) scale(0.96);
}
</style>
