import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { backoffDelay, retryWithBackoff } from './retry'

describe('backoffDelay', () => {
  it('doubles from 1 s and stops at 10 s', () => {
    expect([0, 1, 2, 3, 4, 5, 20].map((n) => backoffDelay(n))).toEqual([1000, 2000, 4000, 8000, 10_000, 10_000, 10_000])
  })
})

describe('retryWithBackoff', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => vi.useRealTimers())

  it('returns at once when the first call works', async () => {
    const task = vi.fn().mockResolvedValue('ok')
    await expect(retryWithBackoff(task)).resolves.toBe('ok')
    expect(task).toHaveBeenCalledTimes(1)
  })

  it('retries with growing pauses until the call succeeds and reports every failure', async () => {
    const task = vi
      .fn()
      .mockRejectedValueOnce(new Error('down'))
      .mockRejectedValueOnce(new Error('down'))
      .mockResolvedValue('ok')
    const onError = vi.fn()
    const result = retryWithBackoff(task, onError)

    await vi.advanceTimersByTimeAsync(0)
    expect(task).toHaveBeenCalledTimes(1)
    await vi.advanceTimersByTimeAsync(999)
    expect(task).toHaveBeenCalledTimes(1)
    await vi.advanceTimersByTimeAsync(1)
    expect(task).toHaveBeenCalledTimes(2)
    await vi.advanceTimersByTimeAsync(2000)
    expect(task).toHaveBeenCalledTimes(3)

    await expect(result).resolves.toBe('ok')
    expect(onError).toHaveBeenCalledTimes(2)
  })
})
