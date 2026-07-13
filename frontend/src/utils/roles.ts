import type { UsuarioRol } from '../types/auth';

// Página de inicio según el rol: el superadmin no tiene tenant, así que su home
// es la gestión de manicuristas, no el panel de agenda.
export function homeForRole(rol: UsuarioRol | null): string {
  return rol === 'SuperAdmin' ? '/superadmin' : '/admin';
}
