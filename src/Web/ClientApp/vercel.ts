const apiOriginValue = process.env.API_ORIGIN;

if (!apiOriginValue) {
  throw new Error('API_ORIGIN must be configured in Vercel.');
}

const apiOriginUrl = new URL(apiOriginValue);

if (apiOriginUrl.protocol !== 'https:') {
  throw new Error('API_ORIGIN must use HTTPS.');
}

const apiOrigin = apiOriginUrl.origin;

export const config = {
  framework: 'vite',
  buildCommand: 'npm run build:vercel',
  outputDirectory: 'build',
  rewrites: [
    {
      source: '/api/:path*',
      destination: `${apiOrigin}/api/:path*`,
    },
    {
      source: '/:path*',
      destination: '/index.html',
    },
  ],
  headers: [
    {
      source: '/api/:path*',
      headers: [
        {
          key: 'x-vercel-enable-rewrite-caching',
          value: '0',
        },
      ],
    },
  ],
};
