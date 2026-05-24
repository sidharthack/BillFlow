import { useState } from 'react';
import {
  Building2, Mail, Globe, FileText,
  Percent, Bell, RefreshCw, CheckCircle,
  XCircle, Clock, ChevronDown, ChevronUp,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { useTenant, useNotificationLogs } from '../../hooks/useSettings';
import { PageHeader } from '../../components/ui/PageHeader';
import { Spinner } from '../../components/ui/Spinner';
import { NotificationBadge } from '../../components/ui/NotificationBadge';
import { formatDate, formatRelative } from '../../utils/format';
import { EVENT_TYPE_CONFIG } from '../../utils/eventType';
import { clsx } from 'clsx';

export function SettingsPage() {
  const { user } = useAuth();
  const { data: tenant, isLoading: tenantLoading } = useTenant();
  const {
    data: logs = [],
    isLoading: logsLoading,
    refetch: refetchLogs,
    isFetching,
  } = useNotificationLogs();

  const [expandedLog, setExpandedLog] = useState<number | null>(null);
  const [activeTab, setActiveTab]     = useState<'profile' | 'notifications'>('profile');

  return (
    <div className="p-8 max-w-4xl mx-auto">
      <PageHeader
        title="Settings"
        subtitle="Workspace configuration and activity"
      />

      {/* Tabs */}
      <div className="flex gap-1 mb-8 border-b border-gray-200">
        {(
          [
            { id: 'profile',       label: 'Workspace Profile' },
            { id: 'notifications', label: 'Notification Log'  },
          ] as const
        ).map(tab => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={clsx(
              'px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors',
              activeTab === tab.id
                ? 'border-primary-500 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* ── Profile tab ─────────────────────────────────────────────────── */}
      {activeTab === 'profile' && (
        <div className="space-y-6">

          {tenantLoading ? (
            <div className="flex justify-center py-12">
              <Spinner size="lg" />
            </div>
          ) : tenant ? (
            <>
              {/* Workspace card */}
              <div className="card p-6">
                <div className="flex items-center gap-4 mb-6">
                  <div className="w-14 h-14 rounded-xl bg-primary-100
                                  text-primary-700 font-bold text-xl
                                  flex items-center justify-center">
                    {tenant.name.charAt(0).toUpperCase()}
                  </div>
                  <div>
                    <h2 className="text-lg font-bold text-gray-900">
                      {tenant.name}
                    </h2>
                    <p className="text-sm text-gray-500">
                      /{tenant.slug}
                    </p>
                    <div className="flex items-center gap-2 mt-1">
                      <span className="badge bg-primary-100 text-primary-700">
                        {tenant.plan}
                      </span>
                      <span className={clsx(
                        'badge',
                        tenant.status === 'Active'
                          ? 'bg-green-100 text-green-700'
                          : 'bg-gray-100 text-gray-500'
                      )}>
                        {tenant.status}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-6">
                  <InfoRow
                    icon={Mail}
                    label="Owner Email"
                    value={tenant.ownerEmail}
                  />
                  <InfoRow
                    icon={Globe}
                    label="Country"
                    value={tenant.settings.countryCode}
                  />
                  <InfoRow
                    icon={Building2}
                    label="Company Name"
                    value={tenant.settings.companyName}
                  />
                  <InfoRow
                    icon={FileText}
                    label="Invoice Prefix"
                    value={tenant.settings.invoicePrefix}
                  />
                  <InfoRow
                    icon={Globe}
                    label="Currency"
                    value={tenant.settings.currency}
                  />
                  <InfoRow
                    icon={Percent}
                    label="Default Tax Rate"
                    value={`${(tenant.settings.defaultTaxRate * 100).toFixed(0)}%`}
                  />
                </div>
              </div>

              {/* Account card */}
              <div className="card p-6">
                <h3 className="text-sm font-semibold text-gray-900 mb-4">
                  Your Account
                </h3>
                <div className="grid grid-cols-2 gap-6">
                  <InfoRow
                    icon={Mail}
                    label="Email"
                    value={user?.email ?? '—'}
                  />
                  <InfoRow
                    icon={Building2}
                    label="Role"
                    value={user?.role ?? '—'}
                  />
                  <InfoRow
                    icon={Building2}
                    label="Full Name"
                    value={user?.fullName ?? '—'}
                  />
                  <InfoRow
                    icon={FileText}
                    label="Member Since"
                    value={formatDate(tenant.createdAt)}
                  />
                </div>
              </div>
            </>
          ) : (
            <div className="card p-6 text-center text-sm text-red-500">
              Failed to load tenant settings.
            </div>
          )}
        </div>
      )}

      {/* ── Notification log tab ─────────────────────────────────────────── */}
      {activeTab === 'notifications' && (
        <div>
          {/* Header row */}
          <div className="flex items-center justify-between mb-4">
            <div>
              <p className="text-sm font-medium text-gray-900">
                Email Notifications
              </p>
              <p className="text-xs text-gray-400">
                Last 100 notifications sent by BillFlow
              </p>
            </div>
            <button
              onClick={() => refetchLogs()}
              disabled={isFetching}
              className="btn-secondary text-xs px-3 py-1.5"
            >
              {isFetching ? (
                <Spinner size="sm" />
              ) : (
                <RefreshCw className="h-3.5 w-3.5" />
              )}
              Refresh
            </button>
          </div>

          {/* Stats row */}
          {!logsLoading && logs.length > 0 && (
            <div className="grid grid-cols-4 gap-3 mb-6">
              <StatPill
                icon={CheckCircle}
                label="Sent"
                count={logs.filter(l => l.status === 'Sent').length}
                color="text-green-600"
                bg="bg-green-50"
              />
              <StatPill
                icon={XCircle}
                label="Failed"
                count={logs.filter(l => l.status === 'Failed').length}
                color="text-red-600"
                bg="bg-red-50"
              />
              <StatPill
                icon={Clock}
                label="Pending"
                count={logs.filter(l => l.status === 'Pending').length}
                color="text-yellow-600"
                bg="bg-yellow-50"
              />
              <StatPill
                icon={Bell}
                label="Total"
                count={logs.length}
                color="text-primary-600"
                bg="bg-primary-50"
              />
            </div>
          )}

          {/* Log list */}
          {logsLoading ? (
            <div className="flex justify-center py-12">
              <Spinner size="lg" />
            </div>
          ) : logs.length === 0 ? (
            <div className="card p-12 text-center">
              <Bell className="h-8 w-8 text-gray-300 mx-auto mb-3" />
              <p className="text-sm font-medium text-gray-900">
                No notifications yet
              </p>
              <p className="text-xs text-gray-400 mt-1">
                Create and send an invoice to trigger notifications.
              </p>
            </div>
          ) : (
            <div className="card divide-y divide-gray-50">
              {logs.map(log => {
                const cfg    = EVENT_TYPE_CONFIG[log.eventType];
                const isOpen = expandedLog === log.id;

                return (
                  <div key={log.id} className="hover:bg-gray-50/50">
                    <button
                      onClick={() =>
                        setExpandedLog(isOpen ? null : log.id)
                      }
                      className="w-full text-left px-5 py-4"
                    >
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-3 min-w-0">

                          {/* Event type dot */}
                          <div className={clsx(
                            'w-2 h-2 rounded-full shrink-0',
                            log.eventType === 'InvoiceCreated' && 'bg-primary-500',
                            log.eventType === 'InvoiceSent'    && 'bg-yellow-500',
                            log.eventType === 'InvoiceOverdue' && 'bg-red-500',
                            log.eventType === 'InvoicePaid'    && 'bg-green-500',
                          )} />

                          <div className="min-w-0">
                            <div className="flex items-center gap-2">
                              <span className={clsx(
                                'text-sm font-medium',
                                cfg?.color ?? 'text-gray-700'
                              )}>
                                {cfg?.label ?? log.eventType}
                              </span>
                              <NotificationBadge status={log.status} />
                            </div>
                            <p className="text-xs text-gray-400 truncate mt-0.5">
                              To: {log.recipientEmail}
                            </p>
                          </div>
                        </div>

                        <div className="flex items-center gap-3 shrink-0 ml-4">
                          <span className="text-xs text-gray-400">
                            {formatRelative(log.createdAt)}
                          </span>
                          {isOpen ? (
                            <ChevronUp className="h-4 w-4 text-gray-400" />
                          ) : (
                            <ChevronDown className="h-4 w-4 text-gray-400" />
                          )}
                        </div>
                      </div>
                    </button>

                    {/* Expanded detail */}
                    {isOpen && (
                      <div className="px-5 pb-4 space-y-3 border-t
                                      border-gray-100 bg-gray-50/50">
                        <div className="pt-3 grid grid-cols-2 gap-3 text-xs">
                          <div>
                            <p className="text-gray-400 mb-0.5">Subject</p>
                            <p className="text-gray-700 font-medium">
                              {log.subject}
                            </p>
                          </div>
                          <div>
                            <p className="text-gray-400 mb-0.5">Recipient</p>
                            <p className="text-gray-700 font-medium">
                              {log.recipientEmail}
                            </p>
                          </div>
                          <div>
                            <p className="text-gray-400 mb-0.5">Queued At</p>
                            <p className="text-gray-700">
                              {formatDate(log.createdAt)}
                            </p>
                          </div>
                          {log.sentAt && (
                            <div>
                              <p className="text-gray-400 mb-0.5">Sent At</p>
                              <p className="text-green-600 font-medium">
                                {formatDate(log.sentAt)}
                              </p>
                            </div>
                          )}
                          {log.errorMessage && (
                            <div className="col-span-2">
                              <p className="text-gray-400 mb-0.5">Error</p>
                              <p className="text-red-600 font-mono text-xs
                                            bg-red-50 rounded px-2 py-1">
                                {log.errorMessage}
                              </p>
                            </div>
                          )}
                          {log.retryCount > 0 && (
                            <div>
                              <p className="text-gray-400 mb-0.5">
                                Retry Count
                              </p>
                              <p className="text-yellow-600">
                                {log.retryCount}
                              </p>
                            </div>
                          )}
                        </div>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────

function InfoRow({
  icon: Icon,
  label,
  value,
}: {
  icon: React.ElementType;
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-start gap-3">
      <div className="rounded-lg bg-gray-50 p-2 shrink-0">
        <Icon className="h-4 w-4 text-gray-400" />
      </div>
      <div>
        <p className="text-xs text-gray-400">{label}</p>
        <p className="text-sm font-medium text-gray-900 mt-0.5">{value}</p>
      </div>
    </div>
  );
}

function StatPill({
  icon: Icon,
  label,
  count,
  color,
  bg,
}: {
  icon: React.ElementType;
  label: string;
  count: number;
  color: string;
  bg: string;
}) {
  return (
    <div className={clsx('rounded-xl p-3 flex items-center gap-2', bg)}>
      <Icon className={clsx('h-4 w-4', color)} />
      <div>
        <p className={clsx('text-lg font-bold leading-none', color)}>
          {count}
        </p>
        <p className="text-xs text-gray-500 mt-0.5">{label}</p>
      </div>
    </div>
  );
}