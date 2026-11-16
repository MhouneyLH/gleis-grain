import { afterEach, describe, expect, it, vi } from 'vitest'
import { api, ApiError } from './index'

function respond(status: number, body?: unknown, contentType = 'application/problem+json'): Response {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  })
}

afterEach(() => vi.unstubAllGlobals())

describe('api fetch wrapper', () => {
  it('turns problem details into an ApiError', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        respond(409, { title: 'Nickname taken', detail: 'Pick another.', code: 'game.nickname.taken', status: 409 }),
      ),
    )
    const error = await api.createPlayer('Max').catch((e: unknown) => e)
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 409, title: 'Nickname taken', detail: 'Pick another.', code: 'game.nickname.taken' })
  })

  it('survives an error body that is not JSON', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('boom', { status: 502, statusText: 'Bad Gateway' })))
    const error = await api.stops().catch((e: unknown) => e)
    expect(error).toMatchObject({ status: 502, title: 'Bad Gateway' })
  })

  it('reports a network failure as status 0', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('failed')))
    const error = await api.stops().catch((e: unknown) => e)
    expect(error).toMatchObject({ status: 0, title: 'Network error' })
  })

  it('returns undefined for 204', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 204 })))
    await expect(api.watchStop('Max', '60501720')).resolves.toBeUndefined()
  })

  it('sends JSON bodies and encodes path parts and query strings', async () => {
    const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(respond(200, [], 'application/json')))
    vi.stubGlobal('fetch', fetchMock)
    await api.watchStop('Max Muster', '1')
    await api.alerts({ line: '1', stop: 'Linz Hbf' })
    await api.alerts({})
    const [putUrl, putInit] = fetchMock.mock.calls[0] as [string, RequestInit]
    expect(putUrl).toBe('/api/game/players/Max%20Muster/stop')
    expect(putInit.method).toBe('PUT')
    expect(putInit.body).toBe('{"stopId":"1"}')
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/alerts?line=1&stop=Linz+Hbf')
    expect(fetchMock.mock.calls[2]?.[0]).toBe('/api/alerts')
  })
})
