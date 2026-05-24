import { NavLink } from 'react-router-dom';
import { clsx } from 'clsx';
import { useInvoices } from '../../hooks/useInvoices';

import {
  LayoutDashboard,
  Users,
  FileText,
  Settings,
  LogOut,
  Zap,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';

const nav = [
  { to: '/dashboard', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/customers', icon: Users,           label: 'Customers' },
  { to: '/invoices',  icon: FileText,        label: 'Invoices'  },
  { to: '/settings',  icon: Settings,        label: 'Settings'  },
];

export function Sidebar() {
  const { data: invoices = [] } = useInvoices();
  const overdueCount = invoices.filter(i => i.status === 'Overdue').length;
  const { user, logout } = useAuth();

  return (
    <aside className="flex flex-col w-60 min-h-screen bg-white border-r border-gray-200">
      {/* Logo */}
      <div className="flex items-center gap-2 px-6 py-5 border-b border-gray-200">
        <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-primary-500">
          <Zap className="h-4 w-4 text-white" />
        </div>
        <span className="font-bold text-gray-900 text-lg">BillFlow</span>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-3 py-4 space-y-1">
        {nav.map(({ to, icon: Icon, label }) => (
  <NavLink
    key={to}
    to={to}
    className={({ isActive }) =>
      clsx(
        'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-all',
        isActive
          ? 'bg-primary-50 text-primary-600'
          : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
      )
    }
  >
    <div className="relative">
      <Icon className="h-4 w-4 shrink-0" />
      {/* Red dot for overdue invoices on the Invoices nav item */}
      {label === 'Invoices' && overdueCount > 0 && (
        <span className="absolute -top-1 -right-1 w-2 h-2
                         bg-red-500 rounded-full" />
      )}
    </div>
    {label}
    {/* Count badge */}
    {label === 'Invoices' && overdueCount > 0 && (
      <span className="ml-auto text-xs bg-red-100 text-red-600
                       px-1.5 py-0.5 rounded-full font-medium">
        {overdueCount}
      </span>
    )}
  </NavLink>
))}
      </nav>

      {/* User footer */}
      <div className="p-3 border-t border-gray-200">
        <div className="flex items-center gap-3 px-3 py-2 mb-1">
          <div className="flex items-center justify-center w-8 h-8 rounded-full bg-primary-100 text-primary-700 text-sm font-bold shrink-0">
            {user?.fullName?.charAt(0).toUpperCase() ?? 'U'}
          </div>
          <div className="min-w-0">
            <p className="text-sm font-medium text-gray-900 truncate">
              {user?.fullName}
            </p>
            <p className="text-xs text-gray-500 truncate">{user?.email}</p>
          </div>
        </div>
        <button
          onClick={logout}
          className="flex items-center gap-3 w-full px-3 py-2 rounded-lg text-sm text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition-all"
        >
          <LogOut className="h-4 w-4" />
          Sign out
        </button>
      </div>
    </aside>
  );
}