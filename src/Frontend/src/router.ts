import { createRouter, createWebHistory } from 'vue-router'

declare module 'vue-router' {
  interface RouteMeta {
    /** Pages without the top navigation (the projector). */
    bare?: boolean
    title?: string
  }
}

export const router = createRouter({
  history: createWebHistory('/'),
  routes: [
    { path: '/', name: 'audience', component: () => import('./views/AudienceView.vue'), meta: { title: 'Play' } },
    {
      path: '/projector',
      name: 'projector',
      component: () => import('./views/ProjectorView.vue'),
      meta: { bare: true, title: 'Projector' },
    },
    {
      path: '/stop/:id',
      name: 'stop',
      component: () => import('./views/StopBoardView.vue'),
      props: true,
      meta: { title: 'Departures' },
    },
    { path: '/alerts', name: 'alerts', component: () => import('./views/AlertsView.vue'), meta: { title: 'Notices' } },
    { path: '/:rest(.*)*', redirect: '/' },
  ],
})

router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} - gleis-grain` : 'gleis-grain'
})
