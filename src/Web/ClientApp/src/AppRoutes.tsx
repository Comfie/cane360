import { Navigate, Route, Routes } from 'react-router-dom';
import { lazy } from 'react';
import { protectedNavigation } from './navigation';
import { Dashboard } from './components/pages/Dashboard';
import { FarmPage } from './components/pages/FarmPage';
import { FieldsPage } from './components/pages/FieldsPage';
import { CropCycleOverviewPage } from './components/pages/CropCycleOverviewPage';
import { ModulePage } from './components/pages/ModulePage';
import { ActivitiesPage } from './components/pages/ActivitiesPage';
import { LabourPage } from './components/pages/LabourPage';
import { InventoryPage } from './components/pages/InventoryPage';
import { Layout } from './components/Layout';
import { ActivationPage } from './components/api-authorization/ActivationPage';
import { LoginPage } from './components/api-authorization/LoginPage';
import { ProtectedRoute } from './components/api-authorization/ProtectedRoute';
import { RegisterPage } from './components/api-authorization/RegisterPage';

const PayrollPage = lazy(() => import('./components/pages/PayrollPage').then((module) => ({ default: module.PayrollPage })));
const FinancePage = lazy(() => import('./components/pages/FinancePage').then((module) => ({ default: module.FinancePage })));
const AdministrationPage = lazy(() => import('./components/pages/AdministrationPage').then((module) => ({ default: module.AdministrationPage })));

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/activate" element={<ProtectedRoute><ActivationPage /></ProtectedRoute>} />

      <Route element={<ProtectedRoute><Layout /></ProtectedRoute>}>
        <Route index element={<Dashboard />} />
        <Route path="/farm" element={<FarmPage />} />
        <Route path="/fields" element={<FieldsPage />} />
        <Route path="/fields/:fieldId/crop-cycles/:cropCycleId" element={<CropCycleOverviewPage />} />
        <Route path="/activities" element={<ActivitiesPage />} />
        <Route path="/labour" element={<LabourPage />} />
        <Route path="/payroll" element={<PayrollPage />} />
        <Route path="/inventory" element={<InventoryPage />} />
        <Route path="/administration" element={<AdministrationPage />} />
        <Route path="/finance" element={<FinancePage />} />
        {protectedNavigation.filter((item) => item.id === 'reports').map((item) => (
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
