import {
  BarChart3,
  ClipboardList,
  DollarSign,
  LayoutDashboard,
  Leaf,
  LogOut,
  MapPin,
  MoreHorizontal,
  Package,
  PanelLeftClose,
  PanelLeftOpen,
  PanelsTopLeft,
  Settings,
  Users,
  X,
} from 'lucide-react';
import { useState } from 'react';
import type { LucideIcon } from 'lucide-react';
import type { MouseEvent } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { protectedNavigation } from '../navigation.ts';
import type { NavigationId, NavigationItem } from '../navigation.ts';
import { useAuth } from './api-authorization/AuthContext';
import { ThemeToggle } from './ThemeToggle';

const icons: Record<NavigationId, LucideIcon> = {
  dashboard: LayoutDashboard,
  farm: MapPin,
  fields: PanelsTopLeft,
  activities: ClipboardList,
  labour: Users,
  inventory: Package,
  finance: DollarSign,
  reports: BarChart3,
  administration: Settings,
};

interface NavigationLinkProps {
  item: NavigationItem;
  compact?: boolean;
  iconOnly?: boolean;
  onNavigate?: () => void;
}

function NavigationLink({ item, compact = false, iconOnly = false, onNavigate }: NavigationLinkProps) {
  const Icon = icons[item.id];

  return (
    <NavLink
      to={item.path}
      end={item.path === '/'}
      className={({ isActive }) => `navigation-link${isActive ? ' is-active' : ''}${compact ? ' is-compact' : ''}`}
      onClick={onNavigate}
      aria-label={iconOnly ? item.label : undefined}
      title={iconOnly ? item.label : undefined}
    >
      <Icon size={compact ? 20 : 16} aria-hidden="true" />
      <span>{compact ? item.shortLabel : item.label}</span>
    </NavLink>
  );
}

function Brand() {
  return (
    <NavLink className="brand" to="/" aria-label="Cane360 dashboard">
      <span className="brand-mark" aria-hidden="true"><Leaf size={18} /></span>
      <span>
        <strong>Cane360</strong>
        <small>Grower operations</small>
      </span>
    </NavLink>
  );
}

interface DesktopNavigationProps {
  collapsed: boolean;
  onToggle: () => void;
}

export function DesktopNavigation({ collapsed, onToggle }: DesktopNavigationProps) {
  const { accountEmail, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <aside className={`desktop-sidebar${collapsed ? ' is-collapsed' : ''}`} aria-label="Primary navigation">
      <header className="sidebar-header">
        <Brand />
        <button
          className="quiet-icon-button sidebar-toggle"
          type="button"
          onClick={onToggle}
          aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          aria-expanded={!collapsed}
          title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          {collapsed
            ? <PanelLeftOpen size={17} aria-hidden="true" />
            : <PanelLeftClose size={17} aria-hidden="true" />}
        </button>
      </header>
      <div className="workspace-label">
        <span>Workspace</span>
        <strong>Farm setup pending</strong>
      </div>
      <nav className="navigation-list">
        {protectedNavigation.map((item) => (
          <NavigationLink key={item.id} item={item} iconOnly={collapsed} />
        ))}
      </nav>
      <div className="sidebar-footer">
        <div className="account-summary">
          <span className="account-avatar" aria-hidden="true">{accountEmail?.charAt(0).toUpperCase() || 'C'}</span>
          <span>
            <strong>{accountEmail || 'Cane360 user'}</strong>
            <small>Authenticated</small>
          </span>
        </div>
        <div className="sidebar-actions">
          <ThemeToggle />
          <button className="quiet-icon-button" type="button" onClick={handleLogout} aria-label="Log out">
            <LogOut size={17} aria-hidden="true" />
          </button>
        </div>
      </div>
    </aside>
  );
}

export function MobileHeader() {
  const { logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <header className="mobile-header">
      <Brand />
      <div className="mobile-header-actions">
        <ThemeToggle />
        <button className="quiet-icon-button" type="button" onClick={handleLogout} aria-label="Log out">
          <LogOut size={19} aria-hidden="true" />
        </button>
      </div>
    </header>
  );
}

export function MobileNavigation() {
  const [isOpen, setIsOpen] = useState(false);
  const location = useLocation();
  const primaryItems = protectedNavigation.slice(0, 3);
  const secondaryItems = protectedNavigation.slice(3);
  const moreIsActive = secondaryItems.some((item) => item.path === location.pathname);

  return (
    <>
      {isOpen && (
        <div className="mobile-menu-backdrop" role="presentation" onClick={() => setIsOpen(false)}>
          <section
            id="mobile-menu"
            className="mobile-menu-sheet"
            role="dialog"
            aria-modal="true"
            aria-labelledby="mobile-menu-title"
            onClick={(event: MouseEvent<HTMLElement>) => event.stopPropagation()}
          >
            <header>
              <div>
                <span className="eyebrow">Cane360 modules</span>
                <h2 id="mobile-menu-title">More</h2>
              </div>
              <button className="quiet-icon-button" type="button" onClick={() => setIsOpen(false)} aria-label="Close menu">
                <X size={21} aria-hidden="true" />
              </button>
            </header>
            <nav className="mobile-menu-list">
              {secondaryItems.map((item) => (
                <NavigationLink key={item.id} item={item} onNavigate={() => setIsOpen(false)} />
              ))}
            </nav>
          </section>
        </div>
      )}

      <nav className="mobile-bottom-navigation" aria-label="Primary navigation">
        {primaryItems.map((item) => <NavigationLink key={item.id} item={item} compact />)}
        <button
          type="button"
          className={`navigation-link is-compact${moreIsActive || isOpen ? ' is-active' : ''}`}
          onClick={() => setIsOpen(true)}
          aria-expanded={isOpen}
          aria-controls="mobile-menu"
        >
          <MoreHorizontal size={20} aria-hidden="true" />
          <span>More</span>
        </button>
      </nav>
    </>
  );
}
