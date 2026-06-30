import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// In local dev, proxy /api to the gateway so the browser always calls
// same-origin /api/... (no CORS). In Docker, nginx does the same job.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
})
