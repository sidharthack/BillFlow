import type { AuthResponse, LoginRequest, Tenant } from '../types';
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
    const res = await apiClient.post<AuthResponse>('/auth/login', data);
    return res.data;
  },

  registerTenant: async (
    data: RegisterTenantRequest
  ): Promise<RegisterTenantResponse> => {
    const res = await apiClient.post<RegisterTenantResponse>(
      '/tenant/register', data
    );
    return res.data;
  },

  registerUser: async (
    data: RegisterUserRequest
  ): Promise<AuthResponse> => {
    const res = await apiClient.post<AuthResponse>('/auth/register', data);
    return res.data;
  },

  logout: () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
  },
};

export const devApi = {
  // List all tenants
  getAllTenants: async (): Promise<Tenant[]> => {
    const res = await apiClient.get<Tenant[]>('/tenant');
    return res.data;
  },

  // Get single tenant
  getTenant: async (slug: string): Promise<Tenant> => {
    const res = await apiClient.get<Tenant>(`/tenant/${slug}`);
    return res.data;
  },

  // Check if tenant exists
  checkTenantExists: async (slug: string): Promise<boolean> => {
    const res = await apiClient.get<{ slug: string; exists: boolean }>(
      `/tenant/exists/${slug}`
    );
    return res.data.exists;
  },
};