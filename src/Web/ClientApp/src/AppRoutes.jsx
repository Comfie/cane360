import { Navigate, Route, Routes } from 'react-router-dom';
import { protectedNavigation } from './navigation';
import { Dashboard } from './components/pages/Dashboard';
import { FarmPage } from './components/pages/FarmPage';
import { FieldsPage } from './components/pages/FieldsPage';
import { CropCycleOverviewPage } from './components/pages/CropCycleOverviewPage';
import { ModulePage } from './components/pages/ModulePage';
import { ActivitiesPage } from './components/pages/ActivitiesPage';
import { LabourPage } from './components/pages/LabourPage';
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
        <Route path="/fields/:fieldId/crop-cycles/:cropCycleId" element={<CropCycleOverviewPage />} />
        <Route path="/activities" element={<ActivitiesPage />} />
        <Route path="/labour" element={<LabourPage />} />
        {protectedNavigation.slice(5).map((item) => (
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
