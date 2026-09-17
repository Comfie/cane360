import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { InputControlsClient, RedeemManagerInvitationRequest } from '../../web-api-client';
import { AuthLayout } from '../AuthLayout';
import { getApiError } from '../apiError';
import { ValidationError } from '../ValidationError';
import { useAuth } from './AuthContext';

const inputControls = new InputControlsClient();

export function ActivationPage() {
  const [token, setToken] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { session, refreshSession } = useAuth();
  const navigate = useNavigate();

  if (session.hasTenant) {
    return <Navigate to="/" replace />;
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError('');
    setIsSubmitting(true);

    try {
      await inputControls.redeem(new RedeemManagerInvitationRequest({ token }));
      await refreshSession();
      navigate('/', { replace: true });
    } catch (cause) {
      setError(getApiError(cause));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthLayout title="Activate your account" description="Paste the invitation token your Grower shared with you.">
      <form onSubmit={handleSubmit} noValidate>
        <ValidationError title="Unable to activate" message={error} />

        <label htmlFor="token">Invitation token</label>
        <input
          type="text"
          id="token"
          autoComplete="off"
          value={token}
          onChange={(event) => { setError(''); setToken(event.target.value); }}
          aria-invalid={Boolean(error) || undefined}
          required
        />

        <button type="submit" disabled={isSubmitting} aria-busy={isSubmitting}>
          {isSubmitting ? 'Activating…' : 'Activate'}
        </button>
      </form>
    </AuthLayout>
  );
}
