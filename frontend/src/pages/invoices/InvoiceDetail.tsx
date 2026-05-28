import { useState } from 'react';
import {
  Download, ArrowRight, FileText,
  Clock, CheckCircle, XCircle,
  AlertTriangle, User, Calendar,
} from 'lucide-react';
import type { Invoice, InvoiceStatus } from '../../types';
import { Badge } from '../../components/ui/Badge';
import { Spinner } from '../../components/ui/Spinner';
import { ConfirmDialog } from '../../components/ui/ConfirmDialog';
import {
  useTransitionInvoice,
  useDownloadPdf,
} from '../../hooks/useInvoices';
import { useToast } from '../../components/ui/Toast';
import { formatCurrency, formatDate, formatRelative } from '../../utils/format';
import {
  TRANSITION_ACTIONS,
  type TransitionAction,
} from '../../utils/invoiceStatus';
import { clsx } from 'clsx';

interface Props {
  invoice: Invoice;
}

export function InvoiceDetail({ invoice }: Props) {
  const [pendingAction, setPendingAction] =
    useState<TransitionAction | null>(null);

  const toast      = useToast();
  const transition = useTransitionInvoice();
  const download   = useDownloadPdf();

  const actions = TRANSITION_ACTIONS[invoice.status as InvoiceStatus] ?? [];

  const handleTransition = async () => {
    if (!pendingAction) return;
    try {
      await transition.mutateAsync({
        id:       invoice.id,
        toStatus: pendingAction.toStatus,
        note:     `Manually set to ${pendingAction.toStatus}`,
      });
      toast.success(`Invoice ${pendingAction.toStatus.toLowerCase()}`);
      setPendingAction(null);
    } catch (err: any) {
      toast.error(
        err.response?.data?.error ?? 'Transition failed'
      );
      setPendingAction(null);
    }
  };

  const handleDownload = async () => {
    try {
      await download.mutateAsync(invoice.id);
      toast.success('PDF downloaded');
    } catch {
      toast.error('Failed to download PDF');
    }
  };

  return (
    <div className="space-y-6">

      {/* Invoice header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <h3 className="text-lg font-bold text-gray-900 font-mono">
              {invoice.invoiceNumber}
            </h3>
            <Badge label={invoice.status} />
          </div>
          <p className="text-xs text-gray-400">
            Created {formatRelative(invoice.createdAt)}
          </p>
        </div>

        {/* Action buttons */}
        <div className="flex items-center gap-2">
          <button
            onClick={handleDownload}
            disabled={download.isPending}
            className="btn-secondary text-xs px-3 py-1.5"
          >
            {download.isPending ? (
              <Spinner size="sm" />
            ) : (
              <Download className="h-3.5 w-3.5" />
            )}
            PDF
          </button>

          {actions.map(action => (
            <button
              key={action.toStatus}
              onClick={() => setPendingAction(action)}
              disabled={transition.isPending}
              className={clsx('text-xs px-3 py-1.5', {
                'btn-primary':   action.style === 'primary',
                'btn-secondary bg-green-600 text-white hover:bg-green-700 border-green-600':
                                 action.style === 'success',
                'btn-danger':    action.style === 'danger',
                'btn-secondary bg-amber-500 text-white hover:bg-amber-600 border-amber-500':
                                 action.style === 'warning',
              })}
            >
              {action.label}
            </button>
          ))}
        </div>
      </div>

      {/* Customer + dates */}
      <div className="grid grid-cols-2 gap-4 text-sm">
        <div className="rounded-lg bg-gray-50 p-4">
          <p className="text-xs text-gray-400 uppercase tracking-wider
                        font-semibold mb-2">
            Bill To
          </p>
          <div className="flex items-center gap-2 mb-1">
            <User className="h-3.5 w-3.5 text-gray-400" />
            <span className="font-medium text-gray-900">
              {invoice.customerName}
            </span>
          </div>
          <p className="text-gray-500 text-xs pl-5">{invoice.customerEmail}</p>
          {invoice.customerGstNumber && (
            <p className="text-gray-400 text-xs pl-5 font-mono mt-1">
              GST: {invoice.customerGstNumber}
            </p>
          )}
        </div>

        <div className="rounded-lg bg-gray-50 p-4 space-y-2">
          <p className="text-xs text-gray-400 uppercase tracking-wider
                        font-semibold mb-2">
            Dates
          </p>
          <div className="flex items-center gap-2 text-xs">
            <Calendar className="h-3.5 w-3.5 text-gray-400" />
            <span className="text-gray-500">Issued:</span>
            <span className="text-gray-700">
              {formatDate(invoice.createdAt)}
            </span>
          </div>
          {invoice.dueDate && (
            <div className="flex items-center gap-2 text-xs">
              <Clock className="h-3.5 w-3.5 text-amber-400" />
              <span className="text-gray-500">Due:</span>
              <span
                className={clsx(
                  'font-medium',
                  invoice.status === 'Overdue'
                    ? 'text-red-600'
                    : 'text-gray-700'
                )}
              >
                {formatDate(invoice.dueDate)}
              </span>
            </div>
          )}
          {invoice.paidAt && (
            <div className="flex items-center gap-2 text-xs">
              <CheckCircle className="h-3.5 w-3.5 text-green-500" />
              <span className="text-gray-500">Paid:</span>
              <span className="text-green-600 font-medium">
                {formatDate(invoice.paidAt)}
              </span>
            </div>
          )}
        </div>
      </div>

      {/* Line items table */}
      <div className="overflow-x-auto rounded-lg border border-gray-100">
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="text-left px-4 py-2.5 text-xs font-medium
                             text-gray-400 uppercase tracking-wide">
                Description
              </th>
              <th className="text-right px-4 py-2.5 text-xs font-medium
                             text-gray-400 uppercase tracking-wide">
                Qty
              </th>
              <th className="text-right px-4 py-2.5 text-xs font-medium
                             text-gray-400 uppercase tracking-wide">
                Unit Price
              </th>
              <th className="text-right px-4 py-2.5 text-xs font-medium
                             text-gray-400 uppercase tracking-wide">
                Amount
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-50">
            {invoice.lineItems.map(item => (
              <tr key={item.id} className="hover:bg-gray-50/50">
                <td className="px-4 py-3 text-gray-900">
                  {item.description}
                </td>
                <td className="px-4 py-3 text-right text-gray-600">
                  {item.quantity}
                </td>
                <td className="px-4 py-3 text-right text-gray-600">
                  {formatCurrency(item.unitPrice, invoice.currency)}
                </td>
                <td className="px-4 py-3 text-right font-medium text-gray-900">
                  {formatCurrency(item.amount, invoice.currency)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Totals */}
      <div className="flex justify-end">
        <div className="w-64 space-y-2 text-sm">
          <div className="flex justify-between text-gray-600">
            <span>Subtotal</span>
            <span>{formatCurrency(invoice.subTotal, invoice.currency)}</span>
          </div>
          <div className="flex justify-between text-gray-600">
            <span>GST ({(invoice.taxRate * 100).toFixed(0)}%)</span>
            <span>{formatCurrency(invoice.taxAmount, invoice.currency)}</span>
          </div>
          <div className="flex justify-between text-base font-bold
                          text-gray-900 pt-2 border-t border-gray-200">
            <span>Total</span>
            <span className="text-primary-600">
              {formatCurrency(invoice.totalAmount, invoice.currency)}
            </span>
          </div>
        </div>
      </div>

      {/* Notes */}
      {invoice.notes && (
        <div className="rounded-lg bg-amber-50 border border-amber-100 p-4">
          <p className="text-xs font-semibold text-amber-700 mb-1">Notes</p>
          <p className="text-sm text-amber-800">{invoice.notes}</p>
        </div>
      )}

      {/* Audit trail */}
      <div>
        <p className="text-xs font-semibold text-gray-400 uppercase
                      tracking-wider mb-3">
          Audit Trail
        </p>
        <div className="space-y-3">
          {invoice.events.map((event, i) => (
            <div key={i} className="flex items-start gap-3">
              <div className="mt-0.5">
                {event.toStatus === 'Paid' && (
                  <CheckCircle className="h-4 w-4 text-green-500" />
                )}
                {event.toStatus === 'Cancelled' && (
                  <XCircle className="h-4 w-4 text-gray-400" />
                )}
                {event.toStatus === 'Overdue' && (
                  <AlertTriangle className="h-4 w-4 text-red-500" />
                )}
                {event.toStatus === 'Sent' && (
                  <ArrowRight className="h-4 w-4 text-yellow-500" />
                )}
                {event.toStatus === 'Draft' && (
                  <FileText className="h-4 w-4 text-gray-400" />
                )}
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium text-gray-900">
                    {event.fromStatus === 'None'
                      ? 'Invoice created'
                      : `${event.fromStatus} → ${event.toStatus}`}
                  </span>
                </div>
                {event.note && (
                  <p className="text-xs text-gray-400 mt-0.5">{event.note}</p>
                )}
                <p className="text-xs text-gray-400 mt-0.5">
                  {formatRelative(event.occurredAt)}
                </p>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Confirm transition dialog */}
      {pendingAction && (
        <ConfirmDialog
          isOpen={!!pendingAction}
          onClose={() => setPendingAction(null)}
          onConfirm={handleTransition}
          title={pendingAction.label}
          message={`Are you sure you want to mark this invoice as ${pendingAction.toStatus}? This action follows the invoice status rules.`}
          confirmLabel={pendingAction.label}
          isDanger={pendingAction.style === 'danger'}
          isLoading={transition.isPending}
        />
      )}
    </div>
  );
}