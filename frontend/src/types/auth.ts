export type LoginRequest = {
  email: string;
  password: string;
};

export type UsuarioRol = 'Manicurista' | 'SuperAdmin';

export type LoginResponse = {
  token: string;
  email: string;
  tenantId: string;
  rol: UsuarioRol;
};
