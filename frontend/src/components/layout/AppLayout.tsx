import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { ErrorBoundary } from '../ui/ErrorBoundary';

export function AppLayout() {
  return (
    <div className="flex min-h-screen bg-gray-50">
      <Sidebar />
      <main className="flex-1 overflow-auto">
        <ErrorBoundary>
          <Outlet />
        </ErrorBoundary>
      </main>
    </div>
  );
}