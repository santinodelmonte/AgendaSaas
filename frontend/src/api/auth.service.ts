import api from './axios';
import type { LoginRequest, LoginResponse } from '../types/auth';

export async function login(request: LoginRequest) {
  const { data } = await api.post<LoginResponse>('/api/auth/login', request);
  return data;
}
