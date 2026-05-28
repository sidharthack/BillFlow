import { createContext, useContext, useState, useCallback } from 'react';
import type { ReactNode } from 'react';
import { clsx } from 'clsx';
import { CheckCircle, XCircle, X, AlertCircle } from 'lucide-react';

type ToastType = 'success' | 'error' | 'info';

interface Toast {
  id: string;
  type: ToastType;
  message: string;
}

interface ToastContextType {
  success: (message: string) => void;
  error: (message: string) => void;
  info: (message: string) => void;
}

const ToastContext = createContext<ToastContextType | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const add = useCallback((type: ToastType, message: string) => {
    const id = Math.random().toString(36).slice(2);
    setToasts(t => [...t, { id, type, message }]);
    setTimeout(() => {
      setToasts(t => t.filter(toast => toast.id !== id));
    }, 4000);
  }, []);

  const remove = useCallback((id: string) => {
    setToasts(t => t.filter(toast => toast.id !== id));
  }, []);

  return (
    <ToastContext.Provider
      value={{
        success: (m) => add('success', m),
        error:   (m) => add('error',   m),
        info:    (m) => add('info',    m),
      }}
    >
      {children}

      {/* Toast container */}
      <div className="fixed bottom-4 right-4 z-50 flex flex-col gap-2 w-80">
        {toasts.map(toast => (
          <div
            key={toast.id}
            className={clsx(
              'flex items-start gap-3 card px-4 py-3 shadow-lg',
              'animate-in slide-in-from-right-5',
              toast.type === 'success' && 'border-l-4 border-green-500',
              toast.type === 'error'   && 'border-l-4 border-red-500',
              toast.type === 'info'    && 'border-l-4 border-primary-500'
            )}
          >
            {toast.type === 'success' && (
              <CheckCircle className="h-4 w-4 text-green-500 shrink-0 mt-0.5" />
            )}
            {toast.type === 'error' && (
              <XCircle className="h-4 w-4 text-red-500 shrink-0 mt-0.5" />
            )}
            {toast.type === 'info' && (
              <AlertCircle className="h-4 w-4 text-primary-500 shrink-0 mt-0.5" />
            )}
            <p className="text-sm text-gray-700 flex-1">{toast.message}</p>
            <button
              onClick={() => remove(toast.id)}
              className="text-gray-400 hover:text-gray-600 shrink-0"
            >
              <X className="h-3.5 w-3.5" />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) throw new Error('useToast must be used within ToastProvider');
  return context;
}