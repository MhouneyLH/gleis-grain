/** An error answer of the API (RFC 9457 problem details) or a failed request (status 0). */
export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly detail: string | undefined
  readonly code: string | undefined

  constructor(status: number, title: string, detail?: string, code?: string) {
    super(detail ? `${title}: ${detail}` : title)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.detail = detail
    this.code = code
  }

  /** The text for a person: the detail if there is one, otherwise the title. */
  get friendly(): string {
    return this.detail || this.title
  }
}

interface ProblemBody {
  title?: unknown
  detail?: unknown
  code?: unknown
  status?: unknown
}

const asText = (value: unknown): string | undefined =>
  typeof value === 'string' && value.length > 0 ? value : undefined

/** Reads the problem details of a failed response. Never throws. */
async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemBody = {}
  try {
    const text = await response.text()
    if (text) problem = JSON.parse(text) as ProblemBody
  } catch {
    // The body is not JSON; the status line has to do.
  }
  return new ApiError(
    response.status,
    asText(problem.title) ?? (response.statusText || `HTTP ${response.status}`),
    asText(problem.detail),
    asText(problem.code),
  )
}

export interface RequestOptions {
  body?: unknown
  signal?: AbortSignal
}

/** One small fetch wrapper: JSON in, JSON out, problem details become an ApiError. */
export async function request<T>(method: string, path: string, options: RequestOptions = {}): Promise<T> {
  const init: RequestInit = { method, headers: { Accept: 'application/json' } }
  if (options.signal) init.signal = options.signal
  if (options.body !== undefined) {
    init.headers = { ...init.headers, 'Content-Type': 'application/json' }
    init.body = JSON.stringify(options.body)
  }

  let response: Response
  try {
    response = await fetch(path, init)
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ApiError(0, 'Network error', 'The server cannot be reached.')
  }

  if (!response.ok) throw await toApiError(response)
  if (response.status === 204) return undefined as T
  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}
