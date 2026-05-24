 import type { LucideIcon } from 'lucide-react';
import { clsx } from 'clsx';

interface StatCardProps {
  title: string;
  value: string;
  subtitle?: string;
  icon: LucideIcon;
  iconColor?: string;
  iconBg?: string;
  trend?: { label: string; positive: boolean };
}

export function StatCard({
  title,
  value,
  subtitle,
  icon: Icon,
  iconColor = 'text-primary-600',
  iconBg = 'bg-primary-50',
  trend,
}: StatCardProps) {
  return (
    <div className="card p-6">
      <div className="flex items-start justify-between">
        <div className="min-w-0 flex-1">
          <p className="text-sm font-medium text-gray-500 truncate">{title}</p>
          <p className="mt-1 text-2xl font-bold text-gray-900 truncate">
            {value}
          </p>
          {subtitle && (
            <p className="mt-1 text-xs text-gray-400">{subtitle}</p>
          )}
          {trend && (
            <p
              className={clsx(
                'mt-2 text-xs font-medium',
                trend.positive ? 'text-green-600' : 'text-red-600'
              )}
            >
              {trend.positive ? '↑' : '↓'} {trend.label}
            </p>
          )}
        </div>
        <div className={clsx('rounded-xl p-3 shrink-0 ml-4', iconBg)}>
          <Icon className={clsx('h-5 w-5', iconColor)} />
        </div>
      </div>
    </div>
  );
}