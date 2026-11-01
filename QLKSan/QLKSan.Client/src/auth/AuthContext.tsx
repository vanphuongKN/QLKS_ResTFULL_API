import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, setAccessToken, setUnauthorizedHandler, type RegisterRequest } from '../api/client';
import { load, save, toSession, type Session } from './session';

interface AuthValue {
  session: Session | null;
  login: (tenDangNhap: string, matKhau: string) => Promise<void>;
  register: (r: RegisterRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthValue | null>(null);

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
