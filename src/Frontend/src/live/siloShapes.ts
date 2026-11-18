import type { SiloShape } from './siloSlots'

/** SVG (24x24 view box, white ring) shared by the map markers, the legend and the silo list. */
export function shapeSvg(shape: SiloShape, fill: string, size: number, extraClass = ''): string {
  const style = `fill="${fill}" stroke="#fff" stroke-width="2.2" stroke-linejoin="round"`
  const body =
    shape === 'square'
      ? `<rect x="3.5" y="3.5" width="17" height="17" rx="1.5" ${style}/>`
      : shape === 'circle'
        ? `<circle cx="12" cy="12" r="9" ${style}/>`
        : `<polygon points="12,2.8 22,20.5 2,20.5" ${style}/>`
  return `<svg class="${extraClass}" viewBox="0 0 24 24" width="${size}" height="${size}" aria-hidden="true">${body}</svg>`
}
