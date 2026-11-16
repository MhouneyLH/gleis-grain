<script setup lang="ts">
import L from 'leaflet'
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { Stop } from '../api'
import type { GrainView, StopDot } from '../live/grainView'
import { NO_SILO_COLOR } from '../live/siloColors'
import { createBaseMap, fitToStops, type BaseMap } from '../map/baseMap'

const props = defineProps<{ stops: Stop[]; view: GrainView; colorOf: (silo: string) => string }>()

const element = ref<HTMLDivElement | null>(null)

let base: BaseMap | null = null
const markers = new Map<string, { marker: L.Marker; signature: string; flashSeq: number }>()
let fitted = false

const escapeHtml = (text: string) =>
  text.replace(/[&<>"']/g, (ch) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[ch] ?? ch)

/** Dormant stops are small and grey, active ones grow with their watchers. */
function sizeOf(dot: StopDot | undefined): number {
  if (!dot?.active) return 18
  return Math.round(30 + Math.min(Math.sqrt(dot.watchers) * 8, 36))
}

function iconFor(stop: Stop, dot: StopDot | undefined, flashing: boolean): L.DivIcon {
  const size = sizeOf(dot)
  const color = dot?.active && dot.silo ? props.colorOf(dot.silo) : NO_SILO_COLOR
  const state = dot?.active ? 'on' : 'off'
  const flash = flashing && dot?.flashKind ? dot.flashKind : ''
  const badge = dot && dot.watchers > 0 ? `<b class="gd-badge">${dot.watchers}</b>` : ''
  return L.divIcon({
    className: 'gd-icon',
    iconSize: [size, size],
    iconAnchor: [size / 2, size / 2],
    html:
      `<div class="gd ${state} ${flash}" style="--c:${color}">` +
      `<i class="gd-ring"></i><i class="gd-core"></i>${badge}` +
      `<span class="gd-name">${escapeHtml(stop.name)}</span></div>`,
  })
}

function render(): void {
  const map = base?.map
  if (!map) return
  for (const stop of props.stops) {
    const dot = props.view.dots[stop.id]
    const signature = `${dot?.active ?? false}|${dot?.silo ?? ''}|${dot?.watchers ?? 0}|${dot?.flashSeq ?? 0}`
    const known = markers.get(stop.id)
    if (known && known.signature === signature) continue

    // A new element restarts the CSS animation; only flash when the flash counter moved.
    const flashing = (dot?.flashSeq ?? 0) !== (known?.flashSeq ?? 0)
    const icon = iconFor(stop, dot, flashing)
    const zIndexOffset = dot?.active ? 1000 + (dot.watchers ?? 0) : 0

    if (known) {
      known.marker.setIcon(icon)
      known.marker.setZIndexOffset(zIndexOffset)
      known.signature = signature
      known.flashSeq = dot?.flashSeq ?? 0
    } else {
      const marker = L.marker([stop.latitude, stop.longitude], {
        icon,
        zIndexOffset,
        interactive: false,
        keyboard: false,
      }).addTo(map)
      markers.set(stop.id, { marker, signature, flashSeq: dot?.flashSeq ?? 0 })
    }
  }
  if (!fitted && props.stops.length > 0) {
    fitToStops(
      map,
      props.stops.map((stop) => [stop.latitude, stop.longitude] as L.LatLngTuple),
      [70, 70],
    )
    fitted = true
  }
}

onMounted(() => {
  if (element.value) base = createBaseMap(element.value, { zoomControl: false })
  render()
})
onBeforeUnmount(() => base?.destroy())

watch(() => props.stops, render)
watch(() => props.view.dots, render, { deep: true })
</script>

<template>
  <div ref="element" class="grain-map" role="img" aria-label="Map of the stop grains and the silos hosting them"></div>
</template>

<style scoped>
.grain-map {
  width: 100%;
  height: 100%;
  background-image: radial-gradient(circle, #1b2430 1px, transparent 1px);
  background-size: 26px 26px;
}
</style>

<style>
/* Not scoped: Leaflet creates these elements itself. */
.grain-map .osm-tiles {
  filter: invert(1) hue-rotate(180deg) brightness(0.72) contrast(0.9) saturate(0.45);
}

.grain-map .leaflet-control-attribution {
  background: rgb(13 17 23 / 0.75);
  color: #94a0b4;
}

.grain-map .leaflet-control-attribution a {
  color: #b9c4d6;
}

.gd-icon {
  background: none;
  border: 0;
}

.gd {
  position: relative;
  width: 100%;
  height: 100%;
}

.gd i {
  position: absolute;
  border-radius: 50%;
}

.gd-core {
  inset: 14%;
  background: var(--c);
  border: 3px solid rgb(255 255 255 / 0.92);
  transition: background 0.4s ease, box-shadow 0.4s ease;
}

.gd.on .gd-core {
  box-shadow: 0 0 22px 5px var(--c);
}

.gd.off .gd-core {
  inset: 12%;
  background: #353e4d;
  border: 2px solid #55617a;
}

.gd-ring {
  inset: 0;
  border: 4px solid var(--c);
  opacity: 0;
}

.gd.woke .gd-ring,
.gd.moved .gd-ring {
  animation: gd-ring 1.1s ease-out 3;
}

.gd.moved .gd-core {
  animation: gd-blink 0.45s ease-in-out 5;
}

.gd-badge {
  position: absolute;
  right: -12px;
  top: -12px;
  min-width: 28px;
  height: 28px;
  padding: 0 7px;
  border-radius: 14px;
  background: #fff;
  color: #0d1117;
  font: 900 16px/28px ui-rounded, 'SF Pro Rounded', 'Nunito', 'Segoe UI', system-ui, sans-serif;
  text-align: center;
  box-shadow: 0 2px 8px rgb(0 0 0 / 0.5);
}

.gd-name {
  position: absolute;
  left: 100%;
  top: 50%;
  margin-left: 12px;
  transform: translateY(-50%);
  white-space: nowrap;
  color: #fff;
  font: 800 15px ui-rounded, 'SF Pro Rounded', 'Nunito', 'Segoe UI', system-ui, sans-serif;
  text-shadow: 0 1px 3px #000, 0 0 8px #000;
}

.gd.off .gd-name {
  display: none;
}

@keyframes gd-ring {
  from {
    transform: scale(0.6);
    opacity: 0.95;
  }
  to {
    transform: scale(3);
    opacity: 0;
  }
}

@keyframes gd-blink {
  50% {
    background: #fff;
    transform: scale(1.25);
  }
}
</style>
