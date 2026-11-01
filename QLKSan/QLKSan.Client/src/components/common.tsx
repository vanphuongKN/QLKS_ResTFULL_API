import { useEffect, useState, type DependencyList, type ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import type { BookingStatus } from '../api/client';
import { TRANG_THAI } from '../utils/format';

// Tải dữ liệu bất đồng bộ: { data, error, loading }; bỏ kết quả cũ khi phụ thuộc đổi
export function useAsync<T>(load: () => Promise<T>, deps: DependencyList) {
  const [state, setState] = useState<{ data?: T; error?: string; loading: boolean }>({ loading: true });
  useEffect(() => {
    let huy = false;
    setState((s) => ({ data: s.data, loading: true }));
    load().then(
      (data) => !huy && setState({ data, loading: false }),
      (e: Error) => !huy && setState({ error: e.message, loading: false })
    );
    return () => {
      huy = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);
  return state;
}

export function Loading({ text = 'Đang tải…' }: { text?: string }) {
  return <p className="loading" role="status">{text}</p>;
}

export function ErrorBox({ message }: { message?: string }) {
  return message ? <div className="alert alert-danger" role="alert">{message}</div> : null;
}

export function StatusBadge({ status }: { status?: BookingStatus }) {
  const s = status ? TRANG_THAI[status] : undefined;
  return s ? <span className={`badge badge-${s.css}`}>{s.nhan}</span> : null;
}

// Trang cần đăng nhập: chuyển tới /dang-nhap rồi quay lại đúng trang sau khi đăng nhập
export function RequireAuth({ children }: { children: ReactNode }) {
  const { session } = useAuth();
  const location = useLocation();
  if (!session) return <Navigate to={`/dang-nhap?quayLai=${encodeURIComponent(location.pathname + location.search)}`} replace />;
  return <>{children}</>;
}

export function Stars({ value }: { value?: number | null }) {
  const n = Math.max(0, Math.min(5, Math.round(value ?? 0)));
  return <span className="stars" aria-label={`${n} trên 5 sao`}>{'★'.repeat(n)}<span className="stars-off">{'★'.repeat(5 - n)}</span></span>;
}
