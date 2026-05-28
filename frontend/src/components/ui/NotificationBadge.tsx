import { clsx } from 'clsx';
import { CheckCircle, XCircle, Clock, SkipForward } from 'lucide-react';

const CONFIG = {
  Sent:    { icon: CheckCircle,  cls: 'bg-green-100 text-green-700'  },
  Failed:  { icon: XCircle,      cls: 'bg-red-100 text-red-700'      },
  Pending: { icon: Clock,        cls: 'bg-yellow-100 text-yellow-700' },
  Skipped: { icon: SkipForward,  cls: 'bg-gray-100 text-gray-500'    },
};

export function NotificationBadge({ status }: { status: string }) {
  const cfg = CONFIG[status as keyof typeof CONFIG] ?? CONFIG.Pending;
  const Icon = cfg.icon;

  return (
    <span className={clsx('badge gap-1', cfg.cls)}>
      <Icon className="h-3 w-3" />
      {status}
    </span>
  );
}