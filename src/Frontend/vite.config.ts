import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vitest/config'

// Aspire gives the dev server the address of the host as GLEIS_GRAIN_HTTP (npm drops variable names with a hyphen).
const backend = process.env['GLEIS_GRAIN_HTTP'] ?? 'http://localhost:5080'

// The audience simulator has its own control API; the frontend reaches it under /sim.
const simulator = process.env['SIMULATOR_HTTP'] ?? 'http://localhost:5090'

export default defineConfig({
  base: '/',
  plugins: [vue()],
  build: {
    // The host serves these files; it is git-ignored.
    outDir: '../Host/wwwroot',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/sim': {
        target: simulator,
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/sim/, ''),
      },
      '/api': {
        target: backend,
        changeOrigin: true,
        // The SSE stream must pass through unbuffered and uncompressed.
        configure: (proxy) => {
          proxy.on('proxyReq', (proxyReq, req) => {
            if (req.url?.startsWith('/api/cluster/events')) {
              proxyReq.setHeader('accept-encoding', 'identity')
            }
          })
          proxy.on('proxyRes', (proxyRes, req, res) => {
            if (req.url?.startsWith('/api/cluster/events')) {
              proxyRes.headers['cache-control'] = 'no-cache, no-transform'
              proxyRes.headers['x-accel-buffering'] = 'no'
              res.setHeader('x-accel-buffering', 'no')
            }
          })
        },
      },
    },
  },
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
})
