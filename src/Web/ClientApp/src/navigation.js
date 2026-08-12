/**
 * @typedef {'dashboard' | 'farm' | 'fields' | 'activities' | 'labour' | 'inventory' | 'finance' | 'reports' | 'administration'} NavigationId
 */

/**
 * @typedef {object} NavigationItem
 * @property {NavigationId} id
 * @property {string} path
 * @property {string} label
 * @property {string} shortLabel
 * @property {string} eyebrow
 * @property {string} description
 */

/** @type {readonly NavigationItem[]} */
export const protectedNavigation = Object.freeze([
  {
    id: 'dashboard',
    path: '/',
    label: 'Dashboard',
    shortLabel: 'Home',
    eyebrow: 'Farm overview',
    description: 'A clear view of records, exceptions, approvals, and work that needs attention.',
  },
  {
    id: 'farm',
    path: '/farm',
    label: 'Farm',
    shortLabel: 'Farm',
    eyebrow: 'Farm setup',
    description: 'Grower, farm, personnel, irrigation, and operating details will live here.',
  },
  {
    id: 'fields',
    path: '/fields',
    label: 'Fields and Crop Cycles',
    shortLabel: 'Fields',
    eyebrow: 'Crop records',
    description: 'Field boundaries, hectares, varieties, and current crop-cycle history will live here.',
  },
  {
    id: 'activities',
    path: '/activities',
    label: 'Activities',
    shortLabel: 'Activities',
    eyebrow: 'Field diary',
    description: 'Plan work and record what happened, when it happened, and who confirmed it.',
  },
  {
    id: 'labour',
    path: '/labour',
    label: 'Labour and Payroll',
    shortLabel: 'Labour',
    eyebrow: 'People and pay',
    description: 'Attendance, verified work, advances, payroll review, and payment evidence will live here.',
  },
  {
    id: 'inventory',
    path: '/inventory',
    label: 'Inventory',
    shortLabel: 'Inventory',
    eyebrow: 'Input control',
    description: 'Trace every controlled input from receipt and issue through field application or return.',
  },
  {
    id: 'finance',
    path: '/finance',
    label: 'Finance',
    shortLabel: 'Finance',
    eyebrow: 'Operational costs',
    description: 'Operational expenses, crop-cycle costs, budgets, and mill evidence will live here.',
  },
  {
    id: 'reports',
    path: '/reports',
    label: 'Reports',
    shortLabel: 'Reports',
    eyebrow: 'Evidence and insight',
    description: 'Trace totals back to farm, field, input, labour, payroll, and audit records.',
  },
  {
    id: 'administration',
    path: '/administration',
    label: 'Administration',
    shortLabel: 'Admin',
    eyebrow: 'Configuration',
    description: 'Users, roles, reference data, tolerances, and audit access will be managed here.',
  },
]);
