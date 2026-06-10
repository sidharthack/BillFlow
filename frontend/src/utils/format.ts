// Map currency codes to locale for proper formatting
const CURRENCY_LOCALE_MAP: Record<string, string> = {
  INR: 'en-IN',
  USD: 'en-US',
  EUR: 'en-IE', // or 'de-DE' or 'fr-FR' depending on preference
  GBP: 'en-GB',
  AUD: 'en-AU',
  SGD: 'en-SG',
  CAD: 'en-CA',
  JPY: 'ja-JP',
  CHF: 'de-CH',
};

export function getLocaleForCurrency(currency: string = 'INR'): string {
  return CURRENCY_LOCALE_MAP[currency] || 'en-US';
}

// Currency formatter
export function formatCurrency(
  amount: number,
  currency: string = 'INR'
): string {
  const locale = getLocaleForCurrency(currency);
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(amount);
}

// Get currency symbol from code
export function getCurrencySymbol(currency: string = 'INR'): string {
  const locale = getLocaleForCurrency(currency);
  const formatter = new Intl.NumberFormat(locale, {
    style: 'currency',
    currency,
  });
  // Format 0 to get just the symbol
  const parts = formatter.formatToParts(0);
  const symbolPart = parts.find(part => part.type === 'currency');
  return symbolPart?.value || '$';
}

// Date formatter
export function formatDate(dateStr: string | undefined, currency: string = 'INR'): string {
  if (!dateStr) return '—';
  const locale = getLocaleForCurrency(currency);
  return new Intl.DateTimeFormat(locale, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  }).format(new Date(dateStr));
}

// Relative time
export function formatRelative(dateStr: string, currency: string = 'INR'): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const days = Math.floor(diff / 86_400_000);
  if (days === 0) return 'Today';
  if (days === 1) return 'Yesterday';
  if (days < 7) return `${days} days ago`;
  if (days < 30) return `${Math.floor(days / 7)} weeks ago`;
  return formatDate(dateStr, currency);
}

// Invoice status colors for charts
export const STATUS_COLORS: Record<string, string> = {
  Draft:     '#94a3b8',
  Sent:      '#f59e0b',
  Paid:      '#10b981',
  Overdue:   '#ef4444',
  Cancelled: '#6b7280',
};