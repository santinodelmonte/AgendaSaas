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
  slug: string;
  whatsApp: string;
  duracionTurnoMinutos: number;
};

export type Horario = {
  id: string;
  diaSemana: number; // DayOfWeek: 0=Dom, 1=Lun, 2=Mar, 3=Mié, 4=Jue, 5=Vie, 6=Sáb
  horaInicio: string; // "HH:mm:ss"
  horaFin: string;
  pausaInicio: string | null;
  pausaFin: string | null;
};

export type HorarioBody = {
  diaSemana: number;
  horaInicio: string;
  horaFin: string;
  pausaInicio?: string | null;
  pausaFin?: string | null;
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
  duracionMinutos?: number | null;
};

// Turno tal como lo devuelve GET /api/admin/agenda (a diferencia de AgendaItem,
// expone servicioSolicitado/telefonoCliente/notaInterna y la duración propia).
export type TurnoAgenda = {
  id: string;
  fechaHora: string;
  estado: 'Disponible' | 'Pendiente' | 'Confirmado' | 'Rechazado' | string;
  nombreCliente: string | null;
  telefonoCliente: string | null;
  servicioSolicitado: string | null;
  notaInterna: string | null;
  duracionMinutos: number | null;
};

export type AgendaSlotsResponse = {
  bloqueado: boolean;
  motivo: string | null;
  slots: AgendaSlot[];
};

export type SemanaDia = {
  fecha: string;
  turnos: AgendaItem[];
};

export type HistorialItem = {
  id: string;
  fechaHora: string;
  estado: string;
  nombreCliente: string | null;
  telefonoCliente: string | null;
  servicioSolicitado: string | null;
  notaInterna: string | null;
};

export type MetricasMes = {
  mes: string;
  total: number;
  confirmados: number;
};

export type MetricasHora = {
  hora: number;
  total: number;
};

export type Metricas = {
  porMes: MetricasMes[];
  tasaConfirmacion: number;
  totalPasados: number;
  confirmados: number;
  rechazados: number;
  horasMasPedidas: MetricasHora[];
};

export type DiaBloqueado = {
  id: string;
  fecha: string;
  motivo: string | null;
};

export type Servicio = {
  id: string;
  nombre: string;
  precio: number;
  activo: boolean;
};

export type ServicioBody = {
  nombre: string;
  precio: number;
  activo?: boolean;
};
