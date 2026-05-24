import type { Tenant } from '../types';
import apiClient from './client';

export const tenantApi = {
  getCurrent: async (slug: string): Promise<Tenant> => {
    const res = await apiClient.get<Tenant>(`/tenant/${slug}`);
    return res.data;
  },
};