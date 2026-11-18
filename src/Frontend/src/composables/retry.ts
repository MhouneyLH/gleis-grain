/** Wait before retry number `attempt` (0 based): 1 s, 2 s, 4 s ... capped at 10 s. */
export function backoffDelay(attempt: number, baseMs = 1000, maxMs = 10_000): number {
  return Math.min(baseMs * 2 ** attempt, maxMs)
}

/**
 * Runs `task` until it succeeds, waiting a little longer after every failure.
 * `onError` is called after each failed attempt (the page can show "retrying").
 * Meant for loads the page cannot work without, for example the stop catalogue
 * while the backend is still starting.
 */
export async function retryWithBackoff<T>(task: () => Promise<T>, onError: (error: unknown) => void = () => {}): Promise<T> {
  for (let attempt = 0; ; attempt += 1) {
    try {
      return await task()
    } catch (error) {
      onError(error)
      await new Promise((resolve) => setTimeout(resolve, backoffDelay(attempt)))
    }
  }
}
