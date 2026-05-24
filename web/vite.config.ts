import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vite';
import solidPlugin from 'vite-plugin-solid';
import { viteSingleFile } from "vite-plugin-singlefile";
import devtools from 'solid-devtools/vite';

export default defineConfig({
  plugins: [
    devtools(),
    solidPlugin(),
    tailwindcss(),
    viteSingleFile({
      useRecommendedBuildConfig: false
    })
  ],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:2365',
        changeOrigin: true,
      },
    },
  },
  build: {
    target: 'esnext',
  },
});
