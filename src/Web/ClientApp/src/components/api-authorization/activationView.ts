export function postLoginDestination(session: { hasTenant: boolean }, requestedUrl?: string): string {
  if (!session.hasTenant) return '/activate';
  if (typeof requestedUrl === 'string' && requestedUrl.startsWith('/') && !requestedUrl.startsWith('//')) {
    return requestedUrl;
  }
  return '/';
}

const fullNavigation = ['dashboard', 'farm', 'fields', 'activities', 'labour', 'inventory', 'finance', 'reports', 'administration'] as const;

export function roleNavigationIds(role: string | null): readonly string[] {
  if (role === 'Grower' || role === 'FarmManager') return fullNavigation;
  if (role === 'Supervisor') return ['dashboard', 'fields', 'activities'];
  return ['dashboard'];
}

export function invitationRoleOptions(): readonly { value: string; label: string }[] {
  return [
    { value: 'FarmManager', label: 'Farm manager' },
    { value: 'Supervisor', label: 'Supervisor' },
  ];
}
