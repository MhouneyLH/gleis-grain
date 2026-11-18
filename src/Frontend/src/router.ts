import { createRouter, createWebHistory } from 'vue-router'

declare module 'vue-router' {
  interface RouteMeta {
    /** Pages without the top navigation (the overview). */
    bare?: boolean
    title?: string
  }
}

export const router = createRouter({
  history: createWebHistory('/'),
  routes: [
    { path: '/', name: 'map', component: () => import('./views/MapView.vue'), meta: { title: 'Map' } },
    {
      path: '/overview',
      name: 'overview',
      component: () => import('./views/OverviewView.vue'),
      meta: { bare: true, title: 'Overview' },
    },
    // Old bookmarks and slides.
    { path: '/projector', redirect: '/overview' },
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
