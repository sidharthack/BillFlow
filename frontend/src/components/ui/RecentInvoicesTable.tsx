import { useNavigate } from 'react-router-dom';
import type { Invoice } from '../../types';
import { Badge } from './Badge';
import { formatCurrency, formatRelative } from '../../utils/format';
import { FileText } from 'lucide-react';

interface Props {
  invoices: Invoice[];
}

export function RecentInvoicesTable({ invoices }: Props) {
  const navigate = useNavigate();

  if (!invoices.length) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-sm text-gray-400">
        <FileText className="h-8 w-8 mb-2 text-gray-300" />
        No invoices yet
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-gray-100">
            <th className="text-left py-3 px-4 text-xs font-medium text-gray-400 uppercase tracking-wide">
              Invoice
            </th>
            <th className="text-left py-3 px-4 text-xs font-medium text-gray-400 uppercase tracking-wide">
              Customer
            </th>
            <th className="text-left py-3 px-4 text-xs font-medium text-gray-400 uppercase tracking-wide">
              Amount
            </th>
            <th className="text-left py-3 px-4 text-xs font-medium text-gray-400 uppercase tracking-wide">
              Status
            </th>
            <th className="text-left py-3 px-4 text-xs font-medium text-gray-400 uppercase tracking-wide">
              Date
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-50">
          {invoices.map(inv => (
            <tr
              key={inv.id}
              onClick={() => navigate(`/invoices/${inv.id}`)}
              className="hover:bg-gray-50 cursor-pointer transition-colors"
            >
              <td className="py-3 px-4 font-medium text-primary-600">
                {inv.invoiceNumber}
              </td>
              <td className="py-3 px-4 text-gray-900">
                {inv.customerName}
              </td>
              <td className="py-3 px-4 font-medium text-gray-900">
                {formatCurrency(inv.totalAmount, inv.currency)}
              </td>
              <td className="py-3 px-4">
                <Badge label={inv.status} />
              </td>
              <td className="py-3 px-4 text-gray-400">
                {formatRelative(inv.createdAt)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}