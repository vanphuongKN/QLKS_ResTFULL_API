import type { LoginResponse } from '../api/client';

// Phiên đăng nhập của khách: JWT giữ trong sessionStorage (đóng tab là đăng xuất), tự đăng xuất khi hết hạn hoặc API trả 401
export interface Session {
  token: string;
  expiresAt: string;
  hoTen: string;
  tenDangNhap: string;
  maKhachHang: string;
}

const KEY = 'qlksan.session';

export function load(): Session | null {
  try {
    const s = JSON.parse(sessionStorage.getItem(KEY) ?? 'null') as Session | null;
    return s && new Date(s.expiresAt) > new Date() ? s : null;
  } catch {
    return null;
  }
}

export function save(s: Session | null) {
  try {
    if (s) sessionStorage.setItem(KEY, JSON.stringify(s));
    else sessionStorage.removeItem(KEY);
  } catch {
    /* trình duyệt chặn lưu trữ: vẫn đăng nhập trong trang hiện tại */
  }
}

// Trang này dành cho khách; nhân viên dùng cổng nhân viên (QLKSan.Web)
export function toSession(r: LoginResponse): Session {
  if (r.VaiTro !== 'Customer' || !r.AccessToken || !r.MaKhachHang)
    throw new Error('Tài khoản nhân viên vui lòng đăng nhập tại cổng nhân viên.');
  return { token: r.AccessToken, expiresAt: r.ExpiresAt ?? '', hoTen: r.HoTen ?? r.TenDangNhap ?? '', tenDangNhap: r.TenDangNhap ?? '', maKhachHang: r.MaKhachHang };
}
