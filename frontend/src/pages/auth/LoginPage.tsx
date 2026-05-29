import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Zap, AlertCircle, ArrowRight,
  ArrowLeft, Building2, User, CheckCircle,
  ShieldCheck,
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { authApi } from '../../api/auth';
import { Spinner } from '../../components/ui/Spinner';
import { clsx } from 'clsx';

// ── Dev credentials — change these to whatever you want ──────────────────
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
type TenantInput = z.input<typeof tenantSchema>;
type UserInput   = z.input<typeof userSchema>;

type Mode = 'login' | 'dev-gate' | 'register-tenant' | 'register-user';

// ── Main component ────────────────────────────────────────────────────────

export function LoginPage() {
  const { login }  = useAuth();
  const navigate   = useNavigate();
  const [mode, setMode]           = useState<Mode>('login');
  const [error, setError]         = useState<string | null>(null);
  const [registeredSlug, setSlug] = useState('');

  const clearError = () => setError(null);

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 to-indigo-100
                    flex items-center justify-center p-4">
      <div className="w-full max-w-md">

        {/* Logo */}
        <div className="flex items-center justify-center gap-3 mb-8">
          <div className="flex items-center justify-center w-10 h-10
                          rounded-xl bg-primary-500 shadow-lg">
            <Zap className="h-5 w-5 text-white" />
          </div>
          <span className="text-2xl font-bold text-gray-900">BillFlow</span>
        </div>

        {/* Step indicator — registration steps only */}
        {(mode === 'register-tenant' || mode === 'register-user') && (
          <div className="flex items-center gap-2 mb-6">
            <StepDot
              step={1}
              label="Organisation"
              active={mode === 'register-tenant'}
              done={mode === 'register-user'}
            />
            <div className={clsx(
              'flex-1 h-px transition-colors',
              mode === 'register-user' ? 'bg-primary-400' : 'bg-gray-200'
            )} />
            <StepDot
              step={2}
              label="Your account"
              active={mode === 'register-user'}
              done={false}
            />
          </div>
        )}

        {/* Card */}
        <div className="card p-8">

          {mode === 'login' && (
            <LoginForm
              error={error}
              onSuccess={(t, r, u) => { login(t, r, u); navigate('/dashboard', { replace: true }); }}
              onError={setError}
              onRegister={() => { clearError(); setMode('dev-gate'); }}
            />
          )}

          {mode === 'dev-gate' && (
            <DevGateForm
              error={error}
              onSuccess={() => { clearError(); setMode('register-tenant'); }}
              onError={setError}
              onBack={() => { clearError(); setMode('login'); }}
            />
          )}

          {mode === 'register-tenant' && (
            <RegisterTenantForm
              error={error}
              onSuccess={(slug) => { setSlug(slug); clearError(); setMode('register-user'); }}
              onError={setError}
              onBack={() => { clearError(); setMode('dev-gate'); }}
            />
          )}

          {mode === 'register-user' && (
            <RegisterUserForm
              tenantSlug={registeredSlug}
              error={error}
              onSuccess={(t, r, u) => { login(t, r, u); navigate('/dashboard', { replace: true }); }}
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

// ── Step dot ──────────────────────────────────────────────────────────────

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
        'Invalid credentials. Please try again.'
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
          <input
            type="email"
            className="input"
            placeholder="you@company.com"
            {...register('email')}
          />
          {errors.email && (
            <p className="text-xs text-red-500 mt-1">{errors.email.message}</p>
          )}
        </div>

        <div>
          <label className="label">Password</label>
          <input
            type="password"
            className="input"
            placeholder="••••••••"
            {...register('password')}
          />
          {errors.password && (
            <p className="text-xs text-red-500 mt-1">
              {errors.password.message}
            </p>
          )}
        </div>

        {error && <ErrorAlert message={error} />}

        <button
          type="submit"
          disabled={isSubmitting}
          className="btn-primary w-full py-2.5"
        >
          {isSubmitting
            ? <><Spinner size="sm" /> Signing in...</>
            : <><ArrowRight className="h-4 w-4" /> Sign in</>}
        </button>
      </form>

      <div className="mt-6 pt-5 border-t border-gray-100 text-center">
        <p className="text-sm text-gray-500">
          Don't have a workspace?{' '}
          <button
            onClick={onRegister}
            className="text-primary-600 font-medium hover:underline"
          >
            Create one free
          </button>
        </p>
      </div>
    </>
  );
}

// ── Developer gate form ───────────────────────────────────────────────────

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

    // Simulate a small delay so it feels like a real auth check
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
        <button
          onClick={onBack}
          className="flex items-center gap-1 text-xs text-gray-400
                     hover:text-gray-600 mb-3 transition-colors"
        >
          <ArrowLeft className="h-3.5 w-3.5" /> Back to sign in
        </button>

        <div className="flex items-center gap-2 mb-1">
          <div className="rounded-lg bg-amber-50 p-1.5">
            <ShieldCheck className="h-4 w-4 text-amber-600" />
          </div>
          <h1 className="text-xl font-bold text-gray-900">
            Developer access
          </h1>
        </div>
        <p className="text-sm text-gray-500">
          Enter your developer credentials to create a new workspace
        </p>
      </div>

      {/* Info banner */}
      <div className="flex items-start gap-2 p-3 rounded-lg bg-amber-50
                      border border-amber-100 mb-5">
        <ShieldCheck className="h-4 w-4 text-amber-600 shrink-0 mt-0.5" />
        <p className="text-xs text-amber-700 leading-relaxed">
          Workspace creation is restricted to authorised developers.
          Contact your administrator if you need access.
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="label">Developer ID</label>
          <input
            className="input font-mono"
            placeholder="dev-id"
            autoComplete="off"
            {...register('devId')}
          />
          {errors.devId && (
            <p className="text-xs text-red-500 mt-1">{errors.devId.message}</p>
          )}
        </div>

        <div>
          <label className="label">Developer password</label>
          <input
            type="password"
            className="input"
            placeholder="••••••••"
            autoComplete="off"
            {...register('devPassword')}
          />
          {errors.devPassword && (
            <p className="text-xs text-red-500 mt-1">
              {errors.devPassword.message}
            </p>
          )}
        </div>

        {error && <ErrorAlert message={error} />}

        <button
          type="submit"
          disabled={isSubmitting}
          className="btn-primary w-full py-2.5"
        >
          {isSubmitting
            ? <><Spinner size="sm" /> Verifying...</>
            : <><ShieldCheck className="h-4 w-4" /> Verify & continue</>}
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
    useForm<TenantInput>({
      resolver: zodResolver(tenantSchema),
      defaultValues: { currency: 'INR', countryCode: 'IN' },
    });

  const onSubmit = async (data: TenantInput) => {
    onError('');
    try {
      const res = await authApi.registerTenant(data);
      onSuccess(res.slug);
    } catch (err: any) {
      onError(
        err.response?.data?.message ??
        err.response?.data?.error ??
        'Failed to create organisation. Please try again.'
      );
    }
  };

  return (
    <>
      <div className="mb-6">
        <button
          onClick={onBack}
          className="flex items-center gap-1 text-xs text-gray-400
                     hover:text-gray-600 mb-3 transition-colors"
        >
          <ArrowLeft className="h-3.5 w-3.5" /> Back
        </button>
        <div className="flex items-center gap-2 mb-1">
          <div className="rounded-lg bg-primary-50 p-1.5">
            <Building2 className="h-4 w-4 text-primary-600" />
          </div>
          <h1 className="text-xl font-bold text-gray-900">
            Create your organisation
          </h1>
        </div>
        <p className="text-sm text-gray-500">
          Set up your BillFlow workspace
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="label">Organisation name *</label>
          <input
            className="input"
            placeholder="Acme Corporation"
            {...register('name')}
          />
          {errors.name && (
            <p className="text-xs text-red-500 mt-1">{errors.name.message}</p>
          )}
          <p className="text-xs text-gray-400 mt-1">
            Workspace slug is auto-generated from this name
          </p>
        </div>

        <div>
          <label className="label">Owner email *</label>
          <input
            type="email"
            className="input"
            placeholder="admin@acme.com"
            {...register('ownerEmail')}
          />
          {errors.ownerEmail && (
            <p className="text-xs text-red-500 mt-1">
              {errors.ownerEmail.message}
            </p>
          )}
        </div>

        <div>
          <label className="label">Company display name</label>
          <input
            className="input"
            placeholder="Acme Corporation Pvt Ltd"
            {...register('companyName')}
          />
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

        <button
          type="submit"
          disabled={isSubmitting}
          className="btn-primary w-full py-2.5"
        >
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
    useForm<UserInput>({
      resolver: zodResolver(userSchema),
      defaultValues: { role: 'Admin' },
    });

  const onSubmit = async (data: UserInput) => {
    onError('');
    try {
      const res = await authApi.registerUser({
        tenantSlug,
        email:    data.email,
        fullName: data.fullName,
        password: data.password,
        role:     data.role ?? 'Admin',
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
        <button
          onClick={onBack}
          className="flex items-center gap-1 text-xs text-gray-400
                     hover:text-gray-600 mb-3 transition-colors"
        >
          <ArrowLeft className="h-3.5 w-3.5" /> Back
        </button>
        <div className="flex items-center gap-2 mb-1">
          <div className="rounded-lg bg-primary-50 p-1.5">
            <User className="h-4 w-4 text-primary-600" />
          </div>
          <h1 className="text-xl font-bold text-gray-900">
            Create your account
          </h1>
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
            <p className="text-xs text-red-500 mt-1">{errors.email.message}</p>
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

        <button
          type="submit"
          disabled={isSubmitting}
          className="btn-primary w-full py-2.5"
        >
          {isSubmitting
            ? <><Spinner size="sm" /> Creating account...</>
            : <><CheckCircle className="h-4 w-4" /> Create account & sign in</>}
        </button>
      </form>
    </>
  );
}

// ── Shared error alert ────────────────────────────────────────────────────

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