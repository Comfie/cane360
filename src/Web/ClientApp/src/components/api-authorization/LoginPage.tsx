import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { AuthLayout } from '../AuthLayout';
import { ValidationError } from '../ValidationError';
import { useAuth } from './AuthContext';

export function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');
    setIsSubmitting(true);

    try {
      await login(email, password);
      const requestedUrl = location.state && typeof location.state === 'object' && 'returnUrl' in location.state
        ? location.state.returnUrl
        : undefined;
      const returnUrl = typeof requestedUrl === 'string'
        && requestedUrl.startsWith('/')
        && !requestedUrl.startsWith('//')
        ? requestedUrl
        : '/';
      navigate(returnUrl, { replace: true });
    } catch {
      setError('The email or password did not match an account. Check both fields and try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthLayout title="Log in" description="Continue to your farm workspace.">
      <form onSubmit={handleSubmit} noValidate>
        <ValidationError title="Unable to log in" message={error} />

        <label htmlFor="email">Email address</label>
        <input
          type="email"
          id="email"
          autoComplete="username"
          inputMode="email"
          value={email}
          onChange={(event) => { setError(''); setEmail(event.target.value); }}
          aria-invalid={Boolean(error) || undefined}
          required
        />

        <label htmlFor="password">Password</label>
        <input
          type="password"
          id="password"
          autoComplete="current-password"
          value={password}
          onChange={(event) => { setError(''); setPassword(event.target.value); }}
          aria-invalid={Boolean(error) || undefined}
          required
        />

        <button type="submit" disabled={isSubmitting} aria-busy={isSubmitting}>
          {isSubmitting ? 'Logging in…' : 'Log in'}
        </button>
        <p className="auth-switch">New to Cane360? <Link to="/register">Create an account</Link></p>
      </form>
    </AuthLayout>
  );
}
