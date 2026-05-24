import { useQuery } from '@tanstack/react-query';
import { tenantApi } from '../api/tenant';
import { notificationsApi } from '../api/notifications';
import { useAuth } from '../contexts/AuthContext';

export function useTenant() {
  const { user } = useAuth();
  return useQuery({
    queryKey: ['tenant', user?.tenantSlug],
    queryFn: () => tenantApi.getCurrent(user!.tenantSlug),
    enabled: !!user?.tenantSlug,
  });
}

export function useNotificationLogs() {
  const { user } = useAuth();
  return useQuery({
    queryKey: ['notification-logs', user?.tenantId],
    queryFn: () => notificationsApi.getLogs(user!.tenantId),
    enabled: !!user?.tenantId,
    refetchInterval: 30_000, // refresh every 30s
  });
}