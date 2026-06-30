export type DashboardStats = {
  turnosPendientes: number;
  confirmadosHoy: number;
  confirmadosMes: number;
  proximoTurno?: {
    fechaHora: string;
    nombreCliente: string;
    servicio: string;
  } | null;
};

export type PerfilAdmin = {
  nombre: string;
  email: string;
  whatsApp: string;
  duracionTurnoMinutos: number;
};

export type Horario = {
  id: string;
  diaSemana: string;
  horaInicio: string;
  horaFin: string;
  activo: boolean;
};

export type TurnoPendiente = {
  id: string;
  nombreCliente: string;
  telefonoCliente: string;
  servicio: string;
  fechaHora: string;
};

export type AgendaItem = {
  id: string;
  fechaHora: string;
  nombreCliente: string;
  servicio: string;
  estado: 'Pendiente' | 'Confirmado' | 'Rechazado' | string;
};

export type AgendaSlot = {
  fechaHora: string;
  estado: 'Disponible' | 'Pendiente' | 'Confirmado' | 'Rechazado';
  id?: string | null;
  nombreCliente?: string | null;
  telefonoCliente?: string | null;
  servicioSolicitado?: string | null;
  notaInterna?: string | null;
};
