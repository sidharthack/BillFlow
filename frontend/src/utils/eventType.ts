export const EVENT_TYPE_CONFIG: Record<string, { label: string; color: string; description: string }> = {
  InvoiceCreated: {
    label:       'Invoice Created',
    color:       'text-primary-600',
    description: 'Sent when a new invoice is created',
  },
  InvoiceSent: {
    label:       'Invoice Sent',
    color:       'text-yellow-600',
    description: 'Payment request sent to customer',
  },
  InvoiceOverdue: {
    label:       'Invoice Overdue',
    color:       'text-red-600',
    description: 'Overdue reminder sent to customer',
  },
  InvoicePaid: {
    label:       'Payment Received',
    color:       'text-green-600',
    description: 'Payment confirmation sent to customer',
  },
};