import { Navigate, useLocation } from 'react-router-dom';
import { LoadingState } from '../LoadingState';
import { useAuth } from './AuthContext';

/** @param {{ children: import('react').ReactNode }} props */
export function ProtectedRoute({ children }) {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) return <LoadingState fullPage label="Opening your secure workspace" />;
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ returnUrl: location.pathname }} replace />;
  }

  return children;
}
