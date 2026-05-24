import { useState } from 'react';
import {
  Mail, Phone, MapPin, FileText,
  Edit2, UserX, Building2
} from 'lucide-react';
import type { Customer } from '../../types';
import { Badge } from '../../components/ui/Badge';
import { Modal } from '../../components/ui/Modal';
import { ConfirmDialog } from '../../components/ui/ConfirmDialog';
import { CustomerForm, type CustomerFormData } from './CustomerForm';
import { useUpdateCustomer, useDeactivateCustomer } from '../../hooks/useCustomers';
import { useToast } from '../../components/ui/Toast';
import { formatDate } from '../../utils/format';

interface Props {
  customer: Customer;
  onClose: () => void;
}

export function CustomerDetail({ customer, onClose }: Props) {
  const [editOpen, setEditOpen]         = useState(false);
  const [confirmOpen, setConfirmOpen]   = useState(false);

  const toast      = useToast();
  const update     = useUpdateCustomer(customer.id);
  const deactivate = useDeactivateCustomer();

  const handleUpdate = async (data: CustomerFormData) => {
    try {
      await update.mutateAsync(data);
      toast.success('Customer updated successfully');
      setEditOpen(false);
    } catch {
      toast.error('Failed to update customer');
    }
  };

  const handleDeactivate = async () => {
    try {
      await deactivate.mutateAsync(customer.id);
      toast.success('Customer deactivated');
      setConfirmOpen(false);
      onClose();
    } catch {
      toast.error('Failed to deactivate customer');
    }
  };

  const defaultValues: CustomerFormData = {
    name:      customer.name,
    email:     customer.email,
    phone:     customer.phone,
    gstNumber: customer.gstNumber,
    panNumber: customer.panNumber,
    address:   customer.address
      ? {
          line1:   customer.address.line1,
          line2:   customer.address.line2,
          city:    customer.address.city,
          state:   customer.address.state,
          pinCode: customer.address.pinCode,
          country: customer.address.country,
        }
      : undefined,
  };

  return (
    <>
      <div className="space-y-6">

        {/* Header */}
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-full bg-primary-100
                            text-primary-700 font-bold text-sm
                            flex items-center justify-center">
              {customer.name.charAt(0).toUpperCase()}
            </div>
            <div>
              <h3 className="font-semibold text-gray-900">{customer.name}</h3>
              <Badge label={customer.status} />
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setEditOpen(true)}
              className="btn-secondary text-xs px-3 py-1.5"
            >
              <Edit2 className="h-3.5 w-3.5" />
              Edit
            </button>
            {customer.status === 'Active' && (
              <button
                onClick={() => setConfirmOpen(true)}
                className="btn-danger text-xs px-3 py-1.5"
              >
                <UserX className="h-3.5 w-3.5" />
                Deactivate
              </button>
            )}
          </div>
        </div>

        {/* Contact */}
        <div className="grid grid-cols-2 gap-4">
          <div className="flex items-center gap-2 text-sm text-gray-600">
            <Mail className="h-4 w-4 text-gray-400" />
            
              href={`mailto:${customer.email}`}
              className="hover:text-primary-600 truncate"
            <a>
              {customer.email}
            </a>
          </div>
          {customer.phone && (
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <Phone className="h-4 w-4 text-gray-400" />
              {customer.phone}
            </div>
          )}
        </div>

        {/* Tax info */}
        {(customer.gstNumber || customer.panNumber) && (
          <div className="rounded-lg bg-gray-50 p-4 space-y-2">
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">
              Tax Information
            </p>
            {customer.gstNumber && (
              <div className="flex items-center gap-2 text-sm">
                <FileText className="h-4 w-4 text-gray-400" />
                <span className="text-gray-500">GST:</span>
                <span className="font-mono font-medium text-gray-900">
                  {customer.gstNumber}
                </span>
              </div>
            )}
            {customer.panNumber && (
              <div className="flex items-center gap-2 text-sm">
                <Building2 className="h-4 w-4 text-gray-400" />
                <span className="text-gray-500">PAN:</span>
                <span className="font-mono font-medium text-gray-900">
                  {customer.panNumber}
                </span>
              </div>
            )}
          </div>
        )}

        {/* Address */}
        {customer.address && (
          <div className="flex items-start gap-2 text-sm text-gray-600">
            <MapPin className="h-4 w-4 text-gray-400 mt-0.5 shrink-0" />
            <div>
              <p>{customer.address.line1}</p>
              {customer.address.line2 && <p>{customer.address.line2}</p>}
              <p>
                {customer.address.city}, {customer.address.state}{' '}
                {customer.address.pinCode}
              </p>
              <p>{customer.address.country}</p>
            </div>
          </div>
        )}

        {/* Footer */}
        <div className="pt-4 border-t border-gray-100">
          <p className="text-xs text-gray-400">
            Customer since {formatDate(customer.createdAt)}
          </p>
        </div>
      </div>

      {/* Edit modal */}
      <Modal
        isOpen={editOpen}
        onClose={() => setEditOpen(false)}
        title="Edit Customer"
        size="lg"
      >
        <CustomerForm
          defaultValues={defaultValues}
          onSubmit={handleUpdate}
          isLoading={update.isPending}
          submitLabel="Update Customer"
        />
      </Modal>

      {/* Deactivate confirm */}
      <ConfirmDialog
        isOpen={confirmOpen}
        onClose={() => setConfirmOpen(false)}
        onConfirm={handleDeactivate}
        title="Deactivate Customer"
        message={`Are you sure you want to deactivate ${customer.name}? They will no longer appear in the active customer list.`}
        confirmLabel="Deactivate"
        isDanger
        isLoading={deactivate.isPending}
      />
    </>
  );
}