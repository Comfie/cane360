import { useEffect, useState } from 'react';
import { Outlet } from 'react-router-dom';
import { DesktopNavigation, MobileHeader, MobileNavigation } from './Navigation';

const SIDEBAR_STORAGE_KEY = 'cane360SidebarCollapsed';

export function Layout() {
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(
    () => localStorage.getItem(SIDEBAR_STORAGE_KEY) === 'true'
  );

  useEffect(() => {
    localStorage.setItem(SIDEBAR_STORAGE_KEY, String(isSidebarCollapsed));
  }, [isSidebarCollapsed]);

  return (
    <div className={`app-shell${isSidebarCollapsed ? ' is-sidebar-collapsed' : ''}`}>
      <a className="skip-link" href="#main-content">Skip to main content</a>
      <DesktopNavigation
        collapsed={isSidebarCollapsed}
        onToggle={() => setIsSidebarCollapsed((collapsed) => !collapsed)}
      />
      <div className="app-frame">
        <MobileHeader />
        <main id="main-content" className="page-content">
          <Outlet />
        </main>
        <MobileNavigation />
      </div>
    </div>
  );
}
