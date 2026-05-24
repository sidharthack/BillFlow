import { useState, useMemo } from 'react';
import { Plus, FileText, Search, Filter } from 'lucide-react';
import { useInvoices, useCreateInvoice } from '../../hooks/useInvoices';
import type { Invoice, InvoiceStatus } from '../../types';
import { Spinner } from '../../components/ui/Spinner';
import { EmptyState } from '../../components/ui/EmptyState';
import { PageHeader } from '../../components/ui/PageHeader';
import { Modal } from '../../components/ui/Modal';
import { CreateInvoiceForm, type CreateInvoiceFormData } from './CreateInvoiceForm';
import { InvoiceDetail } from './InvoiceDetail';
import { useToast } from '../../components/ui/Toast';
import { formatCurrency, formatDate } from '../../utils/format';
import { STATUS_BADGE_CLASSES } from '../../utils/invoiceStatus';
import { clsx } from 'clsx';

const STATUS_FILTERS: (InvoiceStatus | 'All')[] = [
  'All', 'Draft', 'Sent', 'Paid', 'Overdue', 'Cancelled',
];

export function InvoicesPage() {
  const [statusFilter, setStatusFilter] =
    useState<InvoiceStatus | 'All'>('All');
  const [search, setSearch]       = useState('');
  const [createOpen, setCreateOpen] = useState(false);
  const [selected, setSelected]   = useState<Invoice | null>(null);

  const toast  = useToast();
  const { data: invoices = [], isLoading, isError } = useInvoices();
  const create = useCreateInvoice();

  // Filter by status + search
  const filtered = useMemo(() => {
    let list = invoices;

    if (statusFilter !== 'All') {
      list = list.filter(i => i.status === statusFilter);
    }

    const q = search.toLowerCase();
    if (q) {
      list = list.filter(i =>
        i.invoiceNumber.toLowerCase().includes(q) ||
        i.customerName.toLowerCase().includes(q) ||
        i.customerEmail.toLowerCase().includes(q)
      );
    }

    return list;
  }, [invoices, statusFilter, search]);

  // Status counts for filter tabs
  const counts = useMemo(() =>
    invoices.reduce<Record<string, number>>((acc, inv) => {
      acc[inv.status] = (acc[inv.status] ?? 0) + 1;
      return acc;
    }, {}),
    [invoices]
  );

  const handleCreate = async (data: CreateInvoiceFormData) => {
    try {
      const created = await create.mutateAsync({
        customerId: data.customerId,
        lineItems:  data.lineItems,
        notes:      data.notes,
        dueDate:    data.dueDate
          ? new Date(data.dueDate).toISOString()
          : undefined,
      });
      toast.success(`${created.invoiceNumber} created`);
      setCreateOpen(false);
      setSelected(created);
    } catch (err: any) {
      toast.error(
        err.response?.data?.error ??
        err.response?.data?.message ??
        'Failed to create invoice'
      );
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <Spinner size="lg" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="p-8">
        <div className="card p-6 text-center text-red-600 text-sm">
          Failed to load invoices. Make sure InvoiceService is running.
        </div>
      </div>
    );
  }

  return (
    <div className="p-8 max-w-7xl mx-auto">

      {/* Header */}
      <PageHeader
        title="Invoices"
        subtitle={`${invoices.length} total invoices`}
        action={
          <button
            className="btn-primary"
            onClick={() => setCreateOpen(true)}
          >
            <Plus className="h-4 w-4" />
            New Invoice
          </button>
        }
      />

      {/* Status filter tabs */}
      <div className="flex items-center gap-1 mb-4 flex-wrap">
        {STATUS_FILTERS.map(status => (
          <button
            key={status}
            onClick={() => setStatusFilter(status)}
            className={clsx(
              'px-3 py-1.5 rounded-lg text-xs font-medium transition-all',
              statusFilter === status
                ? 'bg-primary-500 text-white shadow-sm'
                : 'bg-white text-gray-600 border border-gray-200 hover:border-primary-200'
            )}
          >
            {status}
            {status !== 'All' && counts[status] ? (
              <span className={clsx(
                'ml-1.5 rounded-full px-1.5 py-0.5 text-xs',
                statusFilter === status
                  ? 'bg-white/20 text-white'
                  : 'bg-gray-100 text-gray-500'
              )}>
                {counts[status]}
              </span>
            ) : null}
          </button>
        ))}
      </div>

      {/* Search */}
      <div className="relative mb-6">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2
                           h-4 w-4 text-gray-400" />
        <input
          className="input pl-9 max-w-md"
          placeholder="Search by invoice number, customer..."
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
      </div>

      {/* Empty states */}
      {invoices.length === 0 ? (
        <EmptyState
          icon={FileText}
          title="No invoices yet"
          description="Create your first invoice to get started."
          action={
            <button
              className="btn-primary"
              onClick={() => setCreateOpen(true)}
            >
              <Plus className="h-4 w-4" />
              New Invoice
            </button>
          }
        />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={Filter}
          title="No invoices match"
          description="Try a different status filter or search term."
        />
      ) : (

        /* Invoice table */
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-100">
              <tr>
                <th className="text-left px-6 py-3 text-xs font-medium
                               text-gray-400 uppercase tracking-wide">
                  Invoice
                </th>
                <th className="text-left px-6 py-3 text-xs font-medium
                               text-gray-400 uppercase tracking-wide">
                  Customer
                </th>
                <th className="text-left px-6 py-3 text-xs font-medium
                               text-gray-400 uppercase tracking-wide">
                  Amount
                </th>
                <th className="text-left px-6 py-3 text-xs font-medium
                               text-gray-400 uppercase tracking-wide">
                  Status
                </th>
                <th className="text-left px-6 py-3 text-xs font-medium
                               text-gray-400 uppercase tracking-wide">
                  Date
                </th>
                <th className="text-left px-6 py-3 text-xs font-medium
                               text-gray-400 uppercase tracking-wide">
                  Due
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {filtered.map(invoice => (
                <InvoiceRow
                  key={invoice.id}
                  invoice={invoice}
                  onClick={() => setSelected(invoice)}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Create modal */}
      <Modal
        isOpen={createOpen}
        onClose={() => setCreateOpen(false)}
        title="New Invoice"
        size="lg"
      >
        <CreateInvoiceForm
          onSubmit={handleCreate}
          isLoading={create.isPending}
        />
      </Modal>

      {/* Detail modal */}
      <Modal
        isOpen={!!selected}
        onClose={() => setSelected(null)}
        title="Invoice Details"
        size="lg"
      >
        {selected && (
          <InvoiceDetail
            invoice={
              // Use fresh data from cache if available
              invoices.find(i => i.id === selected.id) ?? selected
            }
          />
        )}
      </Modal>
    </div>
  );
}

// ── Invoice row ───────────────────────────────────────────────────────────

function InvoiceRow({
  invoice,
  onClick,
}: {
  invoice: Invoice;
  onClick: () => void;
}) {
  const isOverdue = invoice.status === 'Overdue';

  return (
    <tr
      onClick={onClick}
      className={clsx(
        'cursor-pointer transition-colors hover:bg-gray-50',
        isOverdue && 'bg-red-50/30'
      )}
    >
      <td className="px-6 py-4">
        <span className="font-mono font-medium text-primary-600 text-sm">
          {invoice.invoiceNumber}
        </span>
      </td>
      <td className="px-6 py-4">
        <div>
          <p className="font-medium text-gray-900 text-sm">
            {invoice.customerName}
          </p>
          <p className="text-xs text-gray-400">{invoice.customerEmail}</p>
        </div>
      </td>
      <td className="px-6 py-4 font-medium text-gray-900">
        {formatCurrency(invoice.totalAmount, invoice.currency)}
      </td>
      <td className="px-6 py-4">
        <span
          className={clsx(
            'badge',
            STATUS_BADGE_CLASSES[invoice.status as InvoiceStatus]
          )}
        >
          {invoice.status}
        </span>
      </td>
      <td className="px-6 py-4 text-gray-500 text-sm">
        {formatDate(invoice.createdAt)}
      </td>
      <td className="px-6 py-4">
        {invoice.dueDate ? (
          <span
            className={clsx(
              'text-sm',
              isOverdue ? 'text-red-600 font-medium' : 'text-gray-500'
            )}
          >
            {formatDate(invoice.dueDate)}
          </span>
        ) : (
          <span className="text-gray-300">—</span>
        )}
      </td>
    </tr>
  );
}