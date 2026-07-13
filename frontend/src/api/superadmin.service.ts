import api from './axios';
import type {
  ActualizarManicuristaBody,
  CrearManicuristaBody,
  ManicuristaAdmin,
} from '../types/superadmin';

const BASE = '/api/superadmin/manicuristas';

export async function getManicuristas() {
  const { data } = await api.get<ManicuristaAdmin[]>(BASE);
  return data;
}

export async function crearManicurista(body: CrearManicuristaBody) {
  const { data } = await api.post(BASE, body);
  return data;
}

export async function actualizarManicurista(tenantId: string, body: ActualizarManicuristaBody) {
  const { data } = await api.put(`${BASE}/${tenantId}`, body);
  return data;
}

export async function setManicuristaActivo(tenantId: string, activo: boolean) {
  const { data } = await api.put(`${BASE}/${tenantId}/activo`, { activo });
  return data;
}

export async function resetPasswordManicurista(tenantId: string, password: string) {
  const { data } = await api.post(`${BASE}/${tenantId}/reset-password`, { password });
  return data;
}

export async function eliminarManicurista(tenantId: string) {
  await api.delete(`${BASE}/${tenantId}`);
}
