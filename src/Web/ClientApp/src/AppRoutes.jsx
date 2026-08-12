import { Navigate, Route, Routes } from 'react-router-dom';
import { protectedNavigation } from './navigation';
import { Dashboard } from './components/pages/Dashboard';
import { FarmPage } from './components/pages/FarmPage';
import { FieldsPage } from './components/pages/FieldsPage';
import { ModulePage } from './components/pages/ModulePage';
import { Layout } from './components/Layout';
import { LoginPage } from './components/api-authorization/LoginPage';
import { ProtectedRoute } from './components/api-authorization/ProtectedRoute';
import { RegisterPage } from './components/api-authorization/RegisterPage';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<ProtectedRoute><Layout /></ProtectedRoute>}>
        <Route index element={<Dashboard />} />
        <Route path="/farm" element={<FarmPage />} />
        <Route path="/fields" element={<FieldsPage />} />
        {protectedNavigation.slice(3).map((item) => (
          <Route
            key={item.id}
            path={item.path}
            element={<ModulePage item={item} />}
          />
        ))}
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
