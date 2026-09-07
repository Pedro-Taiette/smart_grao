import { fileURLToPath, URL } from 'node:url';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react()],
  resolve: {
    // Import absoluto a partir de src. Evita o `../../../` que aparece assim que a estrutura
    // ganha profundidade e que quebra silenciosamente quando um arquivo muda de pasta.
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    // A porta esta fixada porque e a origem liberada no CORS do backend.
    port: 5173,
    strictPort: true,
  },
});
