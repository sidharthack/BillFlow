import type { Invoice, CreateInvoiceRequest } from '../types';
import apiClient from './client';

export const invoicesApi = {
  getAll: async (): Promise<Invoice[]> => {
    const res = await apiClient.get<Invoice[]>('/invoice');
    return res.data;
  },

  getById: async (id: number): Promise<Invoice> => {
    const res = await apiClient.get<Invoice>(`/invoice/${id}`);
    return res.data;
  },

  create: async (data: CreateInvoiceRequest): Promise<Invoice> => {
    const res = await apiClient.post<Invoice>('/invoice', data);
    return res.data;
  },

  transition: async (
    id: number,
    toStatus: string,
    note?: string
  ): Promise<Invoice> => {
    const res = await apiClient.post<Invoice>(
      `/invoice/${id}/transition`,
      { toStatus, note }
    );
    return res.data;
  },

  downloadPdf: async (id: number): Promise<void> => {
    const res = await apiClient.get(`/invoice/${id}/pdf`, {
      responseType: 'blob',
    });
    const url = URL.createObjectURL(new Blob([res.data]));
    const link = document.createElement('a');
    link.href = url;
    link.download = `invoice-${id}.pdf`;
    link.click();
    URL.revokeObjectURL(url);
  },
}; 