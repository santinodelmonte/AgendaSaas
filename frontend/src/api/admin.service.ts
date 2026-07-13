import api from './axios';
import type {
  AgendaItem, AgendaSlotsResponse, DashboardStats,
  DiaBloqueado, HistorialItem, Horario, HorarioBody, Metricas, PerfilAdmin,
  SemanaDia, Servicio, ServicioBody, TurnoAgenda, TurnoPendiente,
} from '../types/admin';

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

export async function createAdminHorario(body: HorarioBody) {
  const { data } = await api.post<Horario>('/api/admin/horarios', body);
  return data;
}

export async function updateAdminHorario(id: string, body: HorarioBody) {
  const { data } = await api.put<Horario>(`/api/admin/horarios/${id}`, body);
  return data;
}

export async function deleteAdminHorario(id: string) {
  await api.delete(`/api/admin/horarios/${id}`);
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
  const { data } = await api.get<TurnoAgenda[]>('/api/admin/agenda', { params: { fecha } });
  return data;
}

export async function getAgendaSemanal(fechaInicio: string) {
  const { data } = await api.get<SemanaDia[]>('/api/admin/agenda/semana', { params: { fechaInicio } });
  return data;
}

export async function getAgendaSlots(fecha: string) {
  const { data } = await api.get<AgendaSlotsResponse>('/api/admin/agenda/slots', { params: { fecha } });
  return data;
}

export async function getHistorial(pagina = 1, tamano = 20) {
  const { data } = await api.get<HistorialItem[]>('/api/admin/agenda/historial', { params: { pagina, tamano } });
  return data;
}

export async function getMetricas() {
  const { data } = await api.get<Metricas>('/api/admin/metricas');
  return data;
}

export async function getDiasBloqueados() {
  const { data } = await api.get<DiaBloqueado[]>('/api/admin/dias-bloqueados');
  return data;
}

export async function bloquearDia(fecha: string, motivo?: string) {
  const { data } = await api.post<DiaBloqueado>('/api/admin/dias-bloqueados', { fecha, motivo });
  return data;
}

export async function desbloquearDia(id: string) {
  await api.delete(`/api/admin/dias-bloqueados/${id}`);
}

export async function cancelarTurno(id: string) {
  const { data } = await api.post(`/api/admin/turnos/cancelar/${id}`);
  return data;
}

export async function getAdminServicios() {
  const { data } = await api.get<Servicio[]>('/api/admin/servicios');
  return data;
}

export async function createAdminServicio(body: ServicioBody) {
  const { data } = await api.post<Servicio>('/api/admin/servicios', body);
  return data;
}

export async function updateAdminServicio(id: string, body: ServicioBody & { activo: boolean }) {
  const { data } = await api.put<Servicio>(`/api/admin/servicios/${id}`, body);
  return data;
}

export async function deleteAdminServicio(id: string) {
  await api.delete(`/api/admin/servicios/${id}`);
}

export async function crearTurnoManual(body: {
  fechaHora: string;
  nombreCliente?: string;
  telefonoCliente?: string;
  servicio?: string;
  nota?: string;
  duracionMinutos?: number | null;
}) {
  const { data } = await api.post('/api/admin/turnos/crear-manual', body);
  return data;
}

export async function reprogramarTurno(id: string, body: {
  fechaHora: string;
  duracionMinutos?: number | null;
}) {
  const { data } = await api.put(`/api/admin/turnos/reprogramar/${id}`, body);
  return data;
}

export async function intercambiarTurnos(body: {
  turnoAId: string;
  turnoBId: string;
  intercambiarDuraciones: boolean;
}) {
  const { data } = await api.post('/api/admin/turnos/intercambiar', body);
  return data;
}
