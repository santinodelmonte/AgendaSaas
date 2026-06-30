import api from './axios';
import type { AgendaItem, DashboardStats, Horario, PerfilAdmin, TurnoPendiente } from '../types/admin';

export async function getAdminDashboard() {
  const { data } = await api.get<DashboardStats>('/api/admin/dashboard');
  return data;
}

export async function getAdminPerfil() {
  const { data } = await api.get<PerfilAdmin>('/api/admin/perfil');
  return data;
}

export async function updateAdminPerfil(body: PerfilAdmin) {
  const { data } = await api.put<PerfilAdmin>('/api/admin/perfil', body);
  return data;
}

export async function getAdminHorarios() {
  const { data } = await api.get<Horario[]>('/api/admin/horarios');
  return data;
}

export async function createAdminHorario(body: Omit<Horario, 'id'>) {
  const { data } = await api.post<Horario>('/api/admin/horarios', body);
  return data;
}

export async function updateAdminHorario(id: string, body: Partial<Horario>) {
  const { data } = await api.put<Horario>(`/api/admin/horarios/${id}`, body);
  return data;
}

export async function deleteAdminHorario(id: string) {
  const { data } = await api.delete(`/api/admin/horarios/${id}`);
  return data;
}

export async function getTurnosPendientes() {
  const { data } = await api.get<TurnoPendiente[]>('/api/admin/turnos/pendientes');
  return data;
}

export async function confirmarTurno(id: string) {
  const { data } = await api.post(`/api/admin/turnos/confirmar/${id}`);
  return data;
}

export async function rechazarTurno(id: string) {
  const { data } = await api.post(`/api/admin/turnos/rechazar/${id}`);
  return data;
}

export async function getAgendaDiaria(fecha: string) {
  const { data } = await api.get<AgendaItem[]>('/api/admin/agenda', { params: { fecha } });
  return data;
}

export async function getAgendaSemanal(fechaInicio: string) {
  const { data } = await api.get<AgendaItem[]>('/api/admin/agenda/semana', { params: { fechaInicio } });
  return data;
}
