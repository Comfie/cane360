import './styles.scss';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';

const baseUrl = document.getElementsByTagName('base')[0]?.getAttribute('href') ?? '/';
const rootElement = document.getElementById('root');

if (!rootElement) {
  throw new Error('Cane360 could not find its application root.');
}

const root = createRoot(rootElement);

root.render(
  <BrowserRouter basename={baseUrl}>
    <App />
  </BrowserRouter>
);
