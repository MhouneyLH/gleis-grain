<script setup lang="ts">
import { RouterLink, RouterView, useRoute } from 'vue-router'
import ToastHost from './components/ToastHost.vue'

const route = useRoute()
</script>

<template>
  <header v-if="!route.meta.bare" class="topnav">
    <RouterLink to="/" class="brand" aria-label="gleis-grain home">
      <svg viewBox="0 0 64 64" width="24" height="24" aria-hidden="true">
        <rect x="14" y="18" width="36" height="26" rx="7" fill="#fff" />
        <rect x="19" y="23" width="11" height="9" rx="2" fill="#d6232f" />
        <rect x="34" y="23" width="11" height="9" rx="2" fill="#d6232f" />
        <circle cx="23" cy="49" r="3.5" fill="#fff" />
        <circle cx="41" cy="49" r="3.5" fill="#fff" />
        <path d="M32 18V9M23 9h18" stroke="#fff" stroke-width="3.5" stroke-linecap="round" />
      </svg>
      <span>gleis<b>grain</b></span>
    </RouterLink>
    <nav>
      <RouterLink to="/" exact-active-class="on">Play</RouterLink>
      <RouterLink to="/alerts" active-class="on">Notices</RouterLink>
      <RouterLink to="/projector" active-class="on">Projector</RouterLink>
    </nav>
  </header>
  <RouterView />
  <ToastHost />
</template>

<style scoped>
.topnav {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1200;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 3px 10px;
  background: rgb(214 35 47 / 0.88);
  backdrop-filter: blur(4px);
  color: #fff;
  box-shadow: 0 2px 10px rgb(120 10 20 / 0.35);
}

.brand {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #fff;
  text-decoration: none;
  font-size: 1rem;
  font-weight: 700;
  letter-spacing: -0.01em;
}

.brand b {
  font-weight: 900;
}

nav {
  display: flex;
  gap: 4px;
}

nav a {
  display: grid;
  place-items: center;
  min-height: 34px;
  padding: 0 12px;
  border-radius: 999px;
  color: #ffe3e5;
  font-weight: 800;
  font-size: 0.88rem;
  text-decoration: none;
}

nav a.on {
  background: #fff;
  color: var(--red-dark);
}

@media (max-width: 420px) {
  .brand span {
    display: none;
  }
}
</style>
