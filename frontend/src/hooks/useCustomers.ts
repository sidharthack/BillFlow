import {
  useQuery,
  useMutation,
  useQueryClient,
} from '@tanstack/react-query';
import { customersApi } from '../api/customers';
import type { CreateCustomerRequest } from '../types';

export function useCustomers() {
  return useQuery({
    queryKey: ['customers'],
    queryFn: customersApi.getAll,
  });
}

export function useCustomer(id: number) {
  return useQuery({
    queryKey: ['customers', id],
    queryFn: () => customersApi.getById(id),
    enabled: !!id,
  });
}

export function useCreateCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateCustomerRequest) => customersApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['customers'] });
    },
  });
}

export function useUpdateCustomer(id: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<CreateCustomerRequest>) =>
      customersApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['customers'] });
      qc.invalidateQueries({ queryKey: ['customers', id] });
    },
  });
}

export function useDeactivateCustomer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => customersApi.deactivate(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['customers'] });
    },
  });
}