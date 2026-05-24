// ── Auth ──────────────────────────────────────────────────────────────────
export interface LoginRequest {
  tenantSlug: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserInfo;
}

export interface UserInfo {
  id: number;
  email: string;
  fullName: string;
  role: string;
  tenantSlug: string;
  tenantId: number;
}

// ── Tenant ────────────────────────────────────────────────────────────────
export interface TenantSettings {
  companyName: string;
  logoUrl?: string;
  primaryColor: string;
  currency: string;
  countryCode: string;
  defaultTaxRate: number;
  invoicePrefix: string;
}

export interface Tenant {
  id: number;
  slug: string;
  name: string;
  ownerEmail: string;
  plan: string;
  status: string;
  createdAt: string;
  settings: TenantSettings;
}

// ── Customer ──────────────────────────────────────────────────────────────
export interface Address {
  line1: string;
  line2?: string;
  city: string;
  state: string;
  pinCode: string;
  country: string;
}

export interface Customer {
  id: number;
  name: string;
  email: string;
  phone?: string;
  gstNumber?: string;
  panNumber?: string;
  status: string;
  createdAt: string;
  address?: Address;
}

export interface CreateCustomerRequest {
  name: string;
  email: string;
  phone?: string;
  gstNumber?: string;
  panNumber?: string;
  address?: Omit<Address, 'country'> & { country?: string };
}

// ── Invoice ───────────────────────────────────────────────────────────────
export interface LineItem {
  id: number;
  description: string;
  quantity: number;
  unitPrice: number;
  amount: number;
}

export interface InvoiceEvent {
  fromStatus: string;
  toStatus: string;
  note?: string;
  occurredAt: string;
}

export interface Invoice {
  id: number;
  invoiceNumber: string;
  customerId: number;
  customerName: string;
  customerEmail: string;
  customerGstNumber?: string;
  subTotal: number;
  taxRate: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  status: string;
  notes?: string;
  createdAt: string;
  sentAt?: string;
  paidAt?: string;
  dueDate?: string;
  cancelledAt?: string;
  lineItems: LineItem[];
  events: InvoiceEvent[];
}

export interface CreateInvoiceRequest {
  customerId: number;
  lineItems: { description: string; quantity: number; unitPrice: number }[];
  notes?: string;
  dueDate?: string;
}

export type InvoiceStatus =
  | 'Draft'
  | 'Sent'
  | 'Paid'
  | 'Overdue'
  | 'Cancelled';