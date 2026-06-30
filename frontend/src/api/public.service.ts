import api from './axios';
import type { DisponibilidadTurno, PublicPerfil, SolicitarTurnoRequest } from '../types/public';

export async function getPublicPerfil(slug: string) {
  const { data } = await api.get<PublicPerfil>(`/api/public/perfil/${slug}`);
  return data;
}

export async function getTurnosDisponibles(fecha: string, tenantId?: string) {
  const { data } = await api.get<DisponibilidadTurno[]>('/api/turnos/disponibles', {
    params: { fecha },
    headers: tenantId ? { 'X-Tenant-Id': tenantId } : undefined,
  });
  return data;
}

export async function solicitarTurno(body: SolicitarTurnoRequest, tenantId?: string) {
  const { data } = await api.post('/api/turnos/solicitar', body, {
    headers: tenantId ? { 'X-Tenant-Id': tenantId } : undefined,
  });
  return data;
}
