export type ManicuristaAdmin = {
  tenantId: string;
  nombre: string;
  slug: string;
  email: string;
  whatsApp: string;
  activo: boolean;
  duracionTurnoMinutos: number;
  loginEmail: string | null;
  usuarioActivo: boolean;
  cantidadTurnos: number;
};

export type CrearManicuristaBody = {
  nombre: string;
  email: string;
  whatsApp: string;
  duracionTurnoMinutos: number;
  slug?: string;
  password: string;
};

export type ActualizarManicuristaBody = {
  nombre: string;
  whatsApp: string;
  duracionTurnoMinutos: number;
  slug?: string;
};
