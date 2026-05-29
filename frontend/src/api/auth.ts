import type { AuthResponse, LoginRequest } from '../types';
import apiClient from './client';

export interface RegisterTenantRequest {
  name: string;
  ownerEmail: string;
  companyName?: string;
  currency?: string;
  countryCode?: string;
}

export interface RegisterTenantResponse {
  id: number;
  slug: string;
  name: string;
  ownerEmail: string;
  plan: string;
  status: string;
}

export interface RegisterUserRequest {
  tenantSlug: string;
  email: string;
  fullName: string;
  password: string;
  role?: string;
}

export const authApi = {
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>('/auth/login', data);
    return response.data;
  },

  registerTenant: async (
    data: RegisterTenantRequest
  ): Promise<RegisterTenantResponse> => {
    const response = await apiClient.post<RegisterTenantResponse>(
      '/tenant/register',
      data
    );
    return response.data;
  },

  registerUser: async (data: RegisterUserRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<AuthResponse>(
      '/auth/register',
      data
    );
    return response.data;
  },

  logout: () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  },
};