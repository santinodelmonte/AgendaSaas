export type PublicPerfil = {
  tenantId: string;
  nombre: string;
  slug: string;
  whatsApp: string;
  duracionTurnoMinutos: number;
};

export type DisponibilidadTurno = {
  fechaHora: string;
  estado: 'Disponible' | 'Ocupado' | string;
};

export type SolicitarTurnoRequest = {
  fechaHora: string;
  nombreCliente: string;
  telefonoCliente: string;
  servicio: string;
};