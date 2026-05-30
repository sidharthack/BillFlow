import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Zap, AlertCircle, ArrowRight, ArrowLeft,
  Building2, User, CheckCircle, ShieldCheck,
  RefreshCw, Search, ChevronDown, ChevronUp,
  Plus, Mail, Globe, Percent, FileText,
  LayoutDashboard,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { authApi, devApi } from '../../api/auth';
import { Spinner } from '../../components/ui/Spinner';
import { Badge } from '../../components/ui/Badge';
import { formatDate } from '../../utils/format';
import type { Tenant } from '../../types';
import { clsx } from 'clsx';

// ── Dev credentials ───────────────────────────────────────────────────────
const DEV_ID       = 'sidharthabehera@live.com';
const DEV_PASSWORD = 'Manager@22';

// ── Schemas ───────────────────────────────────────────────────────────────
const loginSchema = z.object({
  tenantSlug: z.string().min(1, 'Workspace is required'),
  email:      z.string().email('Invalid email'),
  password:   z.string().min(1, 'Password is required'),
});

const devGateSchema = z.object({
  devId:       z.string().min(1, 'Developer ID is required'),
  devPassword: z.string().min(1, 'Password is required'),
});

const tenantSchema = z.object({
  name:        z.string().min(2, 'At least 2 characters'),
  ownerEmail:  z.string().email('Invalid email'),
  companyName: z.string().optional(),
  currency:    z.string().default('INR'),
  countryCode: z.string().default('IN'),
});

const userSchema = z.object({
  fullName: z.string().min(2, 'Full name is required'),
  email:    z.string().email('Invalid email'),
  password: z.string()
    .min(8, 'Min 8 characters')
    .regex(/[A-Z]/, 'Must contain uppercase')
    .regex(/[0-9]/, 'Must contain a number')
    .regex(/[^A-Za-z0-9]/, 'Must contain a special character'),
  confirmPassword: z.string(),
  role: z.enum(['Admin', 'Member', 'Viewer']).default('Admin'),
}).refine(d => d.password === d.confirmPassword, {
  message: 'Passwords do not match',
  path: ['confirmPassword'],
});

type LoginData   = z.infer<typeof loginSchema>;
type DevGateData = z.infer<typeof devGateSchema>;
type Mode = 'login' | 'dev-gate' | 'dev-portal'
          | 'register-tenant' | 'register-user';

// ── Main component ────────────────────────────────────────────────────────
export function LoginPage() {
  const { login }  = useAuth();
  const navigate   = useNavigate();
  const [mode, setMode]    = useState<Mode>('login');
  const [error, setError]  = useState<string | null>(null);
  const [slug, setSlug]    = useState('');

  const clearError = () => setError(null);

  // Wide layout for dev portal
  const isWide = mode === 'dev-portal';

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 to-indigo-100
                    flex items-center justify-center p-4">
      <div className={clsx(
        'w-full transition-all duration-300',
        isWide ? 'max-w-4xl' : 'max-w-md'
      )}>

        {/* Logo */}
        <div className="flex items-center justify-center gap-3 mb-8">
          <div className="flex items-center justify-center w-10 h-10
                          rounded-xl bg-primary-500 shadow-lg">
            <Zap className="h-5 w-5 text-white" />
          </div>
          <span className="text-2xl font-bold text-gray-900">BillFlow</span>
          {mode === 'dev-portal' && (
            <span className="text-xs bg-amber-100 text-amber-700 font-medium
                             px-2 py-0.5 rounded-full border border-amber-200">
              Dev Portal
            </span>
          )}
        </div>

        {/* Step indicator */}
        {(mode === 'register-tenant' || mode === 'register-user') && (
          <div className="flex items-center gap-2 mb-6">
            <StepDot step={1} label="Organisation"
              active={mode === 'register-tenant'}
              done={mode === 'register-user'} />
            <div className={clsx('flex-1 h-px transition-colors',
              mode === 'register-user' ? 'bg-primary-400' : 'bg-gray-200')} />
            <StepDot step={2} label="Your account"
              active={mode === 'register-user'} done={false} />
          </div>
        )}

        {/* Card */}
        <div className="card p-8">

          {mode === 'login' && (
            <LoginForm
              error={error}
              onSuccess={(t, r, u) => {
                login(t, r, u);
                navigate('/dashboard', { replace: true });
              }}
              onError={setError}
              onRegister={() => { clearError(); setMode('dev-gate'); }}
            />
          )}

          {mode === 'dev-gate' && (
            <DevGateForm
              error={error}
              onSuccess={() => { clearError(); setMode('dev-portal'); }}
              onError={setError}
              onBack={() => { clearError(); setMode('login'); }}
            />
          )}

          {mode === 'dev-portal' && (
            <DevPortal
              onCreateTenant={() => { clearError(); setMode('register-tenant'); }}
              onBack={() => { clearError(); setMode('login'); }}
            />
          )}

          {mode === 'register-tenant' && (
            <RegisterTenantForm
              error={error}
              onSuccess={(s) => { setSlug(s); clearError(); setMode('register-user'); }}
              onError={setError}
              onBack={() => { clearError(); setMode('dev-portal'); }}
            />
          )}

          {mode === 'register-user' && (
            <RegisterUserForm
              tenantSlug={slug}
              error={error}
              onSuccess={(t, r, u) => {
                login(t, r, u);
                navigate('/dashboard', { replace: true });
              }}
              onError={setError}
              onBack={() => { clearError(); setMode('register-tenant'); }}
            />
          )}

        </div>

        <p className="text-center text-xs text-gray-400 mt-6">
          BillFlow · Invoice & Billing SaaS
        </p>
      </div>
    </div>
  );
}

// ── Dev Portal ────────────────────────────────────────────────────────────

function DevPortal({
  onCreateTenant,
  onBack,
}: {
  onCreateTenant: () => void;
  onBack: () => void;
}) {
  const [tenants, setTenants]   = useState<Tenant[]>([]);
  const [loading, setLoading]   = useState(true);
  const [search, setSearch]     = useState('');
  const [expanded, setExpanded] = useState<number | null>(null);
  const [addUserFor, setAddUserFor] = useState<Tenant | null>(null);
  const [error, setError]       = useState('');

  const load = async () => {
    setLoading(true);
    try {
      const data = await devApi.getAllTenants();
      setTenants(data);
    } catch {
      setError('Failed to load tenants');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const filtered = tenants.filter(t =>
    t.name.toLowerCase().includes(search.toLowerCase()) ||
    t.slug.toLowerCase().includes(search.toLowerCase()) ||
    t.ownerEmail.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <>
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <div className="rounded-lg bg-amber-50 p-1.5">
              <ShieldCheck className="h-4 w-4 text-amber-600" />
            </div>
            <h1 className="text-xl font-bold text-gray-900">Developer Portal</h1>
          </div>
          <p className="text-sm text-gray-500">
            {tenants.length} organisation{tenants.length !== 1 ? 's' : ''} registered
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={load}
            disabled={loading}
            className="btn-secondary text-xs px-3 py-1.5"
          >
            {loading ? <Spinner size="sm" /> : <RefreshCw className="h-3.5 w-3.5" />}
            Refresh
          </button>
          <button
            onClick={onBack}
            className="btn-secondary text-xs px-3 py-1.5"
          >
            <LayoutDashboard className="h-3.5 w-3.5" />
            Back to login
          </button>
        </div>
      </div>

      {/* Stats row */}
      {!loading && tenants.length > 0 && (
        <div className="grid grid-cols-3 gap-3 mb-5">
          <StatPill
            label="Total"
            value={tenants.length}
            color="text-primary-600"
            bg="bg-primary-50"
          />
          <StatPill
            label="Active"
            value={tenants.filter(t => t.status === 'Active').length}
            color="text-green-600"
            bg="bg-green-50"
          />
          <StatPill
            label="Plans"
            value={[...new Set(tenants.map(t => t.plan))].length}
            color="text-amber-600"
            bg="bg-amber-50"
          />
        </div>
      )}

      {/* Search + Create */}
      <div className="flex gap-2 mb-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2
                             h-3.5 w-3.5 text-gray-400" />
          <input
            className="input pl-8 text-sm"
            placeholder="Search organisations..."
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        <button
          onClick={onCreateTenant}
          className="btn-primary text-xs px-4 shrink-0"
        >
          <Plus className="h-3.5 w-3.5" />
          New Org
        </button>
      </div>

      {/* Error */}
      {error && <ErrorAlert message={error} />}

      {/* Register user modal */}
      {addUserFor && (
        <AddUserModal
          tenant={addUserFor}
          onClose={() => setAddUserFor(null)}
          onSuccess={() => { setAddUserFor(null); load(); }}
        />
      )}

      {/* Tenant list */}
      <div className="space-y-2 max-h-[420px] overflow-y-auto pr-1">
        {loading ? (
          <div className="flex justify-center py-10">
            <Spinner size="lg" />
          </div>
        ) : filtered.length === 0 ? (
          <div className="text-center py-10 text-sm text-gray-400">
            {search ? `No results for "${search}"` : 'No organisations yet'}
          </div>
        ) : (
          filtered.map(tenant => (
            <TenantRow
              key={tenant.id}
              tenant={tenant}
              isExpanded={expanded === tenant.id}
              onToggle={() =>
                setExpanded(expanded === tenant.id ? null : tenant.id)
              }
              onAddUser={() => setAddUserFor(tenant)}
            />
          ))
        )}
      </div>
    </>
  );
}

// ── Tenant row ────────────────────────────────────────────────────────────

function TenantRow({
  tenant, isExpanded, onToggle, onAddUser,
}: {
  tenant: Tenant;
  isExpanded: boolean;
  onToggle: () => void;
  onAddUser: () => void;
}) {
  return (
    <div className="border border-gray-100 rounded-lg overflow-hidden
                    hover:border-gray-200 transition-colors">

      {/* Row header */}
      <button
        onClick={onToggle}
        className="w-full flex items-center gap-3 px-4 py-3 text-left
                   hover:bg-gray-50 transition-colors"
      >
        {/* Avatar */}
        <div className="w-8 h-8 rounded-full bg-primary-100 text-primary-700
                        font-bold text-sm flex items-center justify-center shrink-0">
          {tenant.name.charAt(0).toUpperCase()}
        </div>

        {/* Info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <p className="text-sm font-medium text-gray-900 truncate">
              {tenant.name}
            </p>
            <Badge label={tenant.status} />
            <span className="text-xs text-gray-400 bg-gray-100
                             px-1.5 py-0.5 rounded font-mono">
              {tenant.plan}
            </span>
          </div>
          <p className="text-xs text-gray-400 font-mono truncate">
            /{tenant.slug}
          </p>
        </div>

        {/* Expand icon */}
        {isExpanded
          ? <ChevronUp className="h-4 w-4 text-gray-400 shrink-0" />
          : <ChevronDown className="h-4 w-4 text-gray-400 shrink-0" />}
      </button>

      {/* Expanded detail */}
      {isExpanded && (
        <div className="border-t border-gray-100 bg-gray-50/50 px-4 py-4">
          <div className="grid grid-cols-2 gap-3 mb-4">

            <DetailRow
              icon={Mail}
              label="Owner email"
              value={tenant.ownerEmail}
            />
            <DetailRow
              icon={Globe}
              label="Country"
              value={tenant.settings?.countryCode ?? '—'}
            />
            <DetailRow
              icon={FileText}
              label="Invoice prefix"
              value={tenant.settings?.invoicePrefix ?? '—'}
            />
            <DetailRow
              icon={Percent}
              label="Default tax"
              value={tenant.settings?.defaultTaxRate
                ? `${(tenant.settings.defaultTaxRate * 100).toFixed(0)}%`
                : '—'}
            />
            <DetailRow
              icon={Globe}
              label="Currency"
              value={tenant.settings?.currency ?? '—'}
            />
            <DetailRow
              icon={Building2}
              label="Created"
              value={formatDate(tenant.createdAt)}
            />
          </div>

          {/* Company name */}
          {tenant.settings?.companyName && (
            <div className="mb-4 px-3 py-2 bg-white rounded-lg
                            border border-gray-100 text-xs text-gray-600">
              <span className="text-gray-400">Company: </span>
              {tenant.settings.companyName}
            </div>
          )}

          {/* Actions */}
          <div className="flex items-center gap-2 pt-2 border-t border-gray-100">
            <p className="text-xs text-gray-400 flex-1">
              ID: {tenant.id}
            </p>

            {/* Add user to this tenant */}
            <button
              onClick={onAddUser}
              className="btn-secondary text-xs px-3 py-1.5"
            >
              <User className="h-3.5 w-3.5" />
              Add user
            </button>

            {/* Login hint */}
            <div className="text-xs text-gray-400 bg-gray-100 rounded
                            px-2 py-1 font-mono">
              slug: {tenant.slug}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Add user modal ────────────────────────────────────────────────────────

function AddUserModal({
  tenant, onClose, onSuccess,
}: {
  tenant: Tenant;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [error, setError] = useState('');
  const [done, setDone]   = useState(false);

  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm({
      resolver: zodResolver(userSchema),
      defaultValues: { role: 'Admin' },
    });

  const onSubmit = async (data: z.infer<typeof userSchema>) => {
    setError('');
    try {
      await authApi.registerUser({
        tenantSlug: tenant.slug,
        email:      data.email,
        fullName:   data.fullName,
        password:   data.password,
        role:       data.role,
      });
      setDone(true);
      setTimeout(onSuccess, 1500);
    } catch (err: any) {
      setError(
        err.response?.data?.message ??
        err.response?.data?.error ??
        'Failed to create user'
      );
    }
  };

  return (
    // Overlay
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4"
         style={{ background: 'rgba(0,0,0,0.4)' }}>
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-md p-6
                      border border-gray-100">

        {/* Header */}
        <div className="flex items-center justify-between mb-4">
          <div>
            <h2 className="text-base font-semibold text-gray-900">
              Add user to {tenant.name}
            </h2>
            <p className="text-xs text-gray-400 font-mono mt-0.5">
              /{tenant.slug}
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 text-lg leading-none"
          >
            ×
          </button>
        </div>

        {done ? (
          <div className="flex flex-col items-center py-6 text-center">
            <CheckCircle className="h-10 w-10 text-green-500 mb-3" />
            <p className="text-sm font-medium text-gray-900">User created!</p>
            <p className="text-xs text-gray-400 mt-1">Closing...</p>
          </div>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
            <div>
              <label className="label">Full name *</label>
              <input
                className="input"
                placeholder="Raj Kumar"
                {...register('fullName')}
              />
              {errors.fullName && (
                <p className="text-xs text-red-500 mt-1">
                  {errors.fullName.message}
                </p>
              )}
            </div>

            <div>
              <label className="label">Email *</label>
              <input
                type="email"
                className="input"
                placeholder="raj@acme.com"
                {...register('email')}
              />
              {errors.email && (
                <p className="text-xs text-red-500 mt-1">
                  {errors.email.message}
                </p>
              )}
            </div>

            <div>
              <label className="label">Password *</label>
              <input
                type="password"
                className="input"
                placeholder="Min 8 chars, uppercase, number, symbol"
                {...register('password')}
              />
              {errors.password && (
                <p className="text-xs text-red-500 mt-1">
                  {errors.password.message}
                </p>
              )}
            </div>

            <div>
              <label className="label">Confirm password *</label>
              <input
                type="password"
                className="input"
                placeholder="••••••••"
                {...register('confirmPassword')}
              />
              {errors.confirmPassword && (
                <p className="text-xs text-red-500 mt-1">
                  {errors.confirmPassword.message}
                </p>
              )}
            </div>

            <div>
              <label className="label">Role</label>
              <select className="input" {...register('role')}>
                <option value="Admin">Admin — full access</option>
                <option value="Member">Member — create & edit</option>
                <option value="Viewer">Viewer — read only</option>
              </select>
            </div>

            {error && <ErrorAlert message={error} />}

            <div className="flex gap-2 pt-2">
              <button
                type="button"
                onClick={onClose}
                className="btn-secondary flex-1"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="btn-primary flex-1"
              >
                {isSubmitting
                  ? <><Spinner size="sm" /> Creating...</>
                  : <><User className="h-4 w-4" /> Create user</>}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────

function StepDot({ step, label, active, done }: {
  step: number; label: string; active: boolean; done: boolean;
}) {
  return (
    <div className="flex flex-col items-center gap-1">
      <div className={clsx(
        'w-8 h-8 rounded-full flex items-center justify-center',
        'text-sm font-medium transition-all',
        done   && 'bg-primary-500 text-white',
        active && !done && 'bg-primary-500 text-white ring-4 ring-primary-100',
        !active && !done && 'bg-gray-100 text-gray-400'
      )}>
        {done ? <CheckCircle className="h-4 w-4" /> : step}
      </div>
      <span className={clsx(
        'text-xs whitespace-nowrap',
        (active || done) ? 'text-primary-600 font-medium' : 'text-gray-400'
      )}>
        {label}
      </span>
    </div>
  );
}

function DetailRow({ icon: Icon, label, value }: {
  icon: React.ElementType; label: string; value: string;
}) {
  return (
    <div className="flex items-center gap-2">
      <Icon className="h-3.5 w-3.5 text-gray-400 shrink-0" />
      <div className="min-w-0">
        <p className="text-xs text-gray-400">{label}</p>
        <p className="text-xs font-medium text-gray-700 truncate">{value}</p>
      </div>
    </div>
  );
}

function StatPill({ label, value, color, bg }: {
  label: string; value: number; color: string; bg: string;
}) {
  return (
    <div className={clsx('rounded-xl p-3 flex items-center gap-2', bg)}>
      <div>
        <p className={clsx('text-lg font-bold leading-none', color)}>{value}</p>
        <p className="text-xs text-gray-500 mt-0.5">{label}</p>
      </div>
    </div>
  );
}

function ErrorAlert({ message }: { message: string }) {
  if (!message) return null;
  return (
    <div className="flex items-start gap-2 p-3 rounded-lg
                    bg-red-50 text-red-700 text-sm">
      <AlertCircle className="h-4 w-4 mt-0.5 shrink-0" />
      {message}
    </div>
  );
}

// ── Login form ────────────────────────────────────────────────────────────

function LoginForm({ error, onSuccess, onError, onRegister }: {
  error: string | null;
  onSuccess:  (t: string, r: string, u: any) => void;
  onError:    (m: string) => void;
  onRegister: () => void;
}) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<LoginData>({ resolver: zodResolver(loginSchema) });

  const onSubmit = async (data: LoginData) => {
    onError('');
    try {
      const res = await authApi.login(data);
      onSuccess(res.accessToken, res.refreshToken, res.user);
    } catch (err: any) {
      onError(
        err.response?.data?.error ??
        err.response?.data?.message ??
        'Invalid credentials.'
      );
    }
  };

  return (
    <>
      <div className="mb-6">
        <h1 className="text-xl font-bold text-gray-900">Welcome back</h1>
        <p className="text-sm text-gray-500 mt-1">Sign in to your workspace</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="label">
            <Building2 className="inline h-3.5 w-3.5 mr-1 text-gray-400" />
            Workspace
          </label>
          <input
            className="input"
            placeholder="acme-corporation"
            {...register('tenantSlug')}
          />
          {errors.tenantSlug && (
            <p className="text-xs text-red-500 mt-1">
              {errors.tenantSlug.message}
            </p>
          )}
          <p className="text-xs text-gray-400 mt-1">
            Your organisation's unique workspace ID
          </p>
        </div>

        <div>
          <label className="label">Email</label>
          <input type="email" className="input"
            placeholder="you@company.com" {...register('email')} />
          {errors.email && (
            <p className="text-xs text-red-500 mt-1">{errors.email.message}</p>
          )}
        </div>

        <div>
          <label className="label">Password</label>
          <input type="password" className="input"
            placeholder="••••••••" {...register('password')} />
          {errors.password && (
            <p className="text-xs text-red-500 mt-1">
              {errors.password.message}
            </p>
          )}
        </div>

        {error && <ErrorAlert message={error} />}

        <button type="submit" disabled={isSubmitting}
          className="btn-primary w-full py-2.5">
          {isSubmitting
            ? <><Spinner size="sm" /> Signing in...</>
            : <><ArrowRight className="h-4 w-4" /> Sign in</>}
        </button>
      </form>

      <div className="mt-6 pt-5 border-t border-gray-100 text-center">
        <p className="text-sm text-gray-500">
          Don't have a workspace?{' '}
          <button onClick={onRegister}
            className="text-primary-600 font-medium hover:underline">
            Create one free
          </button>
        </p>
      </div>
    </>
  );
}

// ── Dev gate form ─────────────────────────────────────────────────────────

function DevGateForm({ error, onSuccess, onError, onBack }: {
  error: string | null;
  onSuccess: () => void;
  onError:   (m: string) => void;
  onBack:    () => void;
}) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<DevGateData>({ resolver: zodResolver(devGateSchema) });

  const onSubmit = async (data: DevGateData) => {
    onError('');
    await new Promise(r => setTimeout(r, 600));
    if (data.devId === DEV_ID && data.devPassword === DEV_PASSWORD) {
      onSuccess();
    } else {
      onError('Invalid developer credentials. Access denied.');
    }
  };

  return (
    <>
      <div className="mb-6">
        <button onClick={onBack}
          className="flex items-center gap-1 text-xs text-gray-400
                     hover:text-gray-600 mb-3 transition-colors">
          <ArrowLeft className="h-3.5 w-3.5" /> Back to sign in
        </button>
        <div className="flex items-center gap-2 mb-1">
          <div className="rounded-lg bg-amber-50 p-1.5">
            <ShieldCheck className="h-4 w-4 text-amber-600" />
          </div>
          <h1 className="text-xl font-bold text-gray-900">Developer access</h1>
        </div>
        <p className="text-sm text-gray-500">
          Enter developer credentials to access the portal
        </p>
      </div>

      <div className="flex items-start gap-2 p-3 rounded-lg bg-amber-50
                      border border-amber-100 mb-5">
        <ShieldCheck className="h-4 w-4 text-amber-600 shrink-0 mt-0.5" />
        <p className="text-xs text-amber-700 leading-relaxed">
          The developer portal lets you view all organisations,
          create new workspaces, and manage users across tenants.
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="label">Developer ID</label>
          <input className="input font-mono" placeholder="dev-id"
            autoComplete="off" {...register('devId')} />
          {errors.devId && (
            <p className="text-xs text-red-500 mt-1">{errors.devId.message}</p>
          )}
        </div>

        <div>
          <label className="label">Developer password</label>
          <input type="password" className="input" placeholder="••••••••"
            autoComplete="off" {...register('devPassword')} />
          {errors.devPassword && (
            <p className="text-xs text-red-500 mt-1">
              {errors.devPassword.message}
            </p>
          )}
        </div>

        {error && <ErrorAlert message={error} />}

        <button type="submit" disabled={isSubmitting}
          className="btn-primary w-full py-2.5">
          {isSubmitting
            ? <><Spinner size="sm" /> Verifying...</>
            : <><ShieldCheck className="h-4 w-4" /> Enter portal</>}
        </button>
      </form>
    </>
  );
}

// ── Register tenant form ──────────────────────────────────────────────────

function RegisterTenantForm({ error, onSuccess, onError, onBack }: {
  error: string | null;
  onSuccess: (slug: string) => void;
  onError:   (m: string) => void;
  onBack:    () => void;
}) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm({
      resolver: zodResolver(tenantSchema),
      defaultValues: { currency: 'INR', countryCode: 'IN' },
    });

  const onSubmit = async (data: z.infer<typeof tenantSchema>) => {
    onError('');
    try {
      const res = await authApi.registerTenant(data);
      onSuccess(res.slug);
    } catch (err: any) {
      onError(
        err.response?.data?.message ??
        err.response?.data?.error ??
        'Failed to create organisation.'
      );
    }
  };

  return (
    <>
      <div className="mb-6">
        <button onClick={onBack}
          className="flex items-center gap-1 text-xs text-gray-400
                     hover:text-gray-600 mb-3 transition-colors">
          <ArrowLeft className="h-3.5 w-3.5" /> Back to portal
        </button>
        <div className="flex items-center gap-2 mb-1">
          <div className="rounded-lg bg-primary-50 p-1.5">
            <Building2 className="h-4 w-4 text-primary-600" />
          </div>
          <h1 className="text-xl font-bold text-gray-900">
            Create organisation
          </h1>
        </div>
        <p className="text-sm text-gray-500">Set up a new BillFlow workspace</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="label">Organisation name *</label>
          <input className="input" placeholder="Acme Corporation"
            {...register('name')} />
          {errors.name && (
            <p className="text-xs text-red-500 mt-1">{errors.name.message}</p>
          )}
          <p className="text-xs text-gray-400 mt-1">
            Workspace slug is auto-generated from this name
          </p>
        </div>

        <div>
          <label className="label">Owner email *</label>
          <input type="email" className="input" placeholder="admin@acme.com"
            {...register('ownerEmail')} />
          {errors.ownerEmail && (
            <p className="text-xs text-red-500 mt-1">
              {errors.ownerEmail.message}
            </p>
          )}
        </div>

        <div>
          <label className="label">Company display name</label>
          <input className="input" placeholder="Acme Corporation Pvt Ltd"
            {...register('companyName')} />
          <p className="text-xs text-gray-400 mt-1">
            Appears on invoices and notification emails
          </p>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Currency</label>
            <select className="input" {...register('currency')}>
              <option value="INR">INR — ₹ Rupee</option>
              <option value="USD">USD — $ Dollar</option>
              <option value="EUR">EUR — € Euro</option>
              <option value="GBP">GBP — £ Pound</option>
            </select>
          </div>
          <div>
            <label className="label">Country</label>
            <select className="input" {...register('countryCode')}>
              <option value="IN">India</option>
              <option value="US">United States</option>
              <option value="GB">United Kingdom</option>
              <option value="SG">Singapore</option>
              <option value="AU">Australia</option>
            </select>
          </div>
        </div>

        {error && <ErrorAlert message={error} />}

        <button type="submit" disabled={isSubmitting}
          className="btn-primary w-full py-2.5">
          {isSubmitting
            ? <><Spinner size="sm" /> Creating workspace...</>
            : <><ArrowRight className="h-4 w-4" /> Continue</>}
        </button>
      </form>
    </>
  );
}

// ── Register user form ────────────────────────────────────────────────────

function RegisterUserForm({ tenantSlug, error, onSuccess, onError, onBack }: {
  tenantSlug: string;
  error: string | null;
  onSuccess: (t: string, r: string, u: any) => void;
  onError:   (m: string) => void;
  onBack:    () => void;
}) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm({
      resolver: zodResolver(userSchema),
      defaultValues: { role: 'Admin' },
    });

  const onSubmit = async (data: z.infer<typeof userSchema>) => {
    onError('');
    try {
      const res = await authApi.registerUser({
        tenantSlug,
        email:    data.email,
        fullName: data.fullName,
        password: data.password,
        role:     data.role,
      });
      onSuccess(res.accessToken, res.refreshToken, res.user);
    } catch (err: any) {
      onError(
        err.response?.data?.message ??
        err.response?.data?.error ??
        'Failed to create account.'
      );
    }
  };

  return (
    <>
      <div className="mb-6">
        <button onClick={onBack}
          className="flex items-center gap-1 text-xs text-gray-400
                     hover:text-gray-600 mb-3 transition-colors">
          <ArrowLeft className="h-3.5 w-3.5" /> Back
        </button>
        <div className="flex items-center gap-2 mb-1">
          <div className="rounded-lg bg-primary-50 p-1.5">
            <User className="h-4 w-4 text-primary-600" />
          </div>
          <h1 className="text-xl font-bold text-gray-900">Create your account</h1>
        </div>
        <p className="text-sm text-gray-500">
          Workspace:{' '}
          <span className="font-mono font-medium text-primary-600">
            {tenantSlug}
          </span>
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="label">Full name *</label>
          <input className="input" placeholder="Raj Kumar"
            {...register('fullName')} />
          {errors.fullName && (
            <p className="text-xs text-red-500 mt-1">
              {errors.fullName.message}
            </p>
          )}
        </div>

        <div>
          <label className="label">Email *</label>
          <input type="email" className="input" placeholder="raj@acme.com"
            {...register('email')} />
          {errors.email && (
            <p className="text-xs text-red-500 mt-1">{errors.email.message}</p>
          )}
        </div>

        <div>
          <label className="label">Password *</label>
          <input type="password" className="input"
            placeholder="Min 8 chars, uppercase, number, symbol"
            {...register('password')} />
          {errors.password && (
            <p className="text-xs text-red-500 mt-1">
              {errors.password.message}
            </p>
          )}
        </div>

        <div>
          <label className="label">Confirm password *</label>
          <input type="password" className="input" placeholder="••••••••"
            {...register('confirmPassword')} />
          {errors.confirmPassword && (
            <p className="text-xs text-red-500 mt-1">
              {errors.confirmPassword.message}
            </p>
          )}
        </div>

        <div>
          <label className="label">Role</label>
          <select className="input" {...register('role')}>
            <option value="Admin">Admin — full access</option>
            <option value="Member">Member — create & edit</option>
            <option value="Viewer">Viewer — read only</option>
          </select>
        </div>

        {error && <ErrorAlert message={error} />}

        <button type="submit" disabled={isSubmitting}
          className="btn-primary w-full py-2.5">
          {isSubmitting
            ? <><Spinner size="sm" /> Creating account...</>
            : <><CheckCircle className="h-4 w-4" /> Create account & sign in</>}
        </button>
      </form>
    </>
  );
}