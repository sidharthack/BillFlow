import type { InvoiceStatus } from '../types';

// Valid transitions per status — mirrors the backend state machine
export const ALLOWED_TRANSITIONS: Record<InvoiceStatus, InvoiceStatus[]> = {
  Draft:     ['Sent', 'Cancelled'],
  Sent:      ['Paid', 'Overdue', 'Cancelled'],
  Overdue:   ['Paid', 'Cancelled'],
  Paid:      [],
  Cancelled: [],
};

export interface TransitionAction {
  toStatus: InvoiceStatus;
  label: string;
  style: 'primary' | 'success' | 'danger' | 'warning';
}

export const TRANSITION_ACTIONS: Record<InvoiceStatus, TransitionAction[]> = {
  Draft: [
    { toStatus: 'Sent',      label: 'Send Invoice', style: 'primary'  },
    { toStatus: 'Cancelled', label: 'Cancel',       style: 'danger'   },
  ],
  Sent: [
    { toStatus: 'Paid',      label: 'Mark as Paid', style: 'success'  },
    { toStatus: 'Overdue',   label: 'Mark Overdue', style: 'warning'  },
    { toStatus: 'Cancelled', label: 'Cancel',       style: 'danger'   },
  ],
  Overdue: [
    { toStatus: 'Paid',      label: 'Mark as Paid', style: 'success'  },
    { toStatus: 'Cancelled', label: 'Cancel',       style: 'danger'   },
  ],
  Paid:      [],
  Cancelled: [],
};

export const STATUS_BADGE_CLASSES: Record<InvoiceStatus, string> = {
  Draft:     'bg-gray-100 text-gray-700',
  Sent:      'bg-yellow-100 text-yellow-700',
  Paid:      'bg-green-100 text-green-700',
  Overdue:   'bg-red-100 text-red-700',
  Cancelled: 'bg-gray-100 text-gray-400 line-through',
};