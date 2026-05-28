import {
  useQuery,
  useMutation,
  useQueryClient,
} from '@tanstack/react-query';
import { invoicesApi } from '../api/invoices';
import type { CreateInvoiceRequest } from '../types';

export function useInvoices() {
  return useQuery({
    queryKey: ['invoices'],
    queryFn: invoicesApi.getAll,
  });
}

export function useInvoice(id: number) {
  return useQuery({
    queryKey: ['invoices', id],
    queryFn: () => invoicesApi.getById(id),
    enabled: !!id,
  });
}

export function useCreateInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateInvoiceRequest) => invoicesApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['invoices'] });
    },
  });
}

export function useTransitionInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      toStatus,
      note,
    }: {
      id: number;
      toStatus: string;
      note?: string;
    }) => invoicesApi.transition(id, toStatus, note),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: ['invoices'] });
      qc.invalidateQueries({ queryKey: ['invoices', id] });
    },
  });
}

export function useDownloadPdf() {
  return useMutation({
    mutationFn: (id: number) => invoicesApi.downloadPdf(id),
  });
}