import type { BookingStatus } from '../api/client';

const vnd = new Intl.NumberFormat('vi-VN');

export const tien = (v?: number | null) => `${vnd.format(Math.round(v ?? 0))} ₫`;

// yyyy-MM-dd theo giờ máy (không dùng toISOString để tránh lệch ngày do múi giờ)
export function isoDate(d: Date) {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

export function addDays(iso: string, n: number) {
  const [y, m, d] = iso.split('-').map(Number);
  return isoDate(new Date(y, m - 1, d + n));
}

export const homNay = () => isoDate(new Date());

export function ngay(v?: string | null) {
  if (!v) return '';
  const [y, m, d] = v.slice(0, 10).split('-');
  return `${d}/${m}/${y}`;
}

export function soDem(checkIn: string, checkOut: string) {
  return Math.round((new Date(checkOut).getTime() - new Date(checkIn).getTime()) / 86_400_000);
}

export const TRANG_THAI: Record<BookingStatus, { nhan: string; css: string }> = {
  Pending: { nhan: 'Chờ xác nhận', css: 'warn' },
  Approved: { nhan: 'Đã xác nhận', css: 'ok' },
  CheckedIn: { nhan: 'Đang lưu trú', css: 'info' },
  CheckedOut: { nhan: 'Đã trả phòng', css: 'muted' },
  Completed: { nhan: 'Hoàn tất', css: 'muted' },
  Cancelled: { nhan: 'Đã huỷ', css: 'danger' }
};

// Ảnh: /uploads/... do API phục vụ (qua Gateway), /images/... là ảnh mẫu dùng chung
export const anh = (duongDan?: string | null) => duongDan || '/images/room_garden.png';
