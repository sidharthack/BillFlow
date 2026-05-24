import type { Customer, CreateCustomerRequest } from '../types';
import apiClient from './client';

export const customersApi = {
  getAll: async (): Promise<Customer[]> => {
    const res = await apiClient.get<Customer[]>('/customer');
    return res.data;
  },

  getById: async (id: number): Promise<Customer> => {
    const res = await apiClient.get<Customer>(`/customer/${id}`);
    return res.data;
  },

  create: async (data: CreateCustomerRequest): Promise<Customer> => {
    const res = await apiClient.post<Customer>('/customer', data);
    return res.data;
  },

  update: async (id: number, data: Partial<CreateCustomerRequest>)
    : Promise<Customer> => {
    const res = await apiClient.put<Customer>(`/customer/${id}`, data);
    return res.data;
  },

  deactivate: async (id: number): Promise<void> => {
    await apiClient.delete(`/customer/${id}`);
  },
};