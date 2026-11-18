import L from 'leaflet'

/** Linz, used until the stops are known. */
export const LINZ_CENTER: L.LatLngTuple = [48.3069, 14.2858]

export const TILE_URL = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png'
export const ATTRIBUTION = '&copy; OpenStreetMap contributors'

/** Zoom level from which the stop names are shown. */
export const LABEL_ZOOM = 15

export interface BaseMap {
  map: L.Map
  destroy(): void
}

/**
 * A Leaflet map with the OpenStreetMap tiles. The only external request of the app.
 * When tiles do not load, the container background (see `--map-bg`) stays visible
 * and the stops are still drawn on top of it.
 */
export function createBaseMap(element: HTMLElement, options: { zoomControl?: boolean; zoomPosition?: L.ControlPosition } = {}): BaseMap {
  const map = L.map(element, {
    center: LINZ_CENTER,
    zoom: 14,
    zoomControl: false,
    attributionControl: true,
    zoomSnap: 0.5,
  })

  if (options.zoomControl ?? true) L.control.zoom({ position: options.zoomPosition ?? 'bottomright' }).addTo(map)

  L.tileLayer(TILE_URL, {
    attribution: ATTRIBUTION,
    maxZoom: 19,
    className: 'osm-tiles',
  }).addTo(map)

  const toggleLabels = () => element.classList.toggle('labels-off', map.getZoom() < LABEL_ZOOM)
  map.on('zoomend', toggleLabels)
  toggleLabels()

  // The container changes size with the layout (bottom sheet, side panel).
  const observer = new ResizeObserver(() => map.invalidateSize())
  observer.observe(element)

  return {
    map,
    destroy() {
      observer.disconnect()
      map.remove()
    },
  }
}

export function fitToStops(
  map: L.Map,
  points: L.LatLngTuple[],
  padding: L.PointTuple = [30, 30],
  options: L.FitBoundsOptions = {},
): void {
  if (points.length === 0) return
  map.fitBounds(L.latLngBounds(points), { padding, ...options })
}
