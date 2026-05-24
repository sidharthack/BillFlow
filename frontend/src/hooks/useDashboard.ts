import { useQuery } from '@tanstack/react-query';
import { invoicesApi } from '../api/invoices';
import { customersApi } from '../api/customers';
import type { Invoice } from '../types';
import { STATUS_COLORS } from '../utils/format';

export interface DashboardStats {
  totalInvoices: number;
  totalRevenue: number;
  overdueCount: number;
  overdueAmount: number;
  paidCount: number;
  draftCount: number;
  sentCount: number;
  totalCustomers: number;
  statusBreakdown: { name: string; value: number; color: string }[];
  recentInvoices: Invoice[];
  monthlyRevenue: { month: string; revenue: number; count: number }[];
}

function buildStats(invoices: Invoice[], customerCount: number): DashboardStats {
  const paid     = invoices.filter(i => i.status === 'Paid');
  const overdue  = invoices.filter(i => i.status === 'Overdue');
  const draft    = invoices.filter(i => i.status === 'Draft');
  const sent     = invoices.filter(i => i.status === 'Sent');

  const totalRevenue  = paid.reduce((s, i) => s + i.totalAmount, 0);
  const overdueAmount = overdue.reduce((s, i) => s + i.totalAmount, 0);

  // Status breakdown for pie chart
  const statusBreakdown = Object.entries(
    invoices.reduce<Record<string, number>>((acc, inv) => {
      acc[inv.status] = (acc[inv.status] ?? 0) + 1;
      return acc;
    }, {})
  ).map(([name, value]) => ({
    name,
    value,
    color: STATUS_COLORS[name] ?? '#94a3b8',
  }));

  // Monthly revenue for bar chart — last 6 months
  const monthlyRevenue = buildMonthlyRevenue(paid);

  return {
    totalInvoices: invoices.length,
    totalRevenue,
    overdueCount: overdue.length,
    overdueAmount,
    paidCount: paid.length,
    draftCount: draft.length,
    sentCount: sent.length,
    totalCustomers: customerCount,
    statusBreakdown,
    recentInvoices: invoices.slice(0, 8),
    monthlyRevenue,
  };
}

function buildMonthlyRevenue(
  paidInvoices: Invoice[]
): { month: string; revenue: number; count: number }[] {
  const now = new Date();
  const months: { month: string; revenue: number; count: number }[] = [];

  for (let i = 5; i >= 0; i--) {
    const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
    const label = d.toLocaleString('en-IN', {
      month: 'short',
      year: '2-digit',
    });

    const monthInvoices = paidInvoices.filter(inv => {
      const paid = new Date(inv.paidAt!);
      return (
        paid.getMonth() === d.getMonth() &&
        paid.getFullYear() === d.getFullYear()
      );
    });

    months.push({
      month: label,
      revenue: monthInvoices.reduce((s, i) => s + i.totalAmount, 0),
      count: monthInvoices.length,
    });
  }

  return months;
}

export function useDashboard() {
  const invoicesQuery = useQuery({
    queryKey: ['invoices'],
    queryFn: invoicesApi.getAll,
  });

  const customersQuery = useQuery({
    queryKey: ['customers'],
    queryFn: customersApi.getAll,
  });

  const isLoading = invoicesQuery.isLoading || customersQuery.isLoading;
  const isError   = invoicesQuery.isError   || customersQuery.isError;

  const stats = !isLoading && !isError
    ? buildStats(
        invoicesQuery.data ?? [],
        customersQuery.data?.length ?? 0
      )
    : null;

  return { stats, isLoading, isError };
}