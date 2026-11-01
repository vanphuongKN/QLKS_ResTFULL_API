// Trợ lý ảo (QLKSan.ChatBot) qua Gateway: /api/chat/conversations — hợp đồng ChatDtos trong QLKSan.Shared
import { request } from './client';

export interface ChatLink {
  Nhan: string;
  Url: string;
}

export interface ChatMessage {
  VaiTro: 'khach' | 'bot';
  NoiDung: string;
  ThoiGian: string;
  LienKet: ChatLink[];
  GoiY: string[];
  Provider?: string | null;
}

export interface ChatConversation {
  MaHoiThoai: string;
  ThoiGianTao: string;
  TinNhan: ChatMessage[];
}

export const chatApi = {
  create: () => request<ChatConversation>('POST', '/api/chat/conversations'),
  get: (id: string) => request<ChatConversation>('GET', `/api/chat/conversations/${encodeURIComponent(id)}`),
  send: (id: string, noiDung: string) => request<ChatMessage>('POST', `/api/chat/conversations/${encodeURIComponent(id)}/messages`, { NoiDung: noiDung })
};
