import { protectedNavigation } from '../../navigation.ts';

export function postLoginDestination(session: { hasTenant: boolean }, requestedUrl?: string): string {
  if (!session.hasTenant) return '/activate';
  if (typeof requestedUrl === 'string' && requestedUrl.startsWith('/') && !requestedUrl.startsWith('//')) {
    return requestedUrl;
  }
  return '/';
}

const fullNavigation: readonly string[] = protectedNavigation.map((item) => item.id);

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
