import { onBeforeUnmount, onMounted, ref } from 'vue'
import { EVENTS_URL, liveEventTypes, type LiveEventMap, type LiveEventType } from '../api'

export type LiveHandlers = { [K in LiveEventType]?: (payload: LiveEventMap[K]) => void }

const REOPEN_DELAY_MS = 2000

/**
 * One EventSource on the live stream with a named listener per event type.
 * EventSource reconnects by itself. After every reconnect (an `open` that follows
 * an `error`) `onResync` runs, so the page can reload what it missed in between.
 * When the browser gives up (for example after a 502 from a dying replica) the
 * connection is opened again after a short pause.
 */
export function useEventStream(handlers: LiveHandlers, onResync?: () => void) {
  const connected = ref(false)
  let source: EventSource | null = null
  let hadError = false
  let reopenTimer: ReturnType<typeof setTimeout> | undefined
  let stopped = false

  function open(): void {
    const current = new EventSource(EVENTS_URL)
    source = current

    current.addEventListener('open', () => {
      connected.value = true
      if (hadError) {
        hadError = false
        onResync?.()
      }
    })

    current.addEventListener('error', () => {
      connected.value = false
      hadError = true
      if (current.readyState === EventSource.CLOSED && !stopped) {
        current.close()
        reopenTimer = setTimeout(open, REOPEN_DELAY_MS)
      }
    })

    for (const type of liveEventTypes) {
      current.addEventListener(type, (event) => {
        let payload: unknown
        try {
          payload = JSON.parse((event as MessageEvent<string>).data)
        } catch {
          return
        }
        const handler = handlers[type] as ((p: unknown) => void) | undefined
        handler?.(payload)
      })
    }
  }

  onMounted(open)
  onBeforeUnmount(() => {
    stopped = true
    clearTimeout(reopenTimer)
    source?.close()
  })

  return { connected }
}
