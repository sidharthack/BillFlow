import { clsx } from 'clsx';

const variants: Record<string, string> = {
  Draft:      'bg-gray-100 text-gray-700',
  Sent:       'bg-yellow-100 text-yellow-700',
  Paid:       'bg-green-100 text-green-700',
  Overdue:    'bg-red-100 text-red-700',
  Cancelled:  'bg-gray-100 text-gray-500',
  Active:     'bg-green-100 text-green-700',
  Inactive:   'bg-gray-100 text-gray-500',
  Admin:      'bg-purple-100 text-purple-700',
  Member:     'bg-blue-100 text-blue-700',
  Viewer:     'bg-gray-100 text-gray-600',
};

interface BadgeProps {
  label: string;
  className?: string;
}

export function Badge({ label, className }: BadgeProps) {
  return (
    <span
      className={clsx(
        'badge',
        variants[label] ?? 'bg-gray-100 text-gray-700',
        className
      )}
    >
      {label}
    </span>
  );
}