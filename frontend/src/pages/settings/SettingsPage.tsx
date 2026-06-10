import {
  Building2, Mail, Globe, FileText,
  Percent, Bell, RefreshCw, CheckCircle,
  XCircle, Clock, ChevronDown, ChevronUp,
  Eye, X,
} from 'lucide-react';
import { useState, useRef, useEffect } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import { useTenant, useNotificationLogs } from '../../hooks/useSettings';
import { PageHeader } from '../../components/ui/PageHeader';
import { Spinner } from '../../components/ui/Spinner';
import { NotificationBadge } from '../../components/ui/NotificationBadge';
import { formatDate, formatRelative } from '../../utils/format';
import { EVENT_TYPE_CONFIG } from '../../utils/eventType';
import type { NotificationLog } from '../../api/notifications';
import { clsx } from 'clsx';

export function SettingsPage() {
  const { user }   = useAuth();
  const { data: tenant, isLoading: tenantLoading } = useTenant();
  const {
    data: logs = [],
    isLoading: logsLoading,
    refetch: refetchLogs,
    isFetching,
  } = useNotificationLogs();

  const [activeTab, setActiveTab]     = useState<'profile' | 'notifications'>('profile');
  const [expandedLog, setExpandedLog] = useState<number | null>(null);
  const [previewLog, setPreviewLog]   = useState<NotificationLog | null>(null);

  return (
    <div className="p-8 max-w-4xl mx-auto">
      <PageHeader
        title="Settings"
        subtitle="Workspace configuration and activity"
      />

      {/* Tabs */}
      <div className="flex gap-1 mb-8 border-b border-gray-200">
        {([
          { id: 'profile',       label: 'Workspace Profile' },
          { id: 'notifications', label: `Notification Log ${logs.length > 0 ? `(${logs.length})` : ''}` },
        ] as const).map(tab => (
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

      {/* ── Profile tab ──────────────────────────────────────────────────── */}
      {activeTab === 'profile' && (
        <div className="space-y-6">
          {tenantLoading ? (
            <div className="flex justify-center py-12">
              <Spinner size="lg" />
            </div>
          ) : tenant ? (
            <>
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
                    <p className="text-sm text-gray-500">/{tenant.slug}</p>
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
                  <InfoRow icon={Mail}     label="Owner Email"    value={tenant.ownerEmail} />
                  <InfoRow icon={Globe}    label="Country"        value={tenant.settings?.countryCode ?? '—'} />
                  <InfoRow icon={Building2} label="Company Name"  value={tenant.settings?.companyName ?? '—'} />
                  <InfoRow icon={FileText} label="Invoice Prefix" value={tenant.settings?.invoicePrefix ?? '—'} />
                  <InfoRow icon={Globe}    label="Currency"       value={tenant.settings?.currency ?? '—'} />
                  <InfoRow icon={Percent}  label="Default Tax"
                    value={tenant.settings?.defaultTaxRate
                      ? `${(tenant.settings.defaultTaxRate * 100).toFixed(0)}%`
                      : '—'} />
                </div>
              </div>

              <div className="card p-6">
                <h3 className="text-sm font-semibold text-gray-900 mb-4">
                  Your Account
                </h3>
                <div className="grid grid-cols-2 gap-6">
                  <InfoRow icon={Mail}     label="Email"       value={user?.email ?? '—'} />
                  <InfoRow icon={Building2} label="Role"       value={user?.role ?? '—'} />
                  <InfoRow icon={Building2} label="Full Name"  value={user?.fullName ?? '—'} />
                  <InfoRow icon={FileText}  label="Member Since" value={formatDate(tenant.createdAt)} />
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

      {/* ── Notification log tab ──────────────────────────────────────────── */}
      {activeTab === 'notifications' && (
        <div>
          {/* Header */}
          <div className="flex items-center justify-between mb-4">
            <div>
              <p className="text-sm font-medium text-gray-900">
                Email Notifications
              </p>
              <p className="text-xs text-gray-400">
                Preview exactly what each email looks like — click Preview to see the full email
              </p>
            </div>
            <button
              onClick={() => refetchLogs()}
              disabled={isFetching}
              className="btn-secondary text-xs px-3 py-1.5"
            >
              {isFetching ? <Spinner size="sm" /> : <RefreshCw className="h-3.5 w-3.5" />}
              Refresh
            </button>
          </div>

          {/* Stats */}
          {!logsLoading && logs.length > 0 && (
            <div className="grid grid-cols-4 gap-3 mb-6">
              <StatPill icon={CheckCircle} label="Sent"
                count={logs.filter(l => l.status === 'Sent').length}
                color="text-green-600" bg="bg-green-50" />
              <StatPill icon={XCircle} label="Failed"
                count={logs.filter(l => l.status === 'Failed').length}
                color="text-red-600" bg="bg-red-50" />
              <StatPill icon={Clock} label="Pending"
                count={logs.filter(l => l.status === 'Pending').length}
                color="text-yellow-600" bg="bg-yellow-50" />
              <StatPill icon={Bell} label="Total"
                count={logs.length}
                color="text-primary-600" bg="bg-primary-50" />
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
                Create and send an invoice to trigger email notifications.
                They will appear here with a full preview.
              </p>
            </div>
          ) : (
            <div className="card divide-y divide-gray-50">
              {logs.map(log => {
                const cfg    = EVENT_TYPE_CONFIG[log.eventType];
                const isOpen = expandedLog === log.id;

                return (
                  <div key={log.id}>
                    <div className="flex items-center gap-3 px-5 py-4
                                    hover:bg-gray-50 transition-colors">

                      {/* Event dot */}
                      <div className={clsx(
                        'w-2 h-2 rounded-full shrink-0',
                        log.eventType === 'InvoiceCreated' && 'bg-primary-500',
                        log.eventType === 'InvoiceSent'    && 'bg-yellow-500',
                        log.eventType === 'InvoiceOverdue' && 'bg-red-500',
                        log.eventType === 'InvoicePaid'    && 'bg-green-500',
                      )} />

                      {/* Info */}
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className={clsx(
                            'text-sm font-medium',
                            cfg?.color ?? 'text-gray-700'
                          )}>
                            {cfg?.label ?? log.eventType}
                          </span>
                          <NotificationBadge status={log.status} />
                        </div>
                        <div className="flex items-center gap-3 mt-0.5">
                          <p className="text-xs text-gray-400 truncate">
                            To: {log.recipientEmail
                              ? `${log.recipientEmail} <${log.recipientEmail}>`
                              : log.recipientEmail}
                          </p>
                          <p className="text-xs text-gray-300">·</p>
                          <p className="text-xs text-gray-400 truncate">
                            {log.subject}
                          </p>
                        </div>
                      </div>

                      {/* Time + actions */}
                      <div className="flex items-center gap-2 shrink-0">
                        <span className="text-xs text-gray-400">
                          {formatRelative(log.createdAt)}
                        </span>

                        {/* Preview button */}
                        <button
                          onClick={() => setPreviewLog(log)}
                          className="flex items-center gap-1 text-xs
                                     text-primary-600 hover:text-primary-700
                                     bg-primary-50 hover:bg-primary-100
                                     px-2 py-1 rounded-md transition-colors"
                        >
                          <Eye className="h-3 w-3" />
                          Preview
                        </button>

                        {/* Expand for details */}
                        <button
                          onClick={() => setExpandedLog(isOpen ? null : log.id)}
                          className="text-gray-400 hover:text-gray-600"
                        >
                          {isOpen
                            ? <ChevronUp className="h-4 w-4" />
                            : <ChevronDown className="h-4 w-4" />}
                        </button>
                      </div>
                    </div>

                    {/* Expanded metadata */}
                    {isOpen && (
                      <div className="px-5 pb-4 bg-gray-50/50 border-t
                                      border-gray-100">
                        <div className="pt-3 grid grid-cols-2 gap-3 text-xs">
                          <div>
                            <p className="text-gray-400 mb-0.5">Subject</p>
                            <p className="text-gray-700 font-medium">
                              {log.subject}
                            </p>
                          </div>
                          <div>
                            <p className="text-gray-400 mb-0.5">Recipient</p>
                            <p className="text-gray-700">
                              {log.recipientEmail && (
                                <span className="font-medium">
                                  {log.recipientEmail}{' '}
                                </span>
                              )}
                              <span className="text-gray-500">
                                &lt;{log.recipientEmail}&gt;
                              </span>
                            </p>
                          </div>
                          <div>
                            <p className="text-gray-400 mb-0.5">Queued at</p>
                            <p className="text-gray-700">
                              {formatDate(log.createdAt)}
                            </p>
                          </div>
                          {log.sentAt && (
                            <div>
                              <p className="text-gray-400 mb-0.5">Sent at</p>
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
                          {log.status === 'Sent' &&
                           !log.sentAt && (
                            <div className="col-span-2">
                              <p className="text-xs text-amber-600
                                            bg-amber-50 rounded px-2 py-1">
                                ⚠ SendGrid API key not configured —
                                email preview available but not delivered.
                                Add your API key in appsettings.json to
                                enable real delivery.
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

      {/* ── Email preview modal ───────────────────────────────────────────── */}
      {previewLog && (
        <EmailPreviewModal
          log={previewLog}
          onClose={() => setPreviewLog(null)}
        />
      )}
    </div>
  );
}

// ── Email Preview Modal ───────────────────────────────────────────────────

function EmailPreviewModal({
  log,
  onClose,
}: {
  log: NotificationLog;
  onClose: () => void;
}) {
  const cfg = EVENT_TYPE_CONFIG[log.eventType];

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      style={{ background: 'rgba(0,0,0,0.6)' }}
      onClick={onClose}
    >
      <div
        className="bg-white rounded-2xl shadow-2xl w-full max-w-2xl
                   max-h-[90vh] flex flex-col overflow-hidden"
        onClick={e => e.stopPropagation()}
      >
        {/* Modal header */}
        <div className="flex items-center justify-between
                        px-6 py-4 border-b border-gray-100 shrink-0">
          <div className="flex items-center gap-3">
            <div className="rounded-lg bg-primary-50 p-1.5">
              <Mail className="h-4 w-4 text-primary-600" />
            </div>
            <div>
              <h2 className="text-sm font-semibold text-gray-900">
                Email Preview
              </h2>
              <p className="text-xs text-gray-400">
                {cfg?.label ?? log.eventType}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Email metadata bar */}
        <div className="px-6 py-3 bg-gray-50 border-b border-gray-100
                        shrink-0 space-y-1.5">
          <div className="flex items-start gap-2 text-xs">
            <span className="text-gray-400 w-14 shrink-0 pt-0.5">From</span>
            <span className="text-gray-700">
              BillFlow{' '}
              <span className="text-gray-400">&lt;noreply@billflow.io&gt;</span>
            </span>
          </div>
          <div className="flex items-start gap-2 text-xs">
            <span className="text-gray-400 w-14 shrink-0 pt-0.5">To</span>
            <span className="text-gray-700">
              {log.recipientEmail && (
                <span className="font-medium">{log.recipientEmail} </span>
              )}
              <span className="text-gray-400">
                &lt;{log.recipientEmail}&gt;
              </span>
            </span>
          </div>
          <div className="flex items-start gap-2 text-xs">
            <span className="text-gray-400 w-14 shrink-0 pt-0.5">
              Subject
            </span>
            <span className="text-gray-900 font-medium">{log.subject}</span>
          </div>
          <div className="flex items-start gap-2 text-xs">
            <span className="text-gray-400 w-14 shrink-0 pt-0.5">Date</span>
            <span className="text-gray-600">{formatDate(log.createdAt)}</span>
          </div>
          <div className="flex items-start gap-2 text-xs">
            <span className="text-gray-400 w-14 shrink-0 pt-0.5">
              Status
            </span>
            <div className="flex items-center gap-2">
              <NotificationBadge status={log.status} />
              {log.status === 'Sent' && !log.sentAt && (
                <span className="text-amber-600 text-xs">
                  (preview only — SendGrid key not configured)
                </span>
              )}
            </div>
          </div>
        </div>

        {/* Email body rendered in iframe */}
        <div className="flex-1 overflow-hidden">
         {log.body ? (
          <EmailIframe html={log.body} />
             ) : (
              <div className="flex flex-col items-center justify-center
                   h-48 text-sm text-gray-400">
                   <Mail className="h-8 w-8 mb-2 text-gray-300" />
                    <p>No email body recorded.</p>
                   <p className="text-xs mt-1">
                   Redeploy NotificationService to capture email HTML.
                   </p>
               </div>
      )}
        </div>

        {/* Footer note */}
        <div className="px-6 py-3 bg-amber-50 border-t border-amber-100
                        shrink-0">
          <p className="text-xs text-amber-700 text-center">
            📧 This is a preview of the email that would be sent to{' '}
            <strong>{log.recipientEmail}</strong>.
            Configure a SendGrid API key to enable real email delivery.
          </p>
        </div>
      </div>
    </div>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────
function EmailIframe({ html }: { html: string }) {
  const iframeRef = useRef<HTMLIFrameElement>(null);

  useEffect(() => {
    const iframe = iframeRef.current;
    if (!iframe) return;

    // Write HTML directly into the iframe document
    const doc = iframe.contentDocument || iframe.contentWindow?.document;
    if (!doc) return;

    doc.open();
    doc.write(html);
    doc.close();

    // Auto-resize iframe to content height
    const resize = () => {
      try {
        const height = doc.body?.scrollHeight;
        if (height && iframe) {
          iframe.style.height = `${Math.min(height + 20, 500)}px`;
        }
      } catch { }
    };

    // Give it time to render then resize
    setTimeout(resize, 100);
  }, [html]);

  return (
    <iframe
      ref={iframeRef}
      className="w-full border-0 bg-white"
      style={{ minHeight: '300px' }}
      title="Email Preview"
    />
  );
}
function InfoRow({ icon: Icon, label, value }: {
  icon: React.ElementType; label: string; value: string;
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

function StatPill({ icon: Icon, label, count, color, bg }: {
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