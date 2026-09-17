import { Navigate, useLocation } from 'react-router-dom';
import { LoadingState } from '../LoadingState';
import { useAuth } from './AuthContext';
import type { ReactNode } from 'react';

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading, session } = useAuth();
  const location = useLocation();

  if (isLoading) return <LoadingState fullPage label="Opening your secure workspace" />;
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ returnUrl: location.pathname }} replace />;
  }
  if (!session.hasTenant && location.pathname !== '/activate') {
    return <Navigate to="/activate" replace />;
  }

  return children;
}
