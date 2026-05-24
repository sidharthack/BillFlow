import {
  FileText,
  Users,
  TrendingUp,
  AlertTriangle,
  CheckCircle,
  Clock,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { useDashboard } from '../../hooks/useDashboard';
import { StatCard } from '../../components/ui/StatCard';
import { PageHeader } from '../../components/ui/PageHeader';
import { RevenueChart } from '../../components/ui/RevenueChart';
import { StatusChart } from '../../components/ui/StatusChart';
import { RecentInvoicesTable } from '../../components/ui/RecentInvoicesTable';
import { Spinner } from '../../components/ui/Spinner';
import { formatCurrency } from '../../utils/format';

export function DashboardPage() {
  const { user } = useAuth();
  const { stats, isLoading, isError } = useDashboard();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full min-h-96">
        <Spinner size="lg" />
      </div>
    );
  }

  if (isError || !stats) {
    return (
      <div className="p-8">
        <div className="card p-6 text-center text-red-600">
          Failed to load dashboard. Make sure all services are running.
        </div>
      </div>
    );
  }

  return (
    <div className="p-8 max-w-7xl mx-auto">
      {/* Header */}
      <PageHeader
        title={`Good ${getGreeting()}, ${user?.fullName?.split(' ')[0]}`}
        subtitle="Here's what's happening with your invoices today"
      />

      {/* Stat cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <StatCard
          title="Total Revenue"
          value={formatCurrency(stats.totalRevenue)}
          subtitle={`${stats.paidCount} paid invoices`}
          icon={TrendingUp}
          iconColor="text-green-600"
          iconBg="bg-green-50"
        />
        <StatCard
          title="Total Invoices"
          value={stats.totalInvoices.toString()}
          subtitle={`${stats.draftCount} draft · ${stats.sentCount} sent`}
          icon={FileText}
          iconColor="text-primary-600"
          iconBg="bg-primary-50"
        />
        <StatCard
          title="Overdue"
          value={stats.overdueCount.toString()}
          subtitle={
            stats.overdueCount > 0
              ? formatCurrency(stats.overdueAmount) + ' at risk'
              : 'Nothing overdue'
          }
          icon={AlertTriangle}
          iconColor={stats.overdueCount > 0 ? 'text-red-600' : 'text-gray-400'}
          iconBg={stats.overdueCount > 0 ? 'bg-red-50' : 'bg-gray-50'}
        />
        <StatCard
          title="Customers"
          value={stats.totalCustomers.toString()}
          subtitle="Active accounts"
          icon={Users}
          iconColor="text-blue-600"
          iconBg="bg-blue-50"
        />
      </div>

      {/* Charts row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8">

        {/* Revenue chart — takes 2/3 width */}
        <div className="card p-6 lg:col-span-2">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h2 className="text-sm font-semibold text-gray-900">
                Monthly Revenue
              </h2>
              <p className="text-xs text-gray-400">Last 6 months · paid invoices</p>
            </div>
            <TrendingUp className="h-4 w-4 text-gray-300" />
          </div>
          <RevenueChart data={stats.monthlyRevenue} />
        </div>

        {/* Status breakdown — takes 1/3 width */}
        <div className="card p-6">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h2 className="text-sm font-semibold text-gray-900">
                Invoice Status
              </h2>
              <p className="text-xs text-gray-400">Current breakdown</p>
            </div>
            <CheckCircle className="h-4 w-4 text-gray-300" />
          </div>
          <StatusChart data={stats.statusBreakdown} />
        </div>
      </div>

      {/* Quick summary row */}
      <div className="grid grid-cols-3 gap-4 mb-8">
        <div className="card p-4 flex items-center gap-3">
          <div className="rounded-lg bg-yellow-50 p-2">
            <Clock className="h-4 w-4 text-yellow-600" />
          </div>
          <div>
            <p className="text-xs text-gray-400">Awaiting Payment</p>
            <p className="text-lg font-bold text-gray-900">
              {stats.sentCount}
            </p>
          </div>
        </div>
        <div className="card p-4 flex items-center gap-3">
          <div className="rounded-lg bg-green-50 p-2">
            <CheckCircle className="h-4 w-4 text-green-600" />
          </div>
          <div>
            <p className="text-xs text-gray-400">Paid This Period</p>
            <p className="text-lg font-bold text-gray-900">
              {stats.paidCount}
            </p>
          </div>
        </div>
        <div className="card p-4 flex items-center gap-3">
          <div className="rounded-lg bg-gray-50 p-2">
            <FileText className="h-4 w-4 text-gray-400" />
          </div>
          <div>
            <p className="text-xs text-gray-400">Drafts</p>
            <p className="text-lg font-bold text-gray-900">
              {stats.draftCount}
            </p>
          </div>
        </div>
      </div>

      {/* Recent invoices */}
      <div className="card">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <h2 className="text-sm font-semibold text-gray-900">
            Recent Invoices
          </h2>
          
            <a href="/invoices"
            className="text-xs text-primary-600 hover:underline font-medium"
          >
            View all →
          </a>
        </div>
        <RecentInvoicesTable invoices={stats.recentInvoices} />
      </div>
    </div>
  );
}

function getGreeting(): string {
  const h = new Date().getHours();
  if (h < 12) return 'morning';
  if (h < 17) return 'afternoon';
  return 'evening';
}