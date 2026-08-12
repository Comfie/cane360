import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const target = process.env.VITE_API_URL || 'https://localhost:7001';
const proxyOptions = { target, secure: false, changeOrigin: true };

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  css: {
    preprocessorOptions: {
      scss: {
        quietDeps: true,
      },
    },
  },
  server: {
    port: Number(process.env.PORT) || 5173,
    proxy: {
      '/api': proxyOptions,
      '/openapi': proxyOptions,
      '/scalar': proxyOptions,
    },
  },
  build: {
    outDir: 'build',
  },
});
