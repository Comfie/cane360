import { deploymentEnv, routes, type VercelConfig } from '@vercel/config/v1';

const apiOrigin = deploymentEnv('API_ORIGIN');

export const config: VercelConfig = {
  framework: 'vite',
  buildCommand: 'npm run build:vercel',
  outputDirectory: 'build',
  rewrites: [
    routes.rewrite('/api/:path*', `${apiOrigin}/api/:path*`),
    routes.rewrite('/:path*', '/index.html'),
  ],
  headers: [
    routes.header('/api/:path*', [
      {
        key: 'x-vercel-enable-rewrite-caching',
        value: '0',
      },
    ]),
  ],
};
