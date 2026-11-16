<script setup lang="ts">
import L from 'leaflet'
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { Stop } from '../api'
import { createBaseMap, fitToStops, type BaseMap } from '../map/baseMap'

const props = defineProps<{ stops: Stop[]; selectedId: string | null; watchedId: string | null }>()
const emit = defineEmits<{ select: [id: string] }>()

const element = ref<HTMLDivElement | null>(null)

// Leaflet objects stay outside of Vue's reactivity.
let base: BaseMap | null = null
const dots = new Map<string, L.CircleMarker>()
let fitted = false

function styleFor(id: string): L.CircleMarkerOptions {
  if (id === props.selectedId) return { radius: 13, color: '#261f21', weight: 4, fillColor: '#f2a900', fillOpacity: 1 }
  if (id === props.watchedId) return { radius: 12, color: '#fff', weight: 4, fillColor: '#1c8a57', fillOpacity: 1 }
  return { radius: 10, color: '#fff', weight: 3, fillColor: '#d6232f', fillOpacity: 1 }
}

function draw(): void {
  const map = base?.map
  if (!map) return
  for (const stop of props.stops) {
    if (dots.has(stop.id)) continue
    const position: L.LatLngTuple = [stop.latitude, stop.longitude]
    const dot = L.circleMarker(position, { ...styleFor(stop.id), interactive: false }).addTo(map)
    dot.bindTooltip(stop.name, { permanent: true, direction: 'right', offset: [10, 0], className: 'stop-label' })
    // A larger invisible circle on top makes the touch target about 40px.
    L.circleMarker(position, { radius: 20, stroke: false, fillOpacity: 0, fill: true })
      .on('click', () => emit('select', stop.id))
      .addTo(map)
    dots.set(stop.id, dot)
  }
  if (!fitted && props.stops.length > 0) {
    fitToStops(
      map,
      props.stops.map((stop) => [stop.latitude, stop.longitude] as L.LatLngTuple),
    )
    fitted = true
  }
  restyle()
}

function restyle(): void {
  for (const [id, dot] of dots) {
    dot.setStyle(styleFor(id))
    if (id === props.selectedId) dot.bringToFront()
  }
}

onMounted(() => {
  if (element.value) base = createBaseMap(element.value)
  draw()
})
onBeforeUnmount(() => base?.destroy())

watch(() => props.stops, draw)
watch(() => [props.selectedId, props.watchedId], restyle)
</script>

<template>
  <div ref="element" class="stop-map" role="application" aria-label="Map of the stops in Linz"></div>
</template>

<style scoped>
.stop-map {
  width: 100%;
  height: 100%;
  min-height: 280px;
  border-radius: var(--radius);
  background-image: radial-gradient(circle, #d6dde1 1px, transparent 1px);
  background-size: 22px 22px;
}
</style>
