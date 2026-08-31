import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AuthLayout } from '../AuthLayout';
import { ValidationError } from '../ValidationError';
import { useAuth } from './AuthContext';

const MIN_PASSWORD_LENGTH = 6;

function validateEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}

export function RegisterPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [emailTouched, setEmailTouched] = useState(false);
  const [passwordTouched, setPasswordTouched] = useState(false);
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { register } = useAuth();
  const navigate = useNavigate();

  const emailValid = validateEmail(email);
  const passwordValid = password.length >= MIN_PASSWORD_LENGTH;
  const emailInvalid = emailTouched && !emailValid;
  const passwordInvalid = passwordTouched && !passwordValid;

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');
    setEmailTouched(true);
    setPasswordTouched(true);

    if (!emailValid || !passwordValid) return;

    setIsSubmitting(true);
    try {
      await register(email, password);
      navigate('/login', { replace: true });
    } catch {
      setError('The account could not be created. The email may already be registered or the password may not meet the security rules.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthLayout title="Create an account" description="Set up secure access before adding farm records.">
      <form onSubmit={handleSubmit} noValidate>
        <ValidationError title="Unable to create the account" message={error} />

        <label htmlFor="email">Email address</label>
        <input
          type="email"
          id="email"
          autoComplete="username"
          inputMode="email"
          value={email}
          onChange={(event) => { setError(''); setEmail(event.target.value); }}
          onBlur={() => setEmailTouched(true)}
          aria-invalid={emailInvalid || undefined}
          aria-describedby="email-helper"
          required
        />
        <small id="email-helper" className={emailInvalid ? 'field-error' : ''}>
          {emailInvalid ? 'Enter a complete email address.' : 'This will be your Cane360 login.'}
        </small>

        <label htmlFor="password">Password</label>
        <input
          type="password"
          id="password"
          autoComplete="new-password"
          value={password}
          onChange={(event) => { setError(''); setPassword(event.target.value); }}
          onBlur={() => setPasswordTouched(true)}
          aria-invalid={passwordInvalid || undefined}
          aria-describedby="password-helper"
          required
        />
        <small id="password-helper" className={passwordInvalid ? 'field-error' : ''}>
          {passwordInvalid ? `Use at least ${MIN_PASSWORD_LENGTH} characters.` : 'Use a strong password that you do not use elsewhere.'}
        </small>

        <button type="submit" disabled={isSubmitting} aria-busy={isSubmitting}>
          {isSubmitting ? 'Creating account…' : 'Create account'}
        </button>
        <p className="auth-switch">Already registered? <Link to="/login">Log in</Link></p>
      </form>
    </AuthLayout>
  );
}
