import { onBeforeUnmount, onMounted, toValue, watch, type MaybeRefOrGetter } from 'vue'

/**
 * Runs `task` right away and then every `intervalMs`, while `enabled` is true.
 * Errors are the task's business; they never stop the timer.
 */
export function usePolling(task: () => Promise<unknown> | unknown, intervalMs: number, enabled: MaybeRefOrGetter<boolean> = true) {
  let timer: ReturnType<typeof setInterval> | undefined
  let mounted = false

  async function run(): Promise<void> {
    try {
      await task()
    } catch {
      // The task reports its own errors.
    }
  }

  function stop(): void {
    clearInterval(timer)
    timer = undefined
  }

  function sync(): void {
    stop()
    if (mounted && toValue(enabled)) {
      void run()
      timer = setInterval(() => void run(), intervalMs)
    }
  }

  onMounted(() => {
    mounted = true
    sync()
  })
  watch(() => toValue(enabled), sync)
  onBeforeUnmount(() => {
    mounted = false
    stop()
  })

  return { refresh: run }
}
