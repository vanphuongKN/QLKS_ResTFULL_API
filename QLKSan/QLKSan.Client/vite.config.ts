import { defineConfig, type Plugin } from 'vite';
import react from '@vitejs/plugin-react';
import { cpSync, existsSync, createReadStream } from 'node:fs';
import { join, resolve, normalize } from 'node:path';

// Gateway mà SPA gọi khi phát triển (/api, /uploads đi qua proxy => cùng nguồn, không cần CORS)
const gateway = process.env.VITE_GATEWAY ?? 'https://localhost:7000';

// Ảnh mẫu của khách sạn dùng chung với QLKSan.Web (/images/...): phục vụ thẳng khi dev, chép vào bản build,
// không nhân bản ảnh trong mã nguồn
const anhDungChung = resolve(__dirname, '../QLKSan.Web/wwwroot/images');
function sharedImages(): Plugin {
  let outDir = 'dist';
  let laBuild = false;
  return {
    name: 'qlksan-shared-images',
    configResolved(config) {
      outDir = resolve(config.root, config.build.outDir);
      // Vitest cũng gọi closeBundle (với outDir giả): chỉ chép ảnh khi thật sự vite build
      laBuild = config.command === 'build' && !process.env.VITEST;
    },
    configureServer(server) {
      server.middlewares.use('/images', (req, res, next) => {
        const file = normalize(join(anhDungChung, decodeURIComponent((req.url ?? '').split('?')[0])));
        if (!file.startsWith(anhDungChung) || !existsSync(file)) return next();
        createReadStream(file).pipe(res);
      });
    },
    closeBundle() {
      if (laBuild && existsSync(anhDungChung)) cpSync(anhDungChung, join(outDir, 'images'), { recursive: true });
    }
  };
}

export default defineConfig({
  plugins: [react(), sharedImages()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: gateway, changeOrigin: true, secure: false },
      '/uploads': { target: gateway, changeOrigin: true, secure: false }
    }
  }
});
