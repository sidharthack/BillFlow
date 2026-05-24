import apiClient from './client';

export interface NotificationLog {
  id: number;
  tenantId: number;
  eventType: string;
  recipientEmail: string;
  subject: string;
  status: string;
  retryCount: number;
  createdAt: string;
  sentAt?: string;
  errorMessage?: string;
}

export const notificationsApi = {
  getLogs: async (tenantId: number): Promise<NotificationLog[]> => {
    const res = await apiClient.get<NotificationLog[]>(
      `/notificationlog?tenantId=${tenantId}`
    );
    return res.data;
  },
};