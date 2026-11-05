import type { LoginResponse } from '../api/client';
import type { Role } from './roles';

// Phiên đăng nhập (khách và nhân viên): JWT giữ trong sessionStorage (đóng tab là đăng xuất),
// tự đăng xuất khi hết hạn hoặc API trả 401. Quyền thật vẫn do API kiểm tra theo JWT; vai trò ở đây chỉ để chọn giao diện.
export interface Session {
  token: string;
  expiresAt: string;
  hoTen: string;
  tenDangNhap: string;
  vaiTro: Role;
  maKhachHang?: string;
  maNhanVien?: string;
}

const KEY = 'qlksan.session';

export function load(): Session | null {
  try {
    const s = JSON.parse(sessionStorage.getItem(KEY) ?? 'null') as Session | null;
    // Phiên lưu từ bản cũ (chưa có vai trò) coi như chưa đăng nhập
    return s && s.vaiTro && new Date(s.expiresAt) > new Date() ? s : null;
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

export function toSession(r: LoginResponse): Session {
  if (!r.AccessToken || !r.VaiTro) throw new Error('Phản hồi đăng nhập không hợp lệ.');
  if (r.VaiTro === 'Customer' && !r.MaKhachHang) throw new Error('Tài khoản khách chưa gắn hồ sơ khách hàng.');
  return {
    token: r.AccessToken,
    expiresAt: r.ExpiresAt ?? '',
    hoTen: r.HoTen ?? r.TenDangNhap ?? '',
    tenDangNhap: r.TenDangNhap ?? '',
    vaiTro: r.VaiTro,
    maKhachHang: r.MaKhachHang ?? undefined,
    maNhanVien: r.MaNhanVien ?? undefined
  };
}
