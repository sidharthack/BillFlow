import { useState, useMemo } from 'react';
import { Search, Plus, Users, Mail, Phone } from 'lucide-react';
import {
  useCustomers,
  useCreateCustomer,
} from '../../hooks/useCustomers';
import type { Customer } from '../../types';
import { Badge } from '../../components/ui/Badge';
import { Spinner } from '../../components/ui/Spinner';
import { EmptyState } from '../../components/ui/EmptyState';
import { PageHeader } from '../../components/ui/PageHeader';
import { Modal } from '../../components/ui/Modal';
import { CustomerForm, type CustomerFormData } from './CustomerForm';
import { CustomerDetail } from './CustomerDetail';
import { useToast } from '../../components/ui/Toast';

export function CustomersPage() {
  const [search, setSearch]               = useState('');
  const [createOpen, setCreateOpen]       = useState(false);
  const [selected, setSelected]           = useState<Customer | null>(null);

  const toast    = useToast();
  const { data: customers = [], isLoading, isError } = useCustomers();
  const create   = useCreateCustomer();

  // Client-side search across name, email, GST, phone
  const filtered = useMemo(() => {
    const q = search.toLowerCase();
    if (!q) return customers;
    return customers.filter(c =>
      c.name.toLowerCase().includes(q) ||
      c.email.toLowerCase().includes(q) ||
      c.gstNumber?.toLowerCase().includes(q) ||
      c.phone?.includes(q)
    );
  }, [customers, search]);

  const handleCreate = async (data: CustomerFormData) => {
    try {
      await create.mutateAsync(data);
      toast.success(`${data.name} added successfully`);
      setCreateOpen(false);
    } catch (err: any) {
      toast.error(
        err.response?.data?.message ?? 'Failed to create customer'
      );
    }
  };

  // ── Loading ───────────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <Spinner size="lg" />
      </div>
    );
  }

  // ── Error ─────────────────────────────────────────────────────────────
  if (isError) {
    return (
      <div className="p-8">
        <div className="card p-6 text-center text-red-600 text-sm">
          Failed to load customers. Make sure CustomerService is running.
        </div>
      </div>
    );
  }

  return (
    <div className="p-8 max-w-7xl mx-auto">

      {/* Header */}
      <PageHeader
        title="Customers"
        subtitle={`${customers.length} total · ${customers.filter(c => c.status === 'Active').length} active`}
        action={
          <button
            className="btn-primary"
            onClick={() => setCreateOpen(true)}
          >
            <Plus className="h-4 w-4" />
            Add Customer
          </button>
        }
      />

      {/* Search bar */}
      <div className="relative mb-6">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
        <input
          className="input pl-9 max-w-md"
          placeholder="Search by name, email, GST..."
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
      </div>

      {/* Empty state */}
      {customers.length === 0 ? (
        <EmptyState
          icon={Users}
          title="No customers yet"
          description="Add your first customer to start creating invoices."
          action={
            <button
              className="btn-primary"
              onClick={() => setCreateOpen(true)}
            >
              <Plus className="h-4 w-4" />
              Add Customer
            </button>
          }
        />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={Search}
          title="No results found"
          description={`No customers match "${search}". Try a different search.`}
        />
      ) : (
        /* Customer grid */
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map(customer => (
            <CustomerCard
              key={customer.id}
              customer={customer}
              onClick={() => setSelected(customer)}
            />
          ))}
        </div>
      )}

      {/* Create modal */}
      <Modal
        isOpen={createOpen}
        onClose={() => setCreateOpen(false)}
        title="Add Customer"
        size="lg"
      >
        <CustomerForm
          onSubmit={handleCreate}
          isLoading={create.isPending}
          submitLabel="Add Customer"
        />
      </Modal>

      {/* Detail panel */}
      <Modal
        isOpen={!!selected}
        onClose={() => setSelected(null)}
        title="Customer Details"
        size="md"
      >
        {selected && (
          <CustomerDetail
            customer={selected}
            onClose={() => setSelected(null)}
          />
        )}
      </Modal>
    </div>
  );
}

// ── Customer card ─────────────────────────────────────────────────────────

interface CardProps {
  customer: Customer;
  onClick: () => void;
}

function CustomerCard({ customer, onClick }: CardProps) {
  return (
    <button
      onClick={onClick}
      className="card p-5 text-left hover:shadow-md hover:border-primary-200
                 transition-all w-full group"
    >
      {/* Avatar + name */}
      <div className="flex items-center gap-3 mb-3">
        <div className="w-10 h-10 rounded-full bg-primary-100 text-primary-700
                        font-bold text-sm flex items-center justify-center
                        shrink-0 group-hover:bg-primary-500
                        group-hover:text-white transition-colors">
          {customer.name.charAt(0).toUpperCase()}
        </div>
        <div className="min-w-0">
          <p className="font-semibold text-gray-900 truncate text-sm">
            {customer.name}
          </p>
          <Badge label={customer.status} />
        </div>
      </div>

      {/* Contact details */}
      <div className="space-y-1.5">
        <div className="flex items-center gap-2 text-xs text-gray-500">
          <Mail className="h-3.5 w-3.5 shrink-0" />
          <span className="truncate">{customer.email}</span>
        </div>
        {customer.phone && (
          <div className="flex items-center gap-2 text-xs text-gray-500">
            <Phone className="h-3.5 w-3.5 shrink-0" />
            {customer.phone}
          </div>
        )}
        {customer.gstNumber && (
          <div className="flex items-center gap-2 text-xs text-gray-400">
            <span className="font-mono">GST: {customer.gstNumber}</span>
          </div>
        )}
      </div>
    </button>
  );
}