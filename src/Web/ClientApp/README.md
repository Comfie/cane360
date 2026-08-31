# Cane360 React Client

This project uses [Vite](https://vitejs.dev/) with React 19 and TypeScript.

## Available Scripts

### `npm start`

Runs the app in development mode with hot module replacement.
Opens at [http://localhost:5173](http://localhost:5173).

The development server proxies API requests to the ASP.NET Core backend.

### `npm run build`

Builds the app for production to the `build` folder.
Optimizes the build for best performance.

### `npm run preview`

Previews the production build locally.

### `npm run lint`

Runs ESLint on the src directory.

### `npm run typecheck`

Checks JavaScript and JSX against the repository TypeScript configuration.

### `npm test`

Runs the container-independent navigation contract tests.

## Project Structure

- `src/` - React source code
- `src/main.tsx` - Application entry point
- `src/App.tsx` - Root component
- `src/components/` - React components
- `public/` - Static assets (favicon, manifest)
- `vite.config.ts` - Vite configuration with proxy settings
- `index.html` - HTML template

## Environment Variables

`VITE_API_URL` configures the development proxy in `vite.config.ts`. It is read
by Vite's Node process and must never contain database configuration.

Example:
```
VITE_API_URL=https://api.example.com
```

Set `VITE_API_URL` to override the default backend URL of
`https://localhost:7001`.

## Learn More

- [Vite Documentation](https://vitejs.dev/)
- [React Documentation](https://react.dev/)
