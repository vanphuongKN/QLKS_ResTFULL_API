import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, setAccessToken, setUnauthorizedHandler, type LoginResponse, type RegisterRequest } from '../api/client';

// Phiên đăng nhập của khách: JWT giữ trong sessionStorage (đóng tab là đăng xuất), tự đăng xuất khi hết hạn hoặc API trả 401
export interface Session {
  token: string;
  expiresAt: string;
  hoTen: string;
  tenDangNhap: string;
  maKhachHang: string;
}

interface AuthValue {
  session: Session | null;
  login: (tenDangNhap: string, matKhau: string) => Promise<void>;
  register: (r: RegisterRequest) => Promise<void>;
  logout: () => void;
}

const KEY = 'qlksan.session';
const AuthContext = createContext<AuthValue | null>(null);

function load(): Session | null {
  try {
    const s = JSON.parse(sessionStorage.getItem(KEY) ?? 'null') as Session | null;
    return s && new Date(s.expiresAt) > new Date() ? s : null;
  } catch {
    return null;
  }
}

function save(s: Session | null) {
  try {
    if (s) sessionStorage.setItem(KEY, JSON.stringify(s));
    else sessionStorage.removeItem(KEY);
  } catch {
    /* trình duyệt chặn lưu trữ: vẫn đăng nhập trong trang hiện tại */
  }
}

// Trang này dành cho khách; nhân viên dùng cổng nhân viên (QLKSan.Web)
function toSession(r: LoginResponse): Session {
  if (r.VaiTro !== 'Customer' || !r.AccessToken || !r.MaKhachHang)
    throw new Error('Tài khoản nhân viên vui lòng đăng nhập tại cổng nhân viên.');
  return { token: r.AccessToken, expiresAt: r.ExpiresAt ?? '', hoTen: r.HoTen ?? r.TenDangNhap ?? '', tenDangNhap: r.TenDangNhap ?? '', maKhachHang: r.MaKhachHang };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(() => {
    const s = load();
    setAccessToken(s?.token ?? null);
    return s;
  });

  const apply = useCallback((s: Session | null) => {
    setAccessToken(s?.token ?? null);
    save(s);
    setSession(s);
  }, []);

  const logout = useCallback(() => apply(null), [apply]);

  useEffect(() => {
    setUnauthorizedHandler(logout);
    if (!session) return;
    const conLai = new Date(session.expiresAt).getTime() - Date.now();
    const timer = window.setTimeout(logout, Math.max(0, Math.min(conLai, 2_147_000_000)));
    return () => window.clearTimeout(timer);
  }, [session, logout]);

  const value = useMemo<AuthValue>(() => ({
    session,
    login: async (tenDangNhap, matKhau) => apply(toSession(await api.login(tenDangNhap, matKhau))),
    register: async (r) => apply(toSession(await api.register(r))),
    logout
  }), [session, apply, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth phải nằm trong AuthProvider');
  return ctx;
}
