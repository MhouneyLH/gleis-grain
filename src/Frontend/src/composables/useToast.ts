import { ref } from 'vue'

export interface Toast {
  id: number
  kind: 'good' | 'bad' | 'info'
  title: string
  text?: string
}

const toasts = ref<Toast[]>([])
let nextId = 1

export function pushToast(kind: Toast['kind'], title: string, text?: string, ms = 6000): void {
  const toast: Toast = text === undefined ? { id: nextId++, kind, title } : { id: nextId++, kind, title, text }
  toasts.value = [...toasts.value, toast]
  setTimeout(() => dismissToast(toast.id), ms)
}

export function dismissToast(id: number): void {
  toasts.value = toasts.value.filter((toast) => toast.id !== id)
}

export function useToasts() {
  return { toasts, dismissToast }
}
